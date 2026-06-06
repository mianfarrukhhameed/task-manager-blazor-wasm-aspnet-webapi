using AutoMapper;
using Fistix.TaskManager.Core.Config;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Fistix.TaskManager.Core.AutoMapperProfiles;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ServiceLayer.Todos;
using Fistix.TaskManager.DataLayer;

namespace Fistix.TaskManager.ServiceLayer
{
  public static class StartUp
  {
    public static void AddServiceLayer(this IServiceCollection services, MasterConfig masterConfig)
    {
      services.AddMediatR(typeof(CreateTodoTaskCommand).Assembly, typeof(CreateTodoTaskCommandHandler).Assembly);

      services.AddAutoMapper(x=>x.AddProfile<TodoTaskProfileMapping>());
            
      services.AddDataLayer(masterConfig);
    }
  }
}
