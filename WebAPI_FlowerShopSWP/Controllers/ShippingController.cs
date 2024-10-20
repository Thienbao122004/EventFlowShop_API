using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Dto;
using Microsoft.Extensions.Caching.Memory;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ShippingController> _logger;
        private readonly GhnApiSettings _ghnSettings;

        private readonly IMemoryCache _cache;
        public ShippingController(IHttpClientFactory httpClientFactory, ILogger<ShippingController> logger, IOptions<GhnApiSettings> ghnSettings, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _ghnSettings = ghnSettings.Value;
            _cache = cache;
        }

        [HttpPost("calculate-shipping-fee")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] ShippingFeeRequest request)
        {
            var hcmDistrictIds = new List<int> { 1442, 1443, 1444, 1446, 1447, 1448, 1449, 1450, 1451, 1452, 1453, 1454, 1455, 1456, 1457, 1458, 1461, 1462, 1463 };

            if (!hcmDistrictIds.Contains(request.to_district_id))
            {
                return BadRequest("Shop chỉ hỗ trợ giao hàng trong các quận thuộc Hồ Chí Minh.");
            }
            string cacheKey = $"shipping_fee_{request.to_district_id}_{request.to_ward_code}_{request.weight}";
            if (_cache.TryGetValue(cacheKey, out string cachedResponse))
            {
                return Ok(cachedResponse);
            }

            request.service_type_id = 2;

            var client = _httpClientFactory.CreateClient("GHNClient");

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("v2/shipping-order/fee", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                _cache.Set(cacheKey, data, cacheEntryOptions);
                return Ok(data);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return BadRequest($"Unable to calculate shipping fee. Error: {errorContent}");
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (string.IsNullOrEmpty(request.to_name) ||
                string.IsNullOrEmpty(request.to_phone) ||
                string.IsNullOrEmpty(request.to_address) ||
                string.IsNullOrEmpty(request.required_note))
            {
                return BadRequest("Missing required fields.");
            }

            var ward = await GetValidWard(request.to_district_id, request.to_ward_code, request.to_ward_name);
            if (ward == null)
            {
                return BadRequest($"Invalid ward information. Please check ward code and name.");
            }

            var createOrderDto = new CreateOrderDto
            {
                payment_type_id = 1,
                note = "Đơn hàng hoa",
                from_name = request.from_name,
                from_phone = request.from_phone,
                from_address = request.from_address,
                from_ward_name = request.from_ward_name,
                from_district_name = request.from_district_name,
                from_province_name = request.from_province_name,
                required_note = "CHOXEMHANGKHONGTHU",
                to_name = request.to_name,
                to_phone = request.to_phone,
                to_address = request.to_address,
                to_ward_code = request.to_ward_code,
                to_ward_name = request.to_ward_name,
                to_district_id = request.to_district_id,
                content = "Hoa",
                weight = request.weight,
                length = request.length,
                width = request.width,
                height = request.height,
                //insurance_value = request.items.Sum(item => item.price * item.quantity),
                service_type_id = 2,
                items = request.items.Select(item => new CreateOrderItemDto
                {
                    name = item.name,
                    quantity = item.quantity,
                    code = item.code,
                    price = item.price,
                }).ToList()
            };

            var client = _httpClientFactory.CreateClient("GHNClient");
            var content = new StringContent(JsonConvert.SerializeObject(createOrderDto), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v2/shipping-order/create", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Ok(data);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return BadRequest($"Unable to create order. Error: {errorContent}");
        }

        private async Task<Ward> GetValidWard(int districtId, string wardCode, string wardName)
        {
            var client = _httpClientFactory.CreateClient("GHNClient");
            var response = await client.GetAsync($"master-data/ward?district_id={districtId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var wardResponse = JsonConvert.DeserializeObject<WardResponse>(content);

                var ward = wardResponse.Data.FirstOrDefault(w => w.WardCode == wardCode);

                if (ward == null)
                {
                    ward = wardResponse.Data.FirstOrDefault(w =>
                        string.Equals(w.WardName, wardName, StringComparison.OrdinalIgnoreCase));
                }

                return ward;
            }

            return null;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var client = _httpClientFactory.CreateClient("GHNClient");
            var response = await client.GetAsync("master-data/province");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Ok(data);
            }
            return BadRequest("Unable to fetch provinces");
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts()
        {
            var client = _httpClientFactory.CreateClient("GHNClient");

            var response = await client.GetAsync("master-data/district?province_id=202");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var districtResponse = JsonConvert.DeserializeObject<DistrictResponse>(data);
                return Ok(districtResponse.Data);
            }

            return BadRequest("Unable to fetch districts.");
        }

        [HttpGet("wards")]
        public async Task<IActionResult> GetWards([FromQuery] int district_id)
        {
            var client = _httpClientFactory.CreateClient("GHNClient");

            var response = await client.GetAsync($"master-data/ward?district_id={district_id}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var wardResponse = JsonConvert.DeserializeObject<WardResponse>(data);

                return Ok(wardResponse.Data);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return BadRequest($"Unable to fetch wards. Error: {errorContent}");
        }
    }
}