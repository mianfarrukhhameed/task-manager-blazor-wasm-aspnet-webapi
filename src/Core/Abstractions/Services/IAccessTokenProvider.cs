using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.Core.Abstractions.Services
{
  public interface IAccessTokenProvider
  {
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
  }
}
