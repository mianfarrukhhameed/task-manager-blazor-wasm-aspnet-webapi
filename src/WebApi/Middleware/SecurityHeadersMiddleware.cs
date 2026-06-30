using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Middleware
{
  public class SecurityHeadersMiddleware
  {
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
      headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
      headers.Remove("Server");
      headers.Remove("X-Powered-By");

      await _next(context);
    }
  }
}
