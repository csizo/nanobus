using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NanoBus.Service.NanoServiceBus
{
    internal class NanoServiceBus : IDisposable
    {
        private static readonly TraceSource Trace = new TraceSource("NanoBus.Services");
        private readonly Dictionary<string, NanoEventBus> _eventBusses = new Dictionary<string, NanoEventBus>();

        public Dictionary<string, NanoEventBus> EventBusses
        {
            get { return _eventBusses; }
        }

        public void Dispose()
        {
            foreach (var eventBus in _eventBusses.Values.AsSnapshot())
            {
                eventBus.Dispose();
            }
        }

        public NanoEventBus GetEventBus(string name)
        {
            return _eventBusses.GetOrAdd(name, s => new NanoEventBus(this, name));
        }
    }
}