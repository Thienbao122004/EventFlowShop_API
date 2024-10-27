using AutoMapper;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI_FlowerShopSWP.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateNotificationDTO, Notification>();
            CreateMap<Notification, NotificationDTO>();
        }
    }
}