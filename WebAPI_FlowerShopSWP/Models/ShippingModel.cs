using System.Collections.Generic;
using WebAPI_FlowerShopSWP.Dto;

namespace WebAPI_FlowerShopSWP.Models
{
    public class ShippingFeeRequest
    {
        public int to_district_id { get; set; }
        public string to_ward_code { get; set; }
        public int from_district_id { get; set; }
        public int weight { get; set; }
        public int service_id { get; set; }
        public int service_type_id { get; set; }
    }

    public class CreateOrderRequest
    {

        public string to_name { get; set; }
        public string from_name { get; set; }
        public string from_phone { get; set; }
        public string from_address { get; set; }
        public string from_ward_name { get; set; }
        public string from_district_name { get; set; }
        public string from_province_name { get; set; }
        public string to_ward_name { get; set; }
        public string to_phone { get; set; }
        public string to_address { get; set; }
        public string to_ward_code { get; set; }
        public int to_district_id { get; set; }
        public int weight { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int service_type_id { get; set; }
        public int payment_type_id { get; set; }
        public string required_note { get; set; }
        public List<CreateOrderItemRequest> items { get; set; }
    }

    public class CreateOrderItemRequest
    {
        public string name { get; set; }
        public string code { get; set; }
        public int quantity { get; set; }
        public int price { get; set; }
    }

    public class WardResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<Ward> Data { get; set; }
    }

    public class Ward
    {
        public string WardCode { get; set; }
        public string WardName { get; set; }
        public int DistrictId { get; set; }
    }

    public class DistrictResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<District> Data { get; set; }
    }

    public class District
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public int ProvinceId { get; set; }
    }

    public class ErrorResponse
    {
        public int code { get; set; }
        public string message { get; set; }
        public string code_message_value { get; set; }
    }
}