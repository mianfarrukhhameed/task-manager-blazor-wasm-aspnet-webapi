using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fistix.TaskManager.WebApp.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fistix.TaskManager.WebApp.Extentions
{
  public static class ServiceCollectionExtentions
  {
    public static void SetupAuth0Service(
      this IServiceCollection services,
      IConfigurationRoot configurationRoot,
      string baseAddress)
    {
      Auth0Config auth0Config = configurationRoot.GetSection("Auth0").Get<Auth0Config>();

      services.AddSingleton(auth0Config);

      var normalizedBase = baseAddress.EndsWith('/') ? baseAddress : $"{baseAddress}/";
      var logoutCallback = normalizedBase;

      services.AddOidcAuthentication(options =>
      {
        options.ProviderOptions.Authority = auth0Config.Authority;
        options.ProviderOptions.ClientId = auth0Config.ClientId;
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.ResponseMode = "query";
        options.ProviderOptions.RedirectUri = $"{normalizedBase}authentication/login-callback";
        options.ProviderOptions.PostLogoutRedirectUri = logoutCallback;

        // Ensure expected OpenID scopes are explicitly requested.
        if (!options.ProviderOptions.DefaultScopes.Contains("openid"))
          options.ProviderOptions.DefaultScopes.Add("openid");
        if (!options.ProviderOptions.DefaultScopes.Contains("profile"))
          options.ProviderOptions.DefaultScopes.Add("profile");
        if (!options.ProviderOptions.DefaultScopes.Contains("email"))
          options.ProviderOptions.DefaultScopes.Add("email");

        // API identifier belongs in audience only — not in scope.
        if (!string.IsNullOrWhiteSpace(auth0Config.Audience))
          options.ProviderOptions.AdditionalProviderParameters["audience"] = auth0Config.Audience;

        // Optional custom API permissions (e.g. "read:todos write:todos"), not the API identifier.
        if (!string.IsNullOrWhiteSpace(auth0Config.Scope)
            && !string.Equals(auth0Config.Scope, auth0Config.Audience, StringComparison.Ordinal))
        {
          foreach (var apiScope in auth0Config.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
          {
            if (!options.ProviderOptions.DefaultScopes.Contains(apiScope))
              options.ProviderOptions.DefaultScopes.Add(apiScope);
          }
        }
      });
    }

    public static void SetupDefaultApiClient(this IServiceCollection services, IConfigurationRoot configurationRoot)
    {
      var apiConfig = configurationRoot.GetSection("Api").Get<ApiConfig>();
      services.AddSingleton(apiConfig);

      services.AddScoped<CustomAuthorizationMessageHandler>();

      services.AddHttpClient("defaultApi", client => client.BaseAddress = new Uri(apiConfig.Url))
          .AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

      services.AddScoped(sp => sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("defaultApi"));
    }


  }
}
