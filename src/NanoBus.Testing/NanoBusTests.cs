using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NanoBus.Client;
using NanoBus.Service;

namespace NanoBus.Services
{
    [TestClass]
    public class NanoBusTests
    {
        [TestMethod]
        public async Task NanoBus_SingleClient_Test()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 1;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 1;
            int numberOfSubscriptions = 1;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);
        }

        [TestMethod]
        public async Task NanoBus_SingleClient_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 1;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 1000;
            int numberOfSubscriptions = 1;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);
        }

        [TestMethod]
        public async Task NanoBus_PooledClient_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 1;
            int numberOfClientPoolSize = 5;
            int numberOfPublishedMessages = 1000;
            int numberOfSubscriptions = 1;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }

        [TestMethod]
        public async Task NanoBus_SeveralClient_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 5;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 1000;
            int numberOfSubscriptions = 1;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }
        [TestMethod]
        public async Task NanoBus_SeveralClient_SomeSubscriptions_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 5;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 1000;
            int numberOfSubscriptions = 5;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }

        [TestMethod]
        public async Task NanoBus_SeveralClient_MediumSubscriptions_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 5;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 1000;
            int numberOfSubscriptions = 50;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }
        [TestMethod]
        public async Task NanoBus_SomeClient_DozensOfSubscriptions_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 2;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 10;
            int numberOfSubscriptions = 500000;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }

        [TestMethod]
        public async Task NanoBus_SeveralClient_DozensOfSubscriptions_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 2;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 10;
            int numberOfSubscriptions = 500000;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }
        [TestMethod]
        public async Task NanoBus_LotsOfClients_SomeSubscriptions_PerformanceTest()
        {
            int basePort = 8090;
            int numberOfServers = 1;
            int numberOfClients = 10000;
            int numberOfClientPoolSize = 1;
            int numberOfPublishedMessages = 10;
            int numberOfSubscriptions = 5;

            await TestRun(numberOfServers, numberOfClients, numberOfSubscriptions, numberOfPublishedMessages, basePort, numberOfClientPoolSize);

        }


        private async Task TestRun(int numberOfServers, int numberOfClients, int numberOfSubscriptions, int numberOfPublishedMessages,
            int basePort, int numberOfClientPoolSize)
        {
            long receivedMessagesCount = 0;
            int numberOfHandlers = numberOfServers * numberOfClients * numberOfSubscriptions;
            int numberOfMessages = numberOfPublishedMessages * numberOfHandlers;

            Console.WriteLine("************************");
            Console.WriteLine("numberOfServers: {0}", numberOfServers);
            Console.WriteLine("numberOfClients: {0}", numberOfClients);
            Console.WriteLine("numberOfClientPoolSize: {0}", numberOfClientPoolSize);
            Console.WriteLine("numberOfSubscriptions: {0}", numberOfSubscriptions);
            Console.WriteLine("numberOfPublishedMessages: {0}", numberOfPublishedMessages);
            Console.WriteLine("numberOfHandlers: {0}", numberOfHandlers);
            Console.WriteLine("numberOfMessages: {0}", numberOfMessages);

            ManualResetEvent ev = new ManualResetEvent(false);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            ConcurrentBag<NanoBusServiceManager> serviceManagers = new ConcurrentBag<NanoBusServiceManager>();
            ConcurrentBag<NanoBusClient> clients = new ConcurrentBag<NanoBusClient>();
            //ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();


            try
            {
                var sw = Stopwatch.StartNew();

                Parallel.For(0, numberOfServers, x =>
                {
                    var service = new NanoBusService();
                    var serviceManager = new NanoBusServiceManager(service);
                    serviceManager.StartAsync(string.Format("http://localhost:{0}/NanoBus/", basePort + x));
                    serviceManagers.Add(serviceManager);

                    Parallel.For(0, numberOfClients, async y =>
                    {
                        var client = new NanoBusClient();
                        await client.ConnectAsync(new Uri(string.Format("ws://localhost:{0}/NanoBus/", basePort + x)), numberOfClientPoolSize).ConfigureAwait(false);
                        clients.Add(client);

                        Parallel.For(0, numberOfSubscriptions, z =>
                        {
                            client.GetNanoEventBus<Session>().Subscribe<SessionStartDomainMessage>(msg =>
                            {
                                Interlocked.Increment(ref receivedMessagesCount);

                                //Console.WriteLine("receive msg client({0}).pool({1}).iteration({2}) [{3}/{4}]", y, z, msg.Iteration, receivedMessagesCount, numberOfMessages);

                                if (Interlocked.Read(ref receivedMessagesCount) == numberOfMessages)
                                    ev.Set();

                                return Task.FromResult(0);
                            });
                        });
                    });
                });


                var started = DateTime.Now;
                //wait for all subscription
                while (serviceManagers.Sum(a => a.NanoBusService.As<NanoBusService>().Sessions.Sum(n => n.Value.Handlers.Count)) != numberOfClients)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    if (DateTime.Now.Subtract(started) > TimeSpan.FromSeconds(30))
                    {
                        Assert.Fail("Failed to connect in 30 seconds");
                    }
                }
                sw.Stop();
                Console.WriteLine("Initialized in {0} seconds", sw.Elapsed.TotalSeconds);

                Measure(() =>
                {
                    for (int i = 0; i < numberOfPublishedMessages; i++)
                    {
                        var msg =
                            new SessionStartDomainMessage(new Session()
                            {
                                Iteration = i,
                                CreatedAt = DateTime.Now,
                                SessionId = Guid.NewGuid()
                            });
                        //Console.WriteLine("send msg iteration({0})", msg.Iteration);
                        clients.TakeRandom().GetNanoEventBus<Session>().PublishAsync(msg).ConfigureAwait(false).GetAwaiter().GetResult();

                    }

                    ev.WaitOne(TimeSpan.FromSeconds(15));




                }, ref receivedMessagesCount);


                //Assert.AreEqual(numberOfPublishedMessages, clients.Sum(a => a.NanoClientConnections.Sum(c => c.TxCount)), "Not all messages sent by clients");
                //Assert.AreEqual(numberOfPublishedMessages, servers.Sum(a => a.Sessions.Sum(c => c.Value.RxCount)), "Not all messages received by servers");
                //Assert.AreEqual(numberOfPublishedMessages * numberOfClients, servers.Sum(a => a.Sessions.Sum(c => c.Value.TxCount)), "Not all messages sent by servers");

                Assert.AreEqual(numberOfMessages, Interlocked.Read(ref receivedMessagesCount));

            }
            finally
            {
                var sw = Stopwatch.StartNew();
                serviceManagers.ToList().ForEach(c => c.Stop());
                clients.ToList().ForEach(c => c.Dispose());
                sw.Stop();
                Console.WriteLine("Cleanup in {0} seconds", sw.Elapsed.TotalSeconds);
            }

        }


        void Measure(Action action, ref long numberOfMessages)
        {
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                action();
            }
            finally
            {
                sw.Stop();
                var msgPerSec = (double)numberOfMessages / (double)sw.Elapsed.TotalSeconds;
                Console.WriteLine("{0} messages in {1} seconds", numberOfMessages.ToString("N"), sw.Elapsed.TotalSeconds);
                Console.WriteLine("{0} msg / second", msgPerSec.ToString("N"));
            }

        }

        public class Session : IDomainDocument
        {
            public Guid SessionId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Iteration { get; set; }

            public Guid GetDocumentId()
            {
                return SessionId;
            }

            public override string ToString()
            {
                return string.Format("Iteration: {0}", Iteration);
            }
        }

        public class SessionStartDomainMessage : DomainMessage<Session>
        {
            public SessionStartDomainMessage()
            {

            }
            public SessionStartDomainMessage(Session document)
                : base(document)
            {
                Iteration = document.Iteration;
            }

            public int Iteration { get; set; }

            public override string ToString()
            {
                return string.Format("Iteration: {0}", Iteration);
            }
        }
    }
}