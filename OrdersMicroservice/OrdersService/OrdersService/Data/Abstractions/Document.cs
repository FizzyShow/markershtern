using MongoDB.Bson;

namespace OrdersService.Data.Abstractions
{
    public class Document : IDocument
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedAt => Id.CreationTime;
    }
}
