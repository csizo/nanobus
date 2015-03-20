using System;
using System.IO;
using ProtoBuf;
using ServiceStack.Text;

namespace NanoBus
{
    [ProtoContract]
    public class NanoPublishMessage
    {
        [ProtoMember(1, Name = "doctype")]
        public string DocumentTypeName { get; set; }

        [ProtoMember(2, Name = "msgtype")]
        public string MessageTypeName { get; set; }

        [ProtoMember(3, Name = "docid")]
        public NanoDocumentId NanoDocumentId { get; set; }

        [ProtoMember(4, Name = "payload")]
        public byte[] DocumentPayload { get; set; }


        public NanoMessage ToNewPublishMessage()
        {
            return new NanoMessage()
            {
                OperationId = Guid.NewGuid(),
                IsAckRequested = false,
                PublishMessage = new NanoPublishMessage
                {
                    DocumentTypeName = DocumentTypeName,
                    MessageTypeName = MessageTypeName,
                    NanoDocumentId = NanoDocumentId,
                    DocumentPayload = DocumentPayload,
                },
            };
        }

        public object DeseralizePayload(Type payloadType)
        {
            using (var ms = new MemoryStream(DocumentPayload, false))
            {
                return JsonSerializer.DeserializeFromStream(payloadType, ms);
            }
        }

        internal static byte[] SerializePayload(object payload)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                JsonSerializer.SerializeToStream(payload, payload.GetType(), ms);

                bytes = ms.ToArray();
            }
            return bytes;
        }
    }
}