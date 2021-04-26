using Castle.DynamicProxy;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ThroughputSwitcher.Attributes;

namespace ThroughputSwitcher.Interceptors
{
    public class ThroughputChangerInterceptor : AsyncInterceptorBase
    {
        private const int FallbackThroughput = 400;

        private readonly CosmosClient _cosmosDbClient;
        private readonly CosmosDbOptions _cosmosDbOptions;

        public ThroughputChangerInterceptor(IOptions<CosmosDbOptions> cosmosDbOptions)
        {
            _cosmosDbOptions = cosmosDbOptions.Value;
            _cosmosDbClient = new CosmosClient(_cosmosDbOptions.Uri, _cosmosDbOptions.PrimaryKey, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
            });
        }

        protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        {
            var attr = invocation.MethodInvocationTarget.GetCustomAttribute<ChangeThroughputAttribute>();

            if (attr == null)
            {
                await proceed(invocation, proceedInfo).ConfigureAwait(false);
                return;
            }

            var container = _cosmosDbClient.GetContainer(_cosmosDbOptions.DatabaseName, attr.CollectionName);
            var originalThroughput = await container.ReadThroughputAsync();

            /*
             * Check whether throughput is provisioned at the database level (instead of the container level).
             * If so, skip interceptor execution, since throughput cannot be set.
             */
            if (!originalThroughput.HasValue)
            {
                await proceed(invocation, proceedInfo).ConfigureAwait(false);
                return;
            }

            var defaultThroughput = _cosmosDbOptions.Throughput?.Default ?? FallbackThroughput;

            var throughput = attr.Throughput == default
                ? _cosmosDbOptions.Throughput?.Max ?? defaultThroughput
                : attr.Throughput;

            try
            {
                if (throughput != originalThroughput)
                {
                    await ChangeThroughput(container, throughput);
                }

                await proceed(invocation, proceedInfo).ConfigureAwait(false);
            }
            finally
            {
                if (throughput != defaultThroughput)
                {
                    await ChangeThroughput(container, defaultThroughput);
                }
            }
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            var attr = invocation.MethodInvocationTarget.GetCustomAttribute<ChangeThroughputAttribute>();

            if (attr == null)
            {
                return await proceed(invocation, proceedInfo).ConfigureAwait(false);
            }

            var container = _cosmosDbClient.GetContainer(_cosmosDbOptions.DatabaseName, attr.CollectionName);
            var originalThroughput = await container.ReadThroughputAsync();

            /*
             * Check whether throughput is provisioned at the database level (instead of the container level).
             * If so, skip interceptor execution, since throughput cannot be set.
             */
            if (!originalThroughput.HasValue)
            {
                return await proceed(invocation, proceedInfo).ConfigureAwait(false);
            }

            var defaultThroughput = _cosmosDbOptions.Throughput?.Default ?? FallbackThroughput;

            var throughput = attr.Throughput == default
                ? _cosmosDbOptions.Throughput?.Max ?? defaultThroughput
                : attr.Throughput;

            try
            {
                if (throughput != originalThroughput)
                {
                    await ChangeThroughput(container, throughput);
                }

                return await proceed(invocation, proceedInfo).ConfigureAwait(false);
            }
            finally
            {
                if (throughput != defaultThroughput)
                {
                    await ChangeThroughput(container, defaultThroughput);
                }
            }
        }

        private async Task ChangeThroughput(Container container, int newThroughput)
        {
            var changeThroughputRetryPolicy = Policy<ThroughputResponse>
                .Handle<Exception>()
                .OrResult(r => r.StatusCode != HttpStatusCode.OK)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var throughputProps = ThroughputProperties.CreateManualThroughput(newThroughput);
            await changeThroughputRetryPolicy.ExecuteAsync(() => container.ReplaceThroughputAsync(throughputProps));

            var getThroughputRetryPolicy = Policy<int?>
                .Handle<Exception>()
                .OrResult(r => r != newThroughput)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await getThroughputRetryPolicy.ExecuteAsync(() => container.ReadThroughputAsync());
        }
    }
}