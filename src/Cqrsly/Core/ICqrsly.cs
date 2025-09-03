namespace Cqrsly
{
    public interface ICqrsly
    {
        Task Send<TRequest>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest;

        Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest<TResponse>;

        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

        Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification;
    }
}