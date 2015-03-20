using System;
using System.Runtime.Serialization;

namespace NanoBus
{
    [DataContract]
    public abstract class DomainMessage<T> : EventArgs, IDomainMessage<T> where T : IDomainDocument
    {
        protected DomainMessage()
        {

        }

        protected DomainMessage(T document)
        {
            DocumentId = document.GetDocumentId();
        }


        [DataMember]
        public Guid DocumentId { get; set; }
    }
}