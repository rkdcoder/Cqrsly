using Cqrsly;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cqrsly
{
    public sealed class CqrslyOptionsBuilder
    {
        internal IServiceCollection Services { get; }
        internal readonly List<Assembly> Assemblies = new();
        internal ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Transient;
        internal NotificationPublishStrategy PublishStrategy { get; private set; } = NotificationPublishStrategy.Sequential;
        internal readonly List<Type> OpenBehaviors = new();

        public CqrslyOptionsBuilder(IServiceCollection services) => Services = services;

        // ----- Fluent API -----

        public CqrslyOptionsBuilder AddHandlersFromAssembly(Assembly assembly)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            Assemblies.Add(assembly);
            return this;
        }

        public CqrslyOptionsBuilder AddHandlersFromAssemblyContaining<T>()
            => AddHandlersFromAssembly(typeof(T).Assembly);

        public CqrslyOptionsBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            foreach (var asm in assemblies) AddHandlersFromAssembly(asm);
            return this;
        }

        public CqrslyOptionsBuilder WithHandlerLifetime(ServiceLifetime lifetime)
        {
            HandlerLifetime = lifetime;
            return this;
        }

        public CqrslyOptionsBuilder WithNotifications(NotificationPublishStrategy strategy)
        {
            PublishStrategy = strategy;
            return this;
        }

        public CqrslyOptionsBuilder AddOpenBehavior(Type openGenericBehavior)
        {
            if (openGenericBehavior is null) throw new ArgumentNullException(nameof(openGenericBehavior));
            if (!openGenericBehavior.IsGenericTypeDefinition) throw new ArgumentException("Behavior precisa ser open generic, ex.: typeof(LoggingBehavior<,>)");
            OpenBehaviors.Add(openGenericBehavior);
            return this;
        }

        public CqrslyOptionsBuilder AddOpenBehavior<TBehavior>() where TBehavior : class
            => AddOpenBehavior(typeof(TBehavior));

        // ----- build -----

        internal void Build()
        {
            foreach (var b in OpenBehaviors.Distinct())
                Services.AddTransient(typeof(IPipelineBehavior<,>), b);

            if (Assemblies.Count > 0)
            {
                foreach (var asm in Assemblies.Distinct())
                    RegisterHandlersFrom(asm, HandlerLifetime);
            }
        }

        private void RegisterHandlersFrom(Assembly assembly, ServiceLifetime lifetime)
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .ToArray();

            foreach (var type in types)
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (!@interface.IsGenericType) continue;

                    var genDef = @interface.GetGenericTypeDefinition();

                    // IRequestHandler<T>
                    if (genDef == typeof(IRequestHandler<>)
                        || genDef == typeof(IRequestHandler<,>)
                        || genDef == typeof(INotificationHandler<>))
                    {
                        Services.Add(new ServiceDescriptor(@interface, type, lifetime));
                    }
                }
            }
        }
    }
}