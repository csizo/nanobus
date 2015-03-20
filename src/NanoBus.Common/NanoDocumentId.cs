using System;
using ProtoBuf;

namespace NanoBus
{
    [ProtoContract]
    public class NanoDocumentId : IEquatable<NanoDocumentId>
    {
        public bool Equals(NanoDocumentId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NanoDocumentId)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(NanoDocumentId left, NanoDocumentId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NanoDocumentId left, NanoDocumentId right)
        {
            return !Equals(left, right);
        }

        [ProtoMember(1)]
        public Guid? Id { get; private set; }

        public NanoDocumentId()
        {
            
        }
        public NanoDocumentId(Guid? documentId)
        {
            Id = documentId;
        }

        public override string ToString()
        {
            return string.Format("Id: {0}", Id);
        }

        public static readonly NanoDocumentId Empty = new NanoDocumentId(null);
    }
}