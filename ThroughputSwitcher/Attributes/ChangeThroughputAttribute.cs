using System;

namespace ThroughputSwitcher.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ChangeThroughputAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public int Throughput { get; set; }
    }
}