using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VetClinic.Api.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Excepción no controlada tras iniciar la respuesta para {Path}", context.Request.Path);
                throw;
            }

            var (status, message) = ex switch
            {
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, ex.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "El registro fue modificado por otra operación. Recargá los datos e intentá de nuevo."),
                DbUpdateException { InnerException: ArgumentException inner } => (StatusCodes.Status400BadRequest, inner.Message),
                DbUpdateException => (StatusCodes.Status409Conflict, "No se pudo guardar la información: registro duplicado o inválido."),
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
            };

            _logger.LogError(ex, "Excepción {Type} durante {Method} {Path} -> {Status}",
                ex.GetType().Name, context.Request.Method, context.Request.Path, status);

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { message }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}