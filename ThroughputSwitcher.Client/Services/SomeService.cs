using System.Threading.Tasks;
using ThroughputSwitcher.Attributes;

namespace ThroughputSwitcher.Client.Services
{
    public class SomeService : ISomeService
    {
        private const string SomeCollectionName = "SomeCollection";
        
        [ChangeThroughput(CollectionName = SomeCollectionName)]
        public async Task WriteSomethingToCosmosDb()
        {
            // Cosmos DB access logic
        }
    }
}