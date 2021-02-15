using Azug.ServiceBar.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Azug.ServiceBar.Customers
{
    public sealed class ServiceBusProxy
    {
        private const string TopicPath = "barTopic";
        private ServiceBusConnectionStringBuilder NamespaceCnBuilder { get; }
        private ServiceBusConnectionStringBuilder OrderQueueCnBuilder { get; }
        private ServiceBusConnectionStringBuilder BarTopicCnBuilder { get; }

        public ServiceBusProxy(IConfiguration configuration)
        {
            NamespaceCnBuilder = new ServiceBusConnectionStringBuilder(configuration.GetValue<string>("Azure:ServiceBus:Namespace:Customers"));
            OrderQueueCnBuilder = new ServiceBusConnectionStringBuilder(configuration.GetValue<string>("Azure:ServiceBus:OrderQueue:Customers"));
            BarTopicCnBuilder = new ServiceBusConnectionStringBuilder(configuration.GetValue<string>("Azure:ServiceBus:BarTopic:Customers"));
        }

        public async Task<bool> IsCustomerPresentAsync(string customerName)
        {
            var client = new ManagementClient(NamespaceCnBuilder);

            return await client.SubscriptionExistsAsync(TopicPath, customerName);
        }

        public async Task TakeASeatAsync(string customerName)
        {
            var client = new ManagementClient(NamespaceCnBuilder);

            if (!await client.SubscriptionExistsAsync(TopicPath, customerName))
            {
                var description = new SubscriptionDescription(TopicPath, customerName);
                var rule = new RuleDescription("default", new SqlFilter($"OrderedFor = '{customerName}' OR OrderedFor = 'Everyone'"));

                await client.CreateSubscriptionAsync(description, rule);
            }
        }

        public async Task LeaveBarAsync(string customerName)
        {
            var client = new ManagementClient(NamespaceCnBuilder);

            if (await client.SubscriptionExistsAsync(TopicPath, customerName))
            {
                await client.DeleteSubscriptionAsync(TopicPath, customerName);
            }
        }

        public async Task MakeOrderAsync(Order order)
        {
            var client = new QueueClient(OrderQueueCnBuilder);

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(order);
            var message = new Message(body);

            await client.SendAsync(message);
        }

        public async Task<ServedDrink> GetNextDrinkAsync(string customerName)
        {
            var client = new SubscriptionClient(BarTopicCnBuilder, customerName);

            string body = null;

            client.RegisterMessageHandler(async (msg, token) =>
            {
                Message message = msg;

                if (message != null)
                {
                    body = Encoding.UTF8.GetString(message.Body);

                    if (body.Contains("spoiled", StringComparison.OrdinalIgnoreCase))
                    {
                        await client.DeadLetterAsync(msg.SystemProperties.LockToken);
                    }
                    else
                    {
                        await client.CompleteAsync(msg.SystemProperties.LockToken);
                    }
                }
            }, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            });

            await client.UnregisterMessageHandlerAsync(TimeSpan.FromMilliseconds(2000));
            await client.CloseAsync();

            if (body != null)
            {
                return JsonSerializer.Deserialize<ServedDrink>(body);
            }

            return default;
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            // logging goes here
            return Task.CompletedTask;
        }
    }
}