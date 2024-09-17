using OrdersService.Data.Abstractions;

namespace OrdersService.Models
{
    [BsonCollection("completed_orders")]
    public class CompletedOrder : Document
    {
        public string OrderId { get; set; }
        public string OrderIdSecond { get; set; }
        public string UserIdSecond { get; set; }
        public decimal Price { get; set; }
    }
}
