using System;

namespace NanoBus.Client.NanoServiceBus
{
    internal class NanoSubsciptionKey : IEquatable<NanoSubsciptionKey>
    {
        public NanoSubsciptionKey(Type messageType, NanoDocumentId nanoDocumentId)
        {
            MessageType = messageType;
            NanoDocumentId = nanoDocumentId;
        }

        public Type MessageType { get; protected set; }
        public NanoDocumentId NanoDocumentId { get; protected set; }

        public bool Equals(NanoSubsciptionKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(NanoDocumentId, other.NanoDocumentId) && Equals(MessageType, other.MessageType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((NanoSubsciptionKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((NanoDocumentId != null ? NanoDocumentId.GetHashCode() : 0) * 397) ^
                       (MessageType != null ? MessageType.GetHashCode() : 0);
            }
        }

        public static bool operator ==(NanoSubsciptionKey left, NanoSubsciptionKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NanoSubsciptionKey left, NanoSubsciptionKey right)
        {
            return !Equals(left, right);
        }
    }
}