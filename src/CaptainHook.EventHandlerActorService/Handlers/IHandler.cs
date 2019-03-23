using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptainHook.EventHandlerActorService.Handlers
{
    public interface IHandler
    {
        Task Call<TRequest>(TRequest request, IDictionary<string, object> metaData = null);
    }
}
