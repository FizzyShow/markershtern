using OrdersService.Data.Abstractions;

namespace OrdersService.Models
{
    [BsonCollection("active_buy_orders")]
    public class ActiveBuyOrder : ActiveOrder
    {
    }
}
