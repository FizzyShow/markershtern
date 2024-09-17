using OrdersService.Models.Enums;
using OrdersService.Protos;

namespace OrdersService.Models.Messages
{
    public class DataCreateOrder
    {
        public string UserId { get; set; }
        public Models.Enums.OrderType OrderType { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public Protos.CustomTypes.DecimalValue Price { get; set; }
    }
}
