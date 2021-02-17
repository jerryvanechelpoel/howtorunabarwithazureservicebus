using System.Collections.Generic;

namespace Azug.ServiceBar.Waiters
{
    public sealed class WaiterFactory
    {
        public Dictionary<string, WaiterLogic> Waiters { get; }

        public WaiterFactory(ServiceBusProxy proxy)
        {
            Waiters = new Dictionary<string, WaiterLogic>
            {
                { "Moe", new WaiterLogic(proxy, "Moe") },
                { "Renee", new WaiterLogic(proxy, "Renee") }
            };
        }
    }
}
