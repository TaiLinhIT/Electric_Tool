using Electric_Meter_WebAPI.Dto.DeviceDto;
using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly Service _service;
        public DeviceController(Service service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            try
            {
                await _service.InsertToDevice(dto);
                return Ok(new { message = "Data received successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                // Rất quan trọng: Server cần trả về lỗi 500 nếu catch được lỗi.
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut]
        public async Task<IActionResult> UpdateDevice([FromBody] EditDeviceDto dto)
        {
            try
            {
                await _service.EditDevice(dto);
                return Ok(new { message = "Data received successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                // Rất quan trọng: Server cần trả về lỗi 500 nếu catch được lỗi.
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetListDevice()
        {
            try
            {
                var devices = await _service.GetListDevice();
                return Ok(devices);
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
