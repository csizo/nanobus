using System;
using System.Threading.Tasks;

namespace NanoBus.Client
{
    public interface INanoEventBus<TDocument> : IDisposable
        where TDocument : IDomainDocument
    {
        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        Task PublishAsync<TMessage>(TMessage message) where TMessage : IDomainMessage<TDocument>;

        /// <summary>
        /// Subscribes to the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="callback">The on message.</param>
        /// <returns>Subscription on the <typeparamref name="TDocument"/> <typeparamref name="TMessage"/></returns>
        IDisposable Subscribe<TMessage>(Func<TMessage, Task> callback) where TMessage : IDomainMessage<TDocument>;


        /// <summary>
        /// Subscribes to the specified message type for the given <paramref name="documentId"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the event.</typeparam>
        /// <param name="onMessage">The on message.</param>
        /// <param name="documentId">The identifier.</param>
        /// <returns>Subscription on the <typeparamref name="TDocument"/> <typeparamref name="TMessage"/></returns>
        IDisposable Subscribe<TMessage>(Func<TMessage, Task> onMessage, Guid documentId) where TMessage : IDomainMessage<TDocument>;


    }
}