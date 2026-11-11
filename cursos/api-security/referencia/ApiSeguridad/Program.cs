using System.Security.Claims;
using ApiSeguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configuracion: la clave sale de la configuracion (variable de entorno o gestor de secretos), nunca del codigo,
// y la aplicacion no arranca si no mide al menos 256 bits.
builder.Services.AddOptions<OpcionesJwt>().BindConfiguration("Jwt")
    .Validate(o => ClavesJwt.EsValida(o.Clave), "La clave de firma debe medir al menos 256 bits (32 bytes).")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Emisor) && !string.IsNullOrWhiteSpace(o.Audiencia), "Faltan el emisor o la audiencia.")
    .ValidateOnStart();
builder.Services.AddOptions<LabOpciones>().BindConfiguration("Lab");
builder.Services.AddOptions<VistasOpciones>().BindConfiguration("Vistas");

builder.Services.AddSingleton<Almacen>();
builder.Services.AddSingleton<Catalogo>();
builder.Services.AddSingleton<EmisorDeTokens>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.ConfigureOptions<ConfigurarJwtBearer>();
builder.Services.AddAuthorization(o =>
{
    // Todo lo que no diga lo contrario exige sesion: una ruta nueva nace protegida.
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    o.AddPolicy("Admin", p => p.RequireRole("admin"));
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("https://app.ejemplo.test")
    .WithMethods("GET", "POST", "PATCH")
    .WithHeaders("Authorization", "Content-Type")));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();
builder.Services.AgregarLimites();
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = Infraestructura.MaximoCuerpoBytes);

var app = builder.Build();
var lab = app.Configuration.GetSection("Lab").Get<LabOpciones>() ?? new LabOpciones();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCabecerasDeApi();
app.UseCors();
app.UseTopeDeCuerpo();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

static int IdDe(ClaimsPrincipal yo) => int.Parse(yo.FindFirstValue("sub")!);

var api = app.MapGroup("/api");

// --- Autenticacion ---
var hashFalso = Contrasenas.Crear("clave-que-nadie-tiene");
api.MapPost("/auth/login", (LoginSolicitud s, Almacen datos, EmisorDeTokens emisor) =>
{
    var usuario = datos.Usuarios.FirstOrDefault(u => string.Equals(u.Nombre, s.Usuario, StringComparison.OrdinalIgnoreCase));
    // Mismo trabajo y misma respuesta si el usuario no existe: no se revela quien tiene cuenta.
    var correcta = Contrasenas.Verificar(s.Clave, usuario?.HashContrasena ?? hashFalso);
    return correcta && usuario is not null
        ? Results.Ok(new { token = emisor.Crear(usuario.Id, usuario.Rol) })
        : Results.Problem(statusCode: 401, title: "Credenciales invalidas");
}).AllowAnonymous().RequireRateLimiting("login");

// --- Perfil (propiedades: API3) ---
api.MapGet("/perfil", (ClaimsPrincipal yo, Almacen datos, IOptions<LabOpciones> opciones) =>
{
    var u = datos.Usuarios.First(x => x.Id == IdDe(yo));
    return opciones.Value.EntidadCompleta ? Results.Ok(u) : Results.Ok(new PerfilDto(u.Id, u.Nombre, u.Rol));
});

api.MapPatch("/perfil", async (HttpRequest peticion, ClaimsPrincipal yo, Almacen datos, IOptions<LabOpciones> opciones) =>
{
    var u = datos.Usuarios.First(x => x.Id == IdDe(yo));
    if (opciones.Value.Bopla)
    {
        // VULNERABLE (solo laboratorio): se enlaza la entidad completa y se copia todo lo que llegue.
        var cambios = await peticion.ReadFromJsonAsync<Usuario>();
        if (cambios is not null)
        {
            if (!string.IsNullOrWhiteSpace(cambios.Nombre)) u.Nombre = cambios.Nombre;
            if (!string.IsNullOrWhiteSpace(cambios.Rol)) u.Rol = cambios.Rol;
        }
    }
    else
    {
        var cambios = await peticion.ReadFromJsonAsync<PerfilCambio>();
        if (cambios?.Nombre is not { Length: > 0 and <= 60 } nombre)
            return Results.Problem(statusCode: 400, title: "Nombre invalido");
        u.Nombre = nombre;
    }
    return Results.Ok(new PerfilDto(u.Id, u.Nombre, u.Rol));
});

// --- Pedidos (objetos: API1) ---
api.MapGet("/pedidos", (ClaimsPrincipal yo, Almacen datos) =>
    datos.Pedidos.Where(p => p.DuenoId == IdDe(yo)).Select(p => new PedidoDto(p.Id, p.Total, p.Estado)));

