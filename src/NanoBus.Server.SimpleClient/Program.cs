using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NanoBus.Client;

namespace NanoBus.Server.SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {


            var n = new NanoBusClient();
            n.ConnectAsync(new Uri("ws://localhost:8091/NanoBus"), 1).Wait();
            List<IDisposable> subscriptionList = new List<IDisposable>(3);
            var x = n.GetNanoEventBus<SampleDocument>();
            subscriptionList.Add(x.Subscribe<SampleDocument.Messages.Created>(async m => { Console.WriteLine(m); }));
            subscriptionList.Add(x.Subscribe<SampleDocument.Messages.Updated>(async m => { Console.WriteLine(m); }));
            subscriptionList.Add(x.Subscribe<SampleDocument.Messages.Deleted>(async m => { Console.WriteLine(m); }));


            while (true)
            {
                var doc = new SampleDocument() { DocumentId = Guid.NewGuid() };
                x.PublishAsync(new SampleDocument.Messages.Created(doc)).Wait();
                x.PublishAsync(new SampleDocument.Messages.Updated(doc)).Wait();
                x.PublishAsync(new SampleDocument.Messages.Deleted(doc)).Wait();
            }

        }
    }

    class SampleDocument : IDomainDocument
    {
        public Guid DocumentId { get; set; }

        public Guid GetDocumentId()
        {
            return DocumentId;
        }

        public static class Messages
        {
            public class Created : DomainMessage<SampleDocument>
            {
                public Created(SampleDocument doc)
                    : base(doc)
                {

                }
                public override string ToString()
                {
                    return string.Concat("Created", this.DocumentId);
                }
            }
            public class Updated : DomainMessage<SampleDocument>
            {
                public Updated(SampleDocument doc)
                    : base(doc)
                {

                }
                public override string ToString()
                {
                    return string.Concat("Updated", this.DocumentId);
                }
            }
            public class Deleted : DomainMessage<SampleDocument>
            {
                public Deleted(SampleDocument doc)
                    : base(doc)
                {

                }
                public override string ToString()
                {
                    return string.Concat("Deleted", this.DocumentId);
                }
            }
        }
    }


}
