using AutoMapper;
using OrdersService.Models.Enums;
using OrdersService.Models.Messages;
using OrdersService.Protos.CustomTypes;

namespace OrdersService.Models.Map
{
    public class OrdersProfile : Profile
    {
        public OrdersProfile()
        {
            CreateMap<DataCreateOrder, Protos.DataCreateOrder>();
            CreateMap<Protos.DataCreateOrder, DataCreateOrder>();

            CreateMap<ProductPriceChanged, Protos.ProductPriceChanged>();
            CreateMap<Protos.ProductPriceChanged, ProductPriceChanged>();

            CreateMap<decimal, DecimalValue>().ConvertUsing(val => val);
            CreateMap<DecimalValue, decimal>().ConvertUsing(val => val);

            CreateMap<DecimalValue, Protos.DecimalValue>();
            CreateMap<Protos.DecimalValue, DecimalValue>();

            CreateMap<OrderType, Protos.OrderType>();
            CreateMap<Protos.OrderType, OrderType>();

            CreateMap<OrderCandidateOccuredProcessSuccess, Protos.OrderCandidateOccuredProcessSuccess>();
            CreateMap<Protos.OrderCandidateOccuredProcessSuccess, OrderCandidateOccuredProcessSuccess>();
        }
    }
}
