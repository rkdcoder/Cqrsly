using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrsly
{
    internal sealed class CqrslyDispatcher : ICqrsly
    {
        private readonly IServiceProvider _provider;
        private readonly NotificationPublishStrategy _publishStrategy;

        public CqrslyDispatcher(IServiceProvider provider, NotificationPublishStrategy publishStrategy)
        {
            _provider = provider;
            _publishStrategy = publishStrategy;
        }

        // -------- Send (sem retorno)
        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var requestType = request.GetType();

            // Fecha generic para <TRequest>
            var method = _sendVoidGeneric ??= typeof(CqrslyDispatcher).GetMethod(nameof(SendInternalVoid), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var closed = method.MakeGenericMethod(requestType);
            return (Task)closed.Invoke(this, new object[] { request, cancellationToken })!;
        }

        // -------- Send (com retorno)
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var requestType = request.GetType();

            var method = _sendGeneric ??= typeof(CqrslyDispatcher).GetMethod(nameof(SendInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var closed = method.MakeGenericMethod(requestType, typeof(TResponse));
            return (Task<TResponse>)closed.Invoke(this, new object[] { request, cancellationToken })!;
        }

        // -------- Publish (notifications)
        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var handlers = _provider.GetServices<INotificationHandler<TNotification>>().ToArray();
            if (handlers.Length == 0) return;

            if (_publishStrategy == NotificationPublishStrategy.Sequential)
            {
                foreach (var h in handlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await h.Handle(notification, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var tasks = new List<Task>(handlers.Length);
                foreach (var h in handlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    tasks.Add(h.Handle(notification, cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        // ===== Internals =====

        private static MethodInfo? _sendGeneric;
        private static MethodInfo? _sendVoidGeneric;

        // TRequest sem retorno
        private Task SendInternalVoid<TRequest>(TRequest request, CancellationToken ct) where TRequest : IRequest
        {
            var handler = _provider.GetService<IRequestHandler<TRequest>>();
            if (handler is null)
                ThrowNoHandler(typeof(TRequest));

            return handler!.Handle(request, ct);
        }

        // TRequest com TResponse
        private Task<TResponse> SendInternal<TRequest, TResponse>(TRequest request, CancellationToken ct) where TRequest : IRequest<TResponse>
        {
            // handler
            var handler = _provider.GetService<IRequestHandler<TRequest, TResponse>>();
            if (handler is null)
                ThrowNoHandler(typeof(TRequest), typeof(TResponse));

            // behaviors (ordem de registro = ordem de execução)
            var behaviors = _provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            RequestHandlerDelegate<TResponse> next = () => handler!.Handle(request, ct);

            // aplica em ordem reversa para compor corretamente
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var current = next;
                next = () => behavior.Handle(request, ct, current);
            }

            return next();
        }

        private static void ThrowNoHandler(Type requestType, Type? responseType = null)
        {
            if (responseType == null)
                throw new InvalidOperationException(
                    $"Nenhum IRequestHandler<{requestType.Name}> foi registrado para {requestType.FullName}.");

            throw new InvalidOperationException(
                $"Nenhum IRequestHandler<{requestType.Name}, {responseType.Name}> foi registrado para {requestType.FullName}.");
        }
    }
}