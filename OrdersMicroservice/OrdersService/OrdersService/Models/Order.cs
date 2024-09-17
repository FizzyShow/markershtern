using OrdersService.Data.Abstractions;
using OrdersService.Models.Enums;

namespace OrdersService.Models
{
    [BsonCollection("orders")]
    public class Order : Document
    { 
        public string UserId { get; set; }
        public OrderType OrderType { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
