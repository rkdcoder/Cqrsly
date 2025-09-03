using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cqrsly
{
    internal sealed class CqrslyDispatcher : ICqrsly
    {
        private readonly IServiceProvider _provider;

        public CqrslyDispatcher(IServiceProvider provider)
            => _provider = provider;

        // =========================
        // Send - sem retorno
        // =========================
        public async Task Send<TRequest>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var handler = _provider.GetRequiredService<IRequestHandler<TRequest>>();
            await handler.Handle(request, ct).ConfigureAwait(false);
        }

        // =========================
        // Send - com retorno (explícito TRequest,TResponse)
        // =========================
        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest<TResponse>
            => SendInternal<TRequest, TResponse>(request, ct);

        // =========================
        // Send - com retorno (MediatR-like, infere TResponse a partir de IRequest<TResponse>)
        // =========================
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Fecha generic SendInternal<TRequest,TResponse> com o tipo concreto do request e TResponse
            var requestType = request.GetType();

            var mi = _sendInternalMi ??= typeof(CqrslyDispatcher)
                .GetMethod(nameof(SendInternal), BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(nameof(CqrslyDispatcher), nameof(SendInternal));

            var closed = mi.MakeGenericMethod(requestType, typeof(TResponse));
            return (Task<TResponse>)closed.Invoke(this, new object[] { request, ct })!;
        }

        private static MethodInfo? _sendInternalMi;

        // =========================
        // Publish - notifications (sequencial)
        // =========================
        public async Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var handlers = _provider.GetServices<INotificationHandler<TNotification>>().ToList();
            if (handlers.Count == 0) return;

            foreach (var handler in handlers)
            {
                ct.ThrowIfCancellationRequested();
                await handler.Handle(notification, ct).ConfigureAwait(false);
            }
        }

        // =========================
        // Internals - compõe pipeline e executa handler final
        // =========================
        private async Task<TResponse> SendInternal<TRequest, TResponse>(TRequest request, CancellationToken ct)
            where TRequest : IRequest<TResponse>
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

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
    }
}