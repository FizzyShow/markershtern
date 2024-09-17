using OrdersService.Models.Enums;
using OrdersService.Protos;

namespace OrdersService.Models.Messages
{
    public class ProductPriceChanged
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public Models.Enums.PriceType PriceType { get; set; }
        public DecimalValue Price { get; set; }
    }
}
