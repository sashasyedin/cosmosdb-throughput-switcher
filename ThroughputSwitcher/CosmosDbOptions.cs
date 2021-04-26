using System;

namespace ThroughputSwitcher
{
    public class CosmosDbOptions
    {
        public string Uri { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseName { get; set; }
        public CosmosDbThroughput Throughput { get; set; }
        public CosmosDbRetryPolicy RetryPolicy { get; set; }
    }

    public class CosmosDbRetryPolicy
    {
        public int MaxRetryAttempts { get; set; }
        public TimeSpan BulkMaxRetryWaitTime { get; set; }
        public TimeSpan SearchMaxRetryWaitTime { get; set; }
    }

    public class CosmosDbThroughput
    {
        public int? Default { get; set; }
        public int? Max { get; set; }
    }
}