api.MapGet("/pedidos/{id:int}", (int id, ClaimsPrincipal yo, Almacen datos, IOptions<LabOpciones> opciones) =>
{
    var pedido = datos.Pedidos.FirstOrDefault(p => p.Id == id);
    // La pregunta correcta no es "esta autenticado" sino "este pedido es suyo". Se responde 404 en los dos casos.
    if (pedido is null || (!opciones.Value.Bola && pedido.DuenoId != IdDe(yo))) return Results.NotFound();
    return Results.Ok(new PedidoDto(pedido.Id, pedido.Total, pedido.Estado));
});

// --- Flujo de negocio (API6): un cupon, una vez por pedido ---
api.MapPost("/pedidos/{id:int}/cupon", (int id, CuponSolicitud c, ClaimsPrincipal yo, Almacen datos, IOptions<LabOpciones> opciones) =>
{
    lock (datos.Candado) // revisar y marcar es una sola operacion: sin el candado dos peticiones simultaneas pasarian las dos
    {
        var pedido = datos.Pedidos.FirstOrDefault(p => p.Id == id && p.DuenoId == IdDe(yo));
        if (pedido is null) return Results.NotFound();
        if (c.Codigo != "PROMO10") return Results.Problem(statusCode: 422, title: "Cupon desconocido");
        if (pedido.CuponAplicado && !opciones.Value.CuponRepetible)
            return Results.Problem(statusCode: 409, title: "El pedido ya uso un cupon");
        pedido.CuponAplicado = true;
        pedido.Total = Math.Round(pedido.Total * 0.9m, 2);
        return Results.Ok(new PedidoDto(pedido.Id, pedido.Total, pedido.Estado));
    }
});

// --- Catalogo (inyeccion API-entrada y consumo de recursos: API4) ---
api.MapGet("/productos", (string? nombre, int? pagina, int? limite, Catalogo catalogo, IOptions<LabOpciones> opciones) =>
{
    if (nombre is { Length: > 50 }) return Results.Problem(statusCode: 400, title: "Busqueda demasiado larga");
    var tamano = opciones.Value.SinTopeDePagina ? limite ?? 20 : Math.Clamp(limite ?? 20, 1, 50);
    var pag = Math.Max(pagina ?? 1, 1);
    var items = opciones.Value.SqlConcatenado
        ? catalogo.BuscarConcatenado(nombre ?? "", tamano, pag)
        : catalogo.Buscar(nombre ?? "", tamano, pag);
    return Results.Ok(new { pagina = pag, limite = tamano, items });
});

// --- Vistas previas: la API visita una URL que le da el cliente (SSRF, API7) ---
api.MapPost("/vistas-previas", (VistaSolicitud s, IOptions<VistasOpciones> vistas, IOptions<LabOpciones> opciones) =>
{
    var permitido = opciones.Value.SinListaDeDestinos
        ? Uri.TryCreate(s.Url, UriKind.Absolute, out _)
        : Destinos.EsPermitido(s.Url, vistas.Value.HostsPermitidos);
    // Aqui, ya validado, iria el HttpClient (sin seguir redirecciones y con tiempo limite).
    return permitido ? Results.Accepted(null, new { destino = s.Url }) : Results.Problem(statusCode: 422, title: "Destino no permitido");
});

// --- Funciones de administracion (API5) ---
var usuarios = api.MapGet("/admin/usuarios", (Almacen datos) =>
    datos.Usuarios.Select(u => new PerfilDto(u.Id, u.Nombre, u.Rol)));
if (!lab.Bfla) usuarios.RequireAuthorization("Admin"); // VULNERABLE si se omite: queda con la politica por omision (solo sesion)

api.MapGet("/reportes/resumen", (IOptions<LabOpciones> opciones) =>
{
    if (opciones.Value.ProvocarFalla)
        throw new InvalidOperationException("No se pudo abrir la base: Host=db.interna;Password=ejemplo-no-real");
    return Results.Ok(new { pedidos = 3 });
}).RequireAuthorization("Admin");

// --- Una ruta de depuracion que nadie documento (API9) ---
if (lab.RutaOlvidada) app.MapGet("/api/v0/debug/estado", () => "ok").AllowAnonymous();

app.Run();

public sealed record LoginSolicitud(string Usuario, string Clave);
public sealed record PedidoDto(int Id, decimal Total, string Estado);
public sealed record PerfilDto(int Id, string Nombre, string Rol);
public sealed record PerfilCambio(string? Nombre);
public sealed record CuponSolicitud(string Codigo);
public sealed record VistaSolicitud(string Url);

public partial class Program;
