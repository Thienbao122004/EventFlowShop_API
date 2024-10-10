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

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ShippingController> _logger;
        private readonly GhnApiSettings _ghnSettings;

        public ShippingController(IHttpClientFactory httpClientFactory, ILogger<ShippingController> logger, IOptions<GhnApiSettings> ghnSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _ghnSettings = ghnSettings.Value;
        }

        [HttpPost("calculate-shipping-fee")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] ShippingFeeRequest request)
        {
            var hcmDistrictIds = new List<int> { 1442, 1443, 1444, 1446, 1447, 1448, 1449, 1450, 1451, 1452, 1453, 1454, 1455, 1456, 1457, 1458, 1461, 1462, 1463 };

            if (!hcmDistrictIds.Contains(request.to_district_id))
            {
                return BadRequest("Shop chỉ hỗ trợ giao hàng trong các quận thuộc Hồ Chí Minh.");
            }

            request.service_id = 53320;
            request.service_type_id = 2;

            var client = _httpClientFactory.CreateClient("GHNClient");

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("v2/shipping-order/fee", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
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
                payment_type_id = request.payment_type_id,
                note = "Đơn hàng hoa",
                from_name = request.from_name,
                from_phone = request.from_phone,
                from_address = request.from_address,
                from_ward_name = request.from_ward_name,
                from_district_name = request.from_district_name,
                from_province_name = request.from_province_name,
                required_note = request.required_note,
                to_name = request.to_name,
                to_phone = request.to_phone,
                to_address = request.to_address,
                to_ward_code = request.to_ward_code,
                to_ward_name = request.to_ward_name,
                to_district_id = request.to_district_id,
                cod_amount = request.items.Sum(item => item.price * item.quantity),
                content = "Hoa",
                weight = request.weight,
                length = request.length,
                width = request.width,
                height = request.height,
                insurance_value = request.items.Sum(item => item.price * item.quantity),
                service_type_id = request.service_type_id,
                items = request.items.Select(item => new CreateOrderItemDto
                {
                    name = item.name,
                    code = item.code,
                    quantity = item.quantity,
                    price = item.price,
                    length = item.length,
                    width = item.width,
                    height = item.height,
                    category = new GhnCategory { level1 = item.category.level1 }
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
            if (district_id <= 0)
            {
                return BadRequest("Invalid district_id. Please provide a valid district ID.");
            }

            var client = _httpClientFactory.CreateClient("GHNClient");

            try
            {
                var response = await client.GetAsync($"master-data/ward?district_id={district_id}");

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw response for district {district_id}: {content}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<dynamic>(content);
                        return Ok(data);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError($"Error parsing JSON for district {district_id}: {jsonEx.Message}");
                        return StatusCode(500, "Error parsing response from GHN API");
                    }
                }
                else
                {
                    _logger.LogError($"Error fetching wards for district {district_id}. Status code: {response.StatusCode}. Content: {content}");
                    return StatusCode((int)response.StatusCode, $"Unable to fetch wards. Error: {content}");
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"HTTP Request Error for district {district_id}: {e.Message}");
                return StatusCode(500, $"An error occurred while fetching wards: {e.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error for district {district_id}: {e.Message}");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}