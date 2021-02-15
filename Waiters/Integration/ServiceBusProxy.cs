using Azug.ServiceBar.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Azug.ServiceBar.Waiters
{
    public sealed class ServiceBusProxy
    {
        private ServiceBusConnectionStringBuilder OrderQueueCnBuilder { get; }
        private ServiceBusConnectionStringBuilder BarTopicCnBuilder { get; }

        public ServiceBusProxy(IConfiguration configuration)
        {
            OrderQueueCnBuilder = new ServiceBusConnectionStringBuilder(configuration.GetValue<string>("Azure:ServiceBus:OrderQueue:Waiters"));
            BarTopicCnBuilder = new ServiceBusConnectionStringBuilder(configuration.GetValue<string>("Azure:ServiceBus:BarTopic:Waiters"));
        }

        public async Task<Order> GetNextOrderAsync()
        {
            Order order = null;
            var client = new QueueClient(OrderQueueCnBuilder);

            client.RegisterMessageHandler(async (msg, token) =>
            {
                Message message = msg;

                if (message != null)
                {
                    string body = Encoding.UTF8.GetString(message.Body);

                    if (body != null)
                    {
                        order = JsonSerializer.Deserialize<Order>(body);
                    }

                    if (order == null)
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

            return order;
        }

        public async Task ServeDrinkAsync(string orderedFor, ServedDrink drink)
        {
            var client = new TopicClient(BarTopicCnBuilder);

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(drink);
            var message = new Message(body);

            message.UserProperties.Add("OrderedFor", orderedFor);

            await client.SendAsync(message);
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            // logging goes here
            return Task.CompletedTask;
        }
    }
}
