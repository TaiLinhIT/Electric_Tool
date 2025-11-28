using Electric_Meter_WebAPI.Models;
using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Service _service;

        public SensorController(IServiceScopeFactory scopeFactory, Service service)
        {
            _scopeFactory = scopeFactory;
            _service = service;
        }

        // DTO trùng với object gửi từ client
        public class DataDto
        {
            public string Devid { get; set; }
            public double Value { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        [HttpPost("sensor")]
        public async Task<IActionResult> Send([FromBody] DataDto dto)
        {
            try
            {
                // Dừng tại đây để kiểm tra giá trị của 'dto'
                Console.WriteLine($"Received: {dto.Devid}, {dto.Value}, {dto.CreatedAt}");
                // ... Thêm logic xử lý/lưu database nếu có ...
                return Ok(new { message = "Data received successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                // Rất quan trọng: Server cần trả về lỗi 500 nếu catch được lỗi.
                return StatusCode(500, ex.Message);
            }
        }

    }


}
