using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NanoBus.Client
{
    internal class NanoConnection : IDisposable
    {
        private static readonly TraceSource Trace = new TraceSource("Framework.Runtime.Azure.Messaging.NanoBus");
        private readonly int _bufferSize;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly NanoBusClient _nanoBusClient;
        private readonly ConcurrentQueue<NanoMessage> _txQueue = new ConcurrentQueue<NanoMessage>();
        private readonly Uri _uri;
        private readonly ClientWebSocket _webSocket;
        private long _rxCount;
        private ManualResetEventSlim _txEvent;

        public NanoConnection(NanoBusClient nanoBusClient, Uri uri, int bufferSize = 8192)
        {
            _nanoBusClient = nanoBusClient;
            _uri = uri;
            _bufferSize = bufferSize;

            _webSocket = new ClientWebSocket();
        }

        public long TxCount { get; private set; }

        public long RxCount
        {
            get { return _rxCount; }
        }

        public NanoBusClient NanoBusClient
        {
            get { return _nanoBusClient; }
        }

        public void Dispose()
        {
            DisconnectAsync();

            lock (_nanoBusClient.NanoConnections)
            {
                _nanoBusClient.NanoConnections.Remove(this);
            }
        }


        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await _webSocket.ConnectAsync(_uri, cancellationToken);

            _txEvent = new ManualResetEventSlim(true); 
            Task.Run(() => ReceiveAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            Task.Run(() => TransmitAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
           


        }

        private async Task DisconnectAsync()
        {
            _cancellationTokenSource.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
        }

        public void EnqueueMessage(NanoMessage nanoMessage)
        {
            _txQueue.Enqueue(nanoMessage);
            _txEvent.Set();
        }

        /// <summary>
        ///     Receives from socket asynchronous.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ReceiveAsync(CancellationToken token)
        {
            var buffer = new ArraySegment<byte>(new Byte[_bufferSize]);

            while (_webSocket.State == WebSocketState.Open)
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        try
                        {
                            result = await _webSocket.ReceiveAsync(buffer, token);
                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    break;
                                case WebSocketMessageType.Binary:
                                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                                    break;
                                case WebSocketMessageType.Close:
                                    return;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        catch (WebSocketException e)
                        {
                            //log error and exit
                            return;
                        }
                    } while (!result.EndOfMessage);


                    var bytes = ms.ToArray();
                    Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "Rx {0} bytes", bytes.Length.ToString());

                    if (bytes.Length > 0)
                        ProcessMessageAsync(bytes, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Processes the message asynchronous.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task ProcessMessageAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            NanoMessageBatch nanoMessageBatch;
            using (var ms = new MemoryStream(bytes))
            {
                nanoMessageBatch = NanoMessageBatch.Deserialize(ms);
            }

            var tasks = new List<Task>(nanoMessageBatch.NanoMessages.Count);
            tasks.AddRange(nanoMessageBatch.NanoMessages.Select(OnReceivedNanoMessageAsync));

            return TaskHelpers.Iterate(tasks, cancellationToken);
        }

        private async Task OnReceivedNanoMessageAsync(NanoMessage nanoMessage)
        {
            Interlocked.Increment(ref _rxCount);

            if (nanoMessage.PublishMessage != null)
            {
                await OnReceivedPublishMessageAsync(nanoMessage.PublishMessage);
            }

            if (nanoMessage.IsAckRequested)
            {
                EnqueueMessage(nanoMessage.CreateAckMessage(0));
            }
        }

        private Task OnReceivedPublishMessageAsync(NanoPublishMessage publishMessage)
        {
            Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "OnReceivedPublishMessageAsync");

            return NanoBusClient.DistributePublishMessageAsync(publishMessage);
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
                        try
                        {
                            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, i == segments.Count - 1,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (WebSocketException e)
                        {
                            //TODO: log error and exit
                            return;
                        }


                        tx += segment.Count;
                    }

                    Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "Tx {0} bytes", tx.ToString());
                    TxCount += nanoMessageBatch.NanoMessages.Count;
                }
            }
        }

    }
}