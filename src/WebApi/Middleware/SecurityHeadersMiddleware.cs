using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Middleware
{
  public class SecurityHeadersMiddleware
  {
    private const string StrictCsp =
      "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    // Swagger UI needs scripts/styles from self (and inline) to render; keep frame-ancestors locked down.
    private const string SwaggerCsp =
      "default-src 'self'; " +
      "script-src 'self' 'unsafe-inline'; " +
      "style-src 'self' 'unsafe-inline'; " +
      "img-src 'self' data:; " +
      "font-src 'self'; " +
      "connect-src 'self'; " +
      "frame-ancestors 'none'; " +
      "base-uri 'self'; " +
      "form-action 'self'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var headers = context.Response.Headers;

      headers["X-Content-Type-Options"] = "nosniff";
      headers["X-Frame-Options"] = "DENY";
      headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
      headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
      headers["Content-Security-Policy"] = IsSwaggerPath(context.Request.Path)
        ? SwaggerCsp
        : StrictCsp;
      headers.Remove("Server");
      headers.Remove("X-Powered-By");

      await _next(context);
    }

    private static bool IsSwaggerPath(PathString path) =>
      path.StartsWithSegments("/swagger");
  }
}
