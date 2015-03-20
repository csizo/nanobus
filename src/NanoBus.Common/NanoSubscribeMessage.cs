using ProtoBuf;

namespace NanoBus
{
    [ProtoContract]
    public class NanoSubscribeMessage
    {
        [ProtoMember(1, Name = "doctype")]
        public string DocumentTypeName { get; set; }

        [ProtoMember(2, Name = "msgtype")]
        public string MessageTypeName { get; set; }

        [ProtoMember(3, Name = "docid")]
        public NanoDocumentId NanoDocumentId { get; set; }
    }
}