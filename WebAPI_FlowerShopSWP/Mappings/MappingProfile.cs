using AutoMapper;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static WebAPI_FlowerShopSWP.Controllers.OrdersController;

namespace WebAPI_FlowerShopSWP.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateNotificationDTO, Notification>();
            CreateMap<Notification, NotificationDTO>();

            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.FlowerName, opt => opt.MapFrom(src => src.Flower.FlowerName))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Flower.ImageUrl));

            CreateMap<Cart, CartDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.CartItems))
                .ForMember(dest => dest.TotalAmount,
                    opt => opt.MapFrom(src => src.CartItems.Sum(item => item.Price * item.Quantity)));
        }
    }
}