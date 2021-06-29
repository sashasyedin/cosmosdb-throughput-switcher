using System.Threading.Tasks;

namespace ThroughputSwitcher.Client.Services
{
    public interface ISomeService
    {
        Task WriteSomethingToCosmosDb();
    }
}