using OrdersService.Data.Abstractions;

namespace OrdersService.Models
{
    [BsonCollection("product_best_price")]
    public class BestProductPrice : Document
    {
        public string OrderId { get; set; }
        public decimal BestBuyPrice { get; set; }
        public decimal BestSellPrice { get; set; }
    }
}
