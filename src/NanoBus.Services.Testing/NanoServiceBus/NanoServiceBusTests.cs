using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NanoBus.Services.Testing.NanoServiceBus
{
    [TestClass]
    public class NanoServiceBusTests
    {
        [TestMethod]
        public async Task NanoServiceBus_AddHandler_Then_HandleMessage()
        {
            await RunTest(new[] { Guid.Empty },
                new[] { 1 },
                new[] { 1 }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NanoServiceBus_AddHandlers_Then_HandleMessage()
        {
            await RunTest(new[] { Guid.Empty },
                new[] { 10 },
                new[] { 10000 }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NanoServiceBus_AddDifferentHandlers_Then_HandleMessage()
        {
            await RunTest(new[] { Guid.Empty, Guid.NewGuid(), Guid.NewGuid() },
                new[] { 10, 10, 10 },
                new[] { 10000, 20000, 30000 }).ConfigureAwait(false);
        }

        private static async Task RunTest(Guid[] documentIds, int[] numberOfPublishMessages, int[] numberOfHandlers)
        {
            var numberOfMessages = new long[documentIds.Length];
            for (var i = 0; i < documentIds.Length; i++)
            {
                if (documentIds[i] == Guid.Empty)
                    numberOfMessages[i] = numberOfPublishMessages.Sum() * numberOfHandlers[i];
                else
                    numberOfMessages[i] = numberOfPublishMessages[i] * numberOfHandlers[i];
            }
            var totalNumberOfMessages = numberOfMessages.Sum();

            var documentTypeName = "documentTypeName";
            var messageTypeName = "messageTypeName";

            long actualTotalNumberOfMessages = 0;
            long[] actualNumberOfMessages = new long[documentIds.Length];

            var timeout = TimeSpan.FromSeconds(10);

            using (var serviceBus = new Service.NanoServiceBus.NanoServiceBus())
            {
                var @event = new ManualResetEventSlim(false);

                for (var i = 0; i < documentIds.Length; i++)
                {
                    var nanoDocumentId = documentIds[i] == Guid.Empty
                        ? NanoDocumentId.Empty
                        : new NanoDocumentId(documentIds[i]);

                    for (var j = 0; j < numberOfHandlers[i]; j++)
                    {
                        var index0 = i;
                        serviceBus.GetEventBus(documentTypeName)
                            .GetMessageBus(messageTypeName)
                            .AddHandler(Guid.NewGuid(), nanoDocumentId, async m =>
                            {
                                Interlocked.Increment(ref actualTotalNumberOfMessages);
                                Interlocked.Increment(ref actualNumberOfMessages[index0]);
                                if (Interlocked.Read(ref actualTotalNumberOfMessages) == totalNumberOfMessages)
                                    @event.Set();
                            });
                    }
                }


                var sw = Stopwatch.StartNew();
                sw.Start();
                for (var i = 0; i < numberOfPublishMessages.Length; i++)
                {
                    var nanoDocumentId = documentIds[i] == Guid.Empty
                        ? NanoDocumentId.Empty
                        : new NanoDocumentId(documentIds[i]);

                    for (var j = 0; j < numberOfPublishMessages[i]; j++)
                    {
                        await serviceBus.GetEventBus(documentTypeName)
                            .GetMessageBus(messageTypeName)
                            .HandleMessage(Guid.NewGuid(), new NanoPublishMessage
                            {
                                DocumentTypeName = documentTypeName,
                                MessageTypeName = messageTypeName,
                                NanoDocumentId = nanoDocumentId
                            }).ConfigureAwait(false);
                    }
                }


                if (!@event.Wait(timeout))
                {
                    sw.Stop();
                    Assert.Fail("Test run failed to complete in {0} seconds", timeout.TotalSeconds);
                }

                Assert.AreEqual(totalNumberOfMessages, actualTotalNumberOfMessages);
                for (int i = 0; i < documentIds.Length; i++)
                {
                    Assert.AreEqual(numberOfMessages[i], actualNumberOfMessages[i]);
                }

                sw.Stop();
                var msgPerSec = actualTotalNumberOfMessages / sw.Elapsed.TotalSeconds;
                Console.WriteLine("{0} messages in {1} seconds", actualTotalNumberOfMessages.ToString("N"),
                    sw.Elapsed.TotalSeconds);
                Console.WriteLine("{0} msg / second", msgPerSec.ToString("N"));
                Console.WriteLine();
            }
        }
    }
}