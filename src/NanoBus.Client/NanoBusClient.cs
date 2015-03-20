using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NanoBus.Client.NanoServiceBus;

namespace NanoBus.Client
{
    public sealed class NanoBusClient : INanoBus
    {
        private static readonly TraceSource Trace = new TraceSource("Framework.Runtime.Azure.Messaging.NanoBus");

        private readonly RoundRobinList<NanoConnection> _nanoConnections = new RoundRobinList<NanoConnection>();
        private readonly ConcurrentDictionary<string, Type> _documentTypeMap = new ConcurrentDictionary<string, Type>();
        private readonly ConcurrentDictionary<Type, INanoEventBus> _nanoEventBusses = new ConcurrentDictionary<Type, INanoEventBus>();

        private readonly Guid _clientId = Guid.NewGuid();

        internal RoundRobinList<NanoConnection> NanoConnections
        {
            get { return _nanoConnections; }
        }

        internal ConcurrentDictionary<string, Type> DocumentTypeMap
        {
            get { return _documentTypeMap; }
        }

        internal ConcurrentDictionary<Type, INanoEventBus> NanoEventBusses
        {
            get { return _nanoEventBusses; }
        }

        public Guid ClientId
        {
            get { return _clientId; }
        }

        public INanoEventBus<T> GetNanoEventBus<T>() where T : IDomainDocument
        {
            return (INanoEventBus<T>)_nanoEventBusses.GetOrAdd(typeof(T), type =>
                 {
                     DocumentTypeMap.TryAdd(type.FullName, type);
                     return new NanoEventBus<T>(this);
                 });
        }


        public void Dispose()
        {
            foreach (var nanoClientConnection in _nanoConnections)
            {
                nanoClientConnection.Dispose();
            }
        }


        public Task ConnectAsync(Uri uri, int poolSize = 1)
        {
            Trace.TraceEvent(TraceEventType.Information, 1000, () => "Connecting PoolSize: {0}", poolSize.ToString());

            List<Task> tasks = new List<Task>(poolSize);

            Parallel.For(0, poolSize, i =>
            {
                var clientConnection = new NanoConnection(this, uri);
                var task = clientConnection.ConnectAsync(CancellationToken.None).Then(() => _nanoConnections.Add(clientConnection));
                tasks.Add(task);
            });
            return TaskHelpers.Iterate(tasks);
        }

        internal Task DistributePublishMessageAsync(NanoPublishMessage publishMessage)
        {
            //distribute nano message local callbacks
            Type documentType;
            if (DocumentTypeMap.TryGetValue(publishMessage.DocumentTypeName, out documentType))
            {
                INanoEventBus nanoEventBus;
                if (NanoEventBusses.TryGetValue(documentType, out nanoEventBus))
                {
                    return nanoEventBus.DistributeAsync(publishMessage);
                }
            }

            throw new InvalidOperationException();
        }
    }
}