using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.System.IO;
using System.Threading;
using System.Threading.Tasks;
using NanoBus.Service.NanoServiceBus;

namespace NanoBus.Service
{
    internal class NanoSession : IDisposable
    {
        private static readonly TraceSource Trace = new TraceSource("NanoBusServer");
        private readonly int _bufferSize;

        private readonly ConcurrentDictionary<Guid, NanoHandler> _handlers = new ConcurrentDictionary<Guid, NanoHandler>();

        private readonly NanoBusService _nanoBusService;
        private readonly Guid _sessionId;
        private readonly ConcurrentQueue<NanoMessage> _txQueue = new ConcurrentQueue<NanoMessage>();
        private readonly WebSocket _webSocket;
        private long _rxCount;
        private ManualResetEventSlim _txEvent;

        public NanoSession(NanoBusService nanoBusService, WebSocket webSocket, int bufferSize = 8192)
        {
            _sessionId = Guid.NewGuid();
            _nanoBusService = nanoBusService;
            _webSocket = webSocket;
            _bufferSize = bufferSize;
        }

        public Guid SessionId
        {
            get { return _sessionId; }
        }

        public long TxCount { get; private set; }

        public long RxCount
        {
            get { return _rxCount; }
        }

        public ConcurrentDictionary<Guid, NanoHandler> Handlers
        {
            get { return _handlers; }
        }

        public void Dispose()
        {
            foreach (var handler in _handlers.Values)
            {
                handler.Dispose();
            }

            NanoSession nanoSession;
            _nanoBusService.Sessions.TryRemove(_sessionId, out nanoSession);
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            _txEvent = new ManualResetEventSlim(true);

            Task.Run(() => ReceiveAsync(cancellationToken).ConfigureAwait(false), cancellationToken)
                .ConfigureAwait(false);
            await Task.Run(() => TransmitAsync(cancellationToken).ConfigureAwait(false), cancellationToken)
                   .ConfigureAwait(false);

        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new ArraySegment<byte>(new Byte[_bufferSize]);


                while (_webSocket.State == WebSocketState.Open)
                {
                    using (var ms = MemoryStreamPool.GetStream("nanoSession"))
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    break;
                                case WebSocketMessageType.Binary:
                                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                                    break;
                                case WebSocketMessageType.Close:
                                    await
                                        _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "",
                                            CancellationToken.None);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        } while (!result.EndOfMessage);


                        var bytes = ms.ToArray();
                        Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "Rx {0} bytes", bytes.Length.ToString());

                        if (bytes.Length > 0)
                            ProcessMessageAsync(bytes, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceEvent(TraceEventType.Error, 2003, () => "Exception: {0}", e);
            }
            finally
            {
            }
        }

        private async Task TransmitAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _txEvent.Wait(cancellationToken);
                _txEvent.Reset();

                NanoMessage nanoMessage;
                while (_txQueue.TryDequeue(out nanoMessage))
                {
                    var nanoMessageBatch = new NanoMessageBatch();
                    nanoMessageBatch.NanoMessages.Add(nanoMessage);

                    var segments = nanoMessageBatch.Serialize().ToArraySegments(_bufferSize);

                    var tx = 0;
                    for (var i = 0; i < segments.Count; i++)
                    {
                        var segment = segments[i];
                        await
                            _webSocket.SendAsync(segment, WebSocketMessageType.Binary, i == segments.Count - 1,
                                cancellationToken).ConfigureAwait(false);

                        tx += segment.Count;
                    }

                    Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "Tx {0} bytes", tx.ToString());
                    TxCount += nanoMessageBatch.NanoMessages.Count;
                }
            }
        }


        public Task ProcessMessageAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            NanoMessageBatch nanoMessageBatch;
            using (var ms = new MemoryStream(bytes))
            {
                nanoMessageBatch = NanoMessageBatch.Deserialize(ms);
            }

            var tasks = new List<Task>(nanoMessageBatch.NanoMessages.Count);
            tasks.AddRange(nanoMessageBatch.NanoMessages.Select(OnNanoMessageAsync));

            return TaskHelpers.Iterate(tasks, cancellationToken);
        }

        private async Task OnNanoMessageAsync(NanoMessage nanoMessage)
        {
            Interlocked.Increment(ref _rxCount);

            if (nanoMessage.PublishMessage != null)
            {
                await PublishAsync(nanoMessage.ClientId, nanoMessage.PublishMessage).ConfigureAwait(false);
            }
            if (nanoMessage.SubscribeMessage != null)
            {
                Subscribe(nanoMessage.ClientId, nanoMessage.SubscribeMessage);
            }

            if (nanoMessage.IsAckRequested)
            {
                EnqueueMessage(nanoMessage.CreateAckMessage(0));
            }
        }

        private void EnqueueMessage(NanoMessage nanoMessage)
        {
            _txQueue.Enqueue(nanoMessage);
            _txEvent.Set();
        }

        private Task EnqueueMessageAsync(NanoMessage message)
        {
            EnqueueMessage(message);
            return TaskHelpers.Completed();
        }

        private void Subscribe(Guid clientId, NanoSubscribeMessage subscribeMessage)
        {
            var subsciption = _nanoBusService
                .NanoServiceBus
                .GetEventBus(subscribeMessage.DocumentTypeName)
                .GetMessageBus(subscribeMessage.MessageTypeName)
                .AddHandler(clientId, subscribeMessage.NanoDocumentId, EnqueueMessageAsync);

            _handlers.TryAdd(subsciption.HandlerId, subsciption);
        }

        private Task PublishAsync(Guid clientId, NanoPublishMessage publishMessage)
        {
            return _nanoBusService
                .NanoServiceBus
                .GetEventBus(publishMessage.DocumentTypeName)
                .GetMessageBus(publishMessage.MessageTypeName)
                .HandleMessage(clientId, publishMessage);
        }
    }
}