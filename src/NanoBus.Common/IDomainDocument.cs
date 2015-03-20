using System;

namespace NanoBus
{
    /// <summary>
    /// Defines a document
    /// </summary>
    public interface IDomainDocument
    {
        /// <summary>
        /// Gets the document identifier.
        /// </summary>
        /// <returns></returns>
        Guid GetDocumentId();
    }
}
