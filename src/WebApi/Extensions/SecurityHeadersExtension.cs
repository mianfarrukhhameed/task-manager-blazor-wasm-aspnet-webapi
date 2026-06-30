using Fistix.TaskManager.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Fistix.TaskManager.WebApi.Extensions
{
  public static class SecurityHeadersExtension
  {
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
      return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
  }
}
