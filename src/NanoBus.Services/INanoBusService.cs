using System.Threading;
using System.Threading.Tasks;

namespace NanoBus.Service
{
    public interface INanoBusService
    {
        Task RunAsync(string httpListenerPrefix, CancellationToken cancellationToken);
    }
}