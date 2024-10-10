﻿using System.Collections.Generic;
using WebAPI_FlowerShopSWP.Dto;

namespace WebAPI_FlowerShopSWP.DTO
{
    public class CreateOrderDto
    {
        public int payment_type_id { get; set; }
        public string note { get; set; }
        public string from_name { get; set; }
        public string from_phone { get; set; }
        public string from_address { get; set; }
        public string from_ward_name { get; set; }
        public string from_district_name { get; set; }
        public string from_province_name { get; set; }
        public string required_note { get; set; }
        public string return_phone { get; set; }
        public string return_address { get; set; }
        public int? return_district_id { get; set; }
        public string return_ward_code { get; set; }
        public string client_order_code { get; set; }
        public string to_name { get; set; }
        public string to_phone { get; set; }
        public string to_address { get; set; }
        public string to_ward_code { get; set; }
        public string to_ward_name { get; set; }
        public int to_district_id { get; set; }
        public int cod_amount { get; set; }
        public string content { get; set; }
        public int weight { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int? pick_station_id { get; set; }
        public int? deliver_station_id { get; set; }
        public int insurance_value { get; set; }
        public int service_id { get; set; }
        public int service_type_id { get; set; }
        public string coupon { get; set; }
        public int? pick_shift { get; set; }
        public string pickup_time { get; set; }
        public List<CreateOrderItemDto> items { get; set; }
    }

    public class CreateOrderItemDto
    {
        public string name { get; set; }
        public string code { get; set; }
        public int quantity { get; set; }
        public int price { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public GhnCategory category { get; set; }
    }

    public class Category
    {
        public string level1 { get; set; }
    }
}