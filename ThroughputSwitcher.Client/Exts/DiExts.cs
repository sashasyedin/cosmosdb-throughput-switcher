using System.Linq;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace ThroughputSwitcher.Client.Exts
{
    public static class DiExts
    {
        public static void AddProxiedScoped<TInterface, TImplementation>(this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddScoped<TImplementation>();
            services.AddScoped(typeof(TInterface), serviceProvider =>
            {
                var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
                var actual = serviceProvider.GetRequiredService<TImplementation>();
                var interceptors = serviceProvider.GetServices<IAsyncInterceptor>().ToArray();
                return proxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof(TInterface), actual, interceptors);
            });
        }
    }
}