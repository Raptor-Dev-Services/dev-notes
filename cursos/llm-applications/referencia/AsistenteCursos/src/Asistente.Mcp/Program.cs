using Asistente.Mcp;

var builder = WebApplication.CreateBuilder(args);

var huella = builder.Configuration["MCP_TOKEN_SHA256"];
if (string.IsNullOrWhiteSpace(huella) || huella.Length != 64 || !huella.All(Uri.IsHexDigit))
    throw new InvalidOperationException("Define MCP_TOKEN_SHA256 con el SHA-256 (64 caracteres hexadecimales) del token.");

builder.Services.AddSingleton(Catalogo.Cargar(Path.Combine(AppContext.BaseDirectory, "datos", "catalogo.json")));
builder.Services
    .AddAuthentication(AutenticacionPorToken.Esquema)
    .AddScheme<OpcionesToken, AutenticacionPorToken>(AutenticacionPorToken.Esquema, o => o.HuellaDelToken = huella);
builder.Services.AddAuthorization();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<HerramientasCatalogo>()
    .WithResources<RecursosCatalogo>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();   // sin credencial válida: 401 antes de tocar el protocolo
app.Run();

public partial class Program;
