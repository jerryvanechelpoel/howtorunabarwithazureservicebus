namespace Azug.ServiceBar.Models
{
    public sealed class Order
    {
        public string OrderedBy { get; set; }
        public OrderedDrink[] Drinks { get; set; }
    }
}