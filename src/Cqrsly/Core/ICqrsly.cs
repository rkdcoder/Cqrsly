using Cqrsly;

namespace Cqrsly
{
    public interface ICqrsly
    {
        // Request sem retorno
        Task Send(IRequest request, CancellationToken cancellationToken = default);

        // Request com retorno (genérico)
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        // Publish de notifications (domain events)
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}