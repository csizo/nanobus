using ProtoBuf;

namespace NanoBus
{
    [ProtoContract]
    public class NanoAcknowledgeMessage
    {
        [ProtoMember(1, Name = "errcode")]
        public int ErrorCode { get; set; }

    }
}