using OrdersService.Data.Abstractions;

namespace OrdersService.Models
{
    [BsonCollection("active_sell_orders")]
    public class ActiveSellOrder : ActiveOrder
    {
    }
}
