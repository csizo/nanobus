using System;
using System.Collections.Generic;

namespace NanoBus.Service.NanoServiceBus
{
    internal class NanoEventBus : IDisposable
    {
        private readonly string _documentTypeName;
        private readonly NanoServiceBus _nanoServiceBus;
        private readonly Dictionary<string, NanoMessageBus> _nanoMessageBusses = new Dictionary<string, NanoMessageBus>();

        public NanoEventBus(NanoServiceBus nanoServiceBus, string documentTypeName)
        {
            _nanoServiceBus = nanoServiceBus;
            _documentTypeName = documentTypeName;
        }

        internal Dictionary<string, NanoMessageBus> NanoMessageBusses
        {
            get { return _nanoMessageBusses; }
        }

        public string DocumentTypeName
        {
            get { return _documentTypeName; }
        }

        public void Dispose()
        {
            NanoEventBus nanoEventBus;
            _nanoServiceBus.EventBusses.TryRemove(_documentTypeName, out nanoEventBus);
        }

        public NanoMessageBus GetMessageBus(string eventTypeName)
        {
            return _nanoMessageBusses.GetOrAdd(eventTypeName, typeName => new NanoMessageBus(typeName, this));
        }


    }
}