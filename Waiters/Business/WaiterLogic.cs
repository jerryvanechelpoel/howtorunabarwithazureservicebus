using Azug.ServiceBar.Models;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Azug.ServiceBar.Waiters
{
    public class WaiterLogic
    {
        public event EventHandler<WaiteringActivityEventArgs> OnWaiteringActivity;

        private readonly string _name;
        private Random Randomizer { get; } = new Random();
        private ServiceBusProxy Proxy { get; }
        private Timer ActivityTimer { get; set; }
        public bool IsActive { get; private set; }

        public WaiterLogic(ServiceBusProxy proxy, string name)
        {
            Proxy = proxy;
            _name = name;
        }

        public void Start()
        {
            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} starts working at Moe's Tavern!"));
            ActivityTimer = new Timer { Interval = 5000, Enabled = true, AutoReset = false };
            ActivityTimer.Elapsed += DoWaitering;
            IsActive = true;
        }

        public void Stop()
        {
            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} is finishing up."));
            ActivityTimer.Enabled = false;
            ActivityTimer.Elapsed -= DoWaitering;
            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} stops working at Moe's Tavern!"));
            IsActive = false;
        }

        private async void DoWaitering(object sender, ElapsedEventArgs e)
        {
            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} is getting the next order."));
            Order nextOrder = await Proxy.GetNextOrderAsync();

            if (nextOrder != null)
            {
                await ProcessOrdersAsync(nextOrder);
            }
            else
            {
                DoSomethingElse();
            }

            ActivityTimer.Enabled = true;
        }

        private void DoSomethingElse()
        {
            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} didn't find new orders."));

            int waiterAction = Randomizer.Next(0, 5);

            string activity = waiterAction switch
            {
                1 => $"{_name} is doing some dishes.",
                2 => $"{_name} runs for the bathroom.",
                3 => $"{_name} goes outside to have a smoke.",
                4 => $"{_name} nips of their drink.",
                _ => $"{_name} is talking to the customers."
            };

            OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs(activity));
        }

        private async Task ProcessOrdersAsync(Order nextOrder)
        {
            foreach (OrderedDrink drink in nextOrder.Drinks)
            {
                if (drink == null)
                {
                    OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name}: Well I can't do much with this."));
                }
                else
                {
                    var servedDrink = new ServedDrink { PaidBy = nextOrder.OrderedBy, DrinkName = drink.DrinkName };

                    OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name} Preparing {drink.DrinkName} for {drink.OrderedFor}."));
                    await Task.Delay(1000);

                    int poisonedBeerRatio = Randomizer.Next(0, 100);

                    if (poisonedBeerRatio > 80)
                    {
                        servedDrink.DrinkName = $"Spoiled {drink.DrinkName}";
                        OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name}: Created a SPOILED {drink.DrinkName} without knowing it!"));
                    }

                    await Proxy.ServeDrinkAsync(drink.OrderedFor, servedDrink);
                    OnWaiteringActivity?.Invoke(this, new WaiteringActivityEventArgs($"{_name}: Alright another soul served!"));
                }
            }
        }
    }
}
