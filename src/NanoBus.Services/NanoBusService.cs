using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NanoBus.Service
{
    public interface INanoBusServiceManager : IDisposable
    {
        INanoBusService NanoBusService { get; }
        Task StartAsync(string httpListenerPrefix);
        void Stop();
    }

    public class NanoBusServiceManager : INanoBusServiceManager
    {
        private readonly INanoBusService _nanoBusService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        public NanoBusServiceManager(INanoBusService nanoBusService)
        {
            _nanoBusService = nanoBusService;
        }

        public INanoBusService NanoBusService
        {
            get { return _nanoBusService; }
        }

        public Task StartAsync(string httpListenerPrefix)
        {
            if (_cancellationTokenSource != null) return TaskHelpers.Completed();

            _cancellationTokenSource = new CancellationTokenSource();
            return Task.Run(() => _nanoBusService.RunAsync(httpListenerPrefix, _cancellationTokenSource.Token).ConfigureAwait(false));
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }


        public void Dispose()
        {
            if (_isDisposed) return;
            Stop();
            _isDisposed = true;
        }
    }
    public class NanoBusService : INanoBusService
    {
        private static readonly TraceSource Trace = new TraceSource("NanoBusServer");

        private readonly NanoServiceBus.NanoServiceBus _nanoNanoServiceBus;
        private readonly ConcurrentDictionary<Guid, NanoSession> _sessions = new ConcurrentDictionary<Guid, NanoSession>();
        private HttpListener _listener;


        public NanoBusService()
        {
            //TODO: implement nano bus server farm mode
            _nanoNanoServiceBus = new NanoServiceBus.NanoServiceBus();
        }

        internal ConcurrentDictionary<Guid, NanoSession> Sessions
        {
            get { return _sessions; }
        }

        internal NanoServiceBus.NanoServiceBus NanoServiceBus
        {
            get { return _nanoNanoServiceBus; }
        }


        public async Task RunAsync(string httpListenerPrefix, CancellationToken cancellationToken)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(httpListenerPrefix);
            _listener.Start();

            await ReceiveContextAsync(cancellationToken).ConfigureAwait(false);
            
            _listener.Stop();
        }

        private async Task ReceiveContextAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    if (context.Request.IsWebSocketRequest)
                        Task.Run(() => HandleContextAsync(context, cancellationToken).ConfigureAwait(false), cancellationToken);
                        //HandleContextAsync(context, cancellationToken).ConfigureAwait(false);
                    else
                    {
                        context.Response.StatusCode = 426; //426 - Upgrade Required Status Code
                        context.Response.Close();
                    }
                }
            }
            catch (System.Net.HttpListenerException e)
            {
                //Log receiver error
            }
        }


        /// <summary>
        ///     Handles the web socket connection.
        /// </summary>
        /// <param name="httpListenerContext">The HTTP listener context.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private async Task HandleContextAsync(HttpListenerContext httpListenerContext, CancellationToken cancellationToken)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null).ConfigureAwait(false);
                var ipAddress = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
                Trace.TraceEvent(TraceEventType.Information, 2000, () => "Connected: IPAddress {0}", ipAddress);
            }
            catch (Exception e)
            {
                httpListenerContext.Response.StatusCode = 500;
                httpListenerContext.Response.Close();
                Trace.TraceEvent(TraceEventType.Error, 2001, () => "Connection error: {0}", e);
                return;
            }

            var webSocket = webSocketContext.WebSocket;

            var session = new NanoSession(this, webSocket);

            _sessions.TryAdd(session.SessionId, session);
            await session.ProcessMessagesAsync(cancellationToken).ConfigureAwait(false);
            _sessions.TryRemove(session.SessionId, out session);
            webSocket.Dispose();

        }



    }
}