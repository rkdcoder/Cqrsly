using Microsoft.Extensions.DependencyInjection;

namespace Cqrsly
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCqrsly(this IServiceCollection services, Action<CqrslyOptionsBuilder>? configure = null)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            var builder = new CqrslyOptionsBuilder(services);
            configure?.Invoke(builder);
            builder.Build();

            services.AddScoped<ICqrsly, CqrslyDispatcher>();
            return services;
        }
    }
}