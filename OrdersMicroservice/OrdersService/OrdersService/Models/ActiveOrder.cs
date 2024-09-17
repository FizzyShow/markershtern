using MongoDB.Bson;
using OrdersService.Data.Abstractions;

namespace OrdersService.Models
{
    public abstract class ActiveOrder : Document
    {
        public bool InTransaction { get; set; }
        public string UserId { get; set; }
    }
}
