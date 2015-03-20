using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace NanoBus
{
    [ProtoContract]
    public class NanoMessageBatch
    {

        public NanoMessageBatch()
        {
            NanoMessages = new List<NanoMessage>();
        }
        [ProtoMember(1, Name = "messages")]
        public List<NanoMessage> NanoMessages { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Fixed32);
                return ms.ToArray();
            }
        }

        public static NanoMessageBatch Deserialize(Stream stream)
        {
            return Serializer.DeserializeWithLengthPrefix<NanoMessageBatch>(stream, PrefixStyle.Fixed32);
        }

    }
}