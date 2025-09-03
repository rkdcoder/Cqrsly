using Microsoft.Extensions.DependencyInjection;

namespace Cqrsly
{
    internal sealed class CqrslyDispatcher : ICqrsly
    {
        private readonly IServiceProvider _provider;

        public CqrslyDispatcher(IServiceProvider provider) => _provider = provider;

        public async Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest
        {
            var handler = _provider.GetRequiredService<IRequestHandler<TRequest>>();
            await handler.Handle(request, ct).ConfigureAwait(false);
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest<TResponse>
            => SendInternal<TRequest, TResponse>(request, ct);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            => SendInternal((dynamic)request, ct);

        private async Task<TResponse> SendInternal<TRequest, TResponse>(TRequest request, CancellationToken ct)
            where TRequest : IRequest<TResponse>
        {
            var handler = _provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            var behaviors = _provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToList();

            RequestHandlerDelegate<TResponse> next = () => handler.Handle(request, ct);

            for (var i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var current = next;
                next = () => behavior.Handle(request, ct, current);
            }

            return await next().ConfigureAwait(false);
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
        {
            var handlers = _provider.GetServices<INotificationHandler<TNotification>>().ToList();
            foreach (var h in handlers)
                await h.Handle(notification, ct).ConfigureAwait(false);
        }
    }
}