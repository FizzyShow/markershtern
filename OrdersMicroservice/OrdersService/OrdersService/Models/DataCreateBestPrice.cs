using System.Security.Cryptography.X509Certificates;
using OrdersService.Models.Enums;

namespace OrdersService.Models
{
    public class DataCreateBestPrice
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string OrderId { get; set; }
        public decimal Price { get; set; }
        public PriceType PriceType { get; set; }

        public DataCreateBestPrice(string productId, 
                                   string productName, 
                                   string orderId, 
                                   decimal price, 
                                   PriceType priceType)
        {
            ProductId = productId;
            ProductName = productName;
            OrderId = orderId;
            Price = price;
            PriceType = priceType;
        }
    }
}
