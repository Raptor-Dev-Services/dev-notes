using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiSeguridad;

public sealed class VistasOpciones
{
    public List<string> HostsPermitidos { get; set; } = ["imagenes.ejemplo.test"];
}

/// <summary>Errores no controlados: el detalle va a la bitacora, al cliente solo un identificador para soporte (RFC 9457).</summary>
public sealed class ManejadorDeErrores(IOptions<LabOpciones> lab, ILogger<ManejadorDeErrores> bitacora, IProblemDetailsService problemas)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        bitacora.LogError(excepcion, "Error no controlado. TraceId {TraceId}", contexto.TraceIdentifier);
        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno",
            // Solo el laboratorio devuelve la excepcion tal cual, para que la prueba de fuga se vea fallar.
            Detail = lab.Value.ErroresDetallados ? excepcion.ToString() : null,
        };
        problema.Extensions["traceId"] = contexto.TraceIdentifier;
        return await problemas.TryWriteAsync(new ProblemDetailsContext { HttpContext = contexto, ProblemDetails = problema });
    }
}

public static class Infraestructura
{
    public const int MaximoCuerpoBytes = 64 * 1024;

    public static IServiceCollection AgregarLimites(this IServiceCollection servicios) =>
        servicios.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Una particion por cliente: sin ella, un solo cliente agota el permiso de todos.
            o.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.OnRejected = async (contexto, ct) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    contexto.HttpContext.Response.Headers.RetryAfter =
                        ((long)Math.Ceiling(espera.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
                contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await contexto.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails { Status = 429, Title = "Demasiadas solicitudes" },
                    options: null, contentType: "application/problem+json", ct);
            };
        });

    /// <summary>Cabeceras de una respuesta de API: nada de interpretar el contenido, nada de cache de datos privados, nada de marcos.</summary>
    public static IApplicationBuilder UseCabecerasDeApi(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var h = contexto.Response.Headers;
                h["X-Content-Type-Options"] = "nosniff";
                h["Cache-Control"] = "no-store";
                h["Content-Security-Policy"] = "frame-ancestors 'none'";
                return Task.CompletedTask;
            });
            await siguiente();
        });

    /// <summary>Rechaza de entrada el cuerpo mas grande de lo razonable. Kestrel tiene su propio tope; este es el de la aplicacion.</summary>
    public static IApplicationBuilder UseTopeDeCuerpo(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            if (contexto.Request.ContentLength > MaximoCuerpoBytes)
            {
                await Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Cuerpo demasiado grande")
                    .ExecuteAsync(contexto);
                return;
            }
            await siguiente();
        });
}
