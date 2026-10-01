using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

internal static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");
        string dataPath = Path.Combine("data", "orders.json");
        IOrderStore store = useFile ? new FileOrderStore(dataPath) : new InMemoryOrderStore(SampleData.Orders());
        
        return useCache ? new CachingOrderStore(store) : store;
    }
}