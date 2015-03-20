using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    /// <summary>
    /// http://eeichinger.blogspot.hu/2009/01/thoughts-on-systemdiagnostics-trace-vs.html
    /// </summary>
    public static class TraceSourceExtensions
    {
        public static void TraceEvent(this TraceSource traceSource, TraceEventType eventType, int id, Func<string> format, params object[] args)
        {
            if (traceSource.Switch.ShouldTrace(eventType))
            {
                traceSource.TraceEvent(eventType, id, format(), args);
            }
        }
    }
}
