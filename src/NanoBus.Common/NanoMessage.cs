using System;
using ProtoBuf;

namespace NanoBus
{
    [ProtoContract]
    public class NanoMessage
    {
        [ProtoMember(1, Name = "clid")]
        public Guid ClientId { get; set; }

        [ProtoMember(2, Name = "opid")]
        public Guid OperationId { get; set; }

        [ProtoMember(3, Name = "ackreq")]
        public bool IsAckRequested { get; set; }

        [ProtoMember(10, Name = "ack")]
        public NanoAcknowledgeMessage AcknowledgeMessage { get; set; }

        [ProtoMember(20, Name = "pub")]
        public NanoPublishMessage PublishMessage { get; set; }

        [ProtoMember(30, Name = "sub")]
        public NanoSubscribeMessage SubscribeMessage { get; set; }


        public static NanoMessage CreatePublishMessage<TDocument, TMessage>(Guid clientId, TMessage message, bool isAckRequested = false)
            where TDocument : IDomainDocument
            where TMessage : IDomainMessage<TDocument>
        {
            return new NanoMessage
            {
                ClientId = clientId,
                OperationId = Guid.NewGuid(),
                IsAckRequested = isAckRequested,
                PublishMessage = new NanoPublishMessage
                {
                    DocumentTypeName = typeof(TDocument).FullName,
                    MessageTypeName = typeof(TMessage).FullName,
                    NanoDocumentId = new NanoDocumentId(message.DocumentId),
                    DocumentPayload = NanoPublishMessage.SerializePayload(message)
                }
            };
        }

        public static NanoMessage CreateSubscribeMessage<TDocument, TMessage>(Guid clientId, NanoDocumentId documentId, bool isAckRequested = false)
            where TDocument : IDomainDocument
            where TMessage : IDomainMessage<TDocument>
        {
            return new NanoMessage
            {
                ClientId = clientId,
                OperationId = Guid.NewGuid(),
                IsAckRequested = isAckRequested,
                SubscribeMessage = new NanoSubscribeMessage
                {
                    DocumentTypeName = typeof(TDocument).FullName,
                    MessageTypeName = typeof(TMessage).FullName,
                    NanoDocumentId = documentId,
                }
            };
        }

        public NanoMessage CreateAckMessage(int errorCode = 0)
        {
            return new NanoMessage()
            {
                ClientId = ClientId,
                OperationId = OperationId,
                IsAckRequested = false,
                AcknowledgeMessage = new NanoAcknowledgeMessage
                {
                    ErrorCode = errorCode
                }
            };
        }

    }
}