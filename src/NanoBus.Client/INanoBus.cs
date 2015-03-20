using System;
using System.Threading.Tasks;

namespace NanoBus.Client
{
    public interface INanoBus : IDisposable
    {
        INanoEventBus<T> GetNanoEventBus<T>() where T : IDomainDocument;
        Task ConnectAsync(Uri uri, int poolSize = 1);
    }
}