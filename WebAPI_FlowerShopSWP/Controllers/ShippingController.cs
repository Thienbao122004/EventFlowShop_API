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

            if (request.from_district_id != 1442)
            {
                return BadRequest("Địa chỉ gửi hàng không hợp lệ.");
            }

            if (string.IsNullOrEmpty(request.to_ward_code))
            {
                return BadRequest("Mã phường/xã không được để trống.");
            }

            // Lấy danh sách dịch vụ có sẵn
            var availableServices = await GetAvailableServices(request.from_district_id, request.to_district_id);
            if (availableServices == null || !availableServices.Any())
            {
                return BadRequest("Không có dịch vụ vận chuyển phù hợp.");
            }

            // Chọn dịch vụ phù hợp (ví dụ: dịch vụ đầu tiên trong danh sách)
            var selectedService = availableServices.First();

            string cacheKey = $"shipping_fee_{request.to_district_id}_{request.to_ward_code}_{request.weight}_{selectedService.service_id}";

            if (_cache.TryGetValue(cacheKey, out string cachedResponse))
            {
                return Ok(cachedResponse);
            }

            // Sử dụng service_id từ dịch vụ đã chọn
            request.service_id = selectedService.service_id;
            request.service_type_id = selectedService.service_type_id;

            var client = _httpClientFactory.CreateClient("GHNClient");

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            _logger.LogInformation($"Sending request to GHN API: {JsonConvert.SerializeObject(request)}");

            var response = await client.PostAsync("v2/shipping-order/fee", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Successful response from GHN API: {data}");

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(cacheKey, data, cacheEntryOptions);

                return Ok(data);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Error response from GHN API: {errorContent}");
            return BadRequest($"Unable to calculate shipping fee. Error: {errorContent}");
        }

        private async Task<List<ServiceInfo>> GetAvailableServices(int fromDistrict, int toDistrict)
        {
            var client = _httpClientFactory.CreateClient("GHNClient");

            var requestBody = new
            {
                shop_id = 194721,
                from_district = fromDistrict,
                to_district = toDistrict
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v2/shipping-order/available-services", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ServiceResponse>(data);
                return result.Data;
            }

            _logger.LogError($"Error fetching available services: {await response.Content.ReadAsStringAsync()}");
            return null;
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

            var availableServices = await GetAvailableServices(1442, request.to_district_id);
            if (availableServices == null || !availableServices.Any())
            {
                return BadRequest("Không có dịch vụ vận chuyển phù hợp.");
            }

            var selectedService = availableServices.First();

            var createOrderDto = new CreateOrderDto
            {
                payment_type_id = 1,
                note = request.note,
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
                client_order_code = request.client_order_code,
                content = "Hoa",
                weight = request.weight,
                length = request.length,
                width = request.width,
                height = request.height,
                service_id = selectedService.service_id,
                service_type_id = selectedService.service_type_id,
                items = request.items.Select(item => new CreateOrderItemDto
                {
                    name = item.name,
                    code = item.code,
                    quantity = item.quantity,
                    price = item.price,
                    weight = 5000,
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

        [HttpGet("services")]
        public async Task<IActionResult> GetServices([FromQuery] ServiceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient("GHNClient");

            var requestBody = new
            {
                shop_id = 194721,
                from_district = request.FromDistrict,
                to_district = request.ToDistrict
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v2/shipping-order/available-services", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Successful response from GHN API for services: {data}");
                return Ok(data);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Error response from GHN API for services: {errorContent}");
            return BadRequest($"Unable to fetch available services. Error: {errorContent}");
        }
    }
}