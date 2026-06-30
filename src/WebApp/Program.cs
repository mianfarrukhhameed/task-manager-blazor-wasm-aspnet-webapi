using Fistix.TaskManager.ViewModel.Validators.Todos;
using Fistix.TaskManager.WebApp.Extentions;
using Fistix.TaskManager.WebApp.Services.DataServices;
using Fistix.TaskManager.WebApp.Services.StateServices;
using FluentValidation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApp
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);
      builder.RootComponents.Add<App>("#app");

      // AuthorizeView logs anonymous checks at Information; that is expected during login transitions.
      builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning);

      ConfigureServices(builder.Services, builder.Configuration, builder.HostEnvironment.BaseAddress);

      await builder.Build().RunAsync();
    }

    public static void ConfigureServices(
      IServiceCollection services,
      IConfigurationRoot configuration,
      string baseAddress)
    {
      services.AddValidatorsFromAssemblyContaining<CreateTodoTaskCommandValidator>();

      services.SetupAuth0Service(configuration, baseAddress);
      services.SetupDefaultApiClient(configuration);
      //services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
      
      services.AddScoped<TodoDataService>();
      services.AddScoped<TodoStateService>();
    }
  }
}
