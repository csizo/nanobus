using System;

namespace NanoBus
{
    /// <summary>
    /// Defines a message of a <see cref="IDomainDocument"/>
    /// </summary>
    public interface IDomainMessage<TDocument>
    {
        /// <summary>
        /// Gets the document identifier.
        /// </summary>
        /// <value>
        /// The document identifier.
        /// </value>
        Guid DocumentId { get; }
    }
}