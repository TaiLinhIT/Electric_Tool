using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorTypeController : ControllerBase
    {
        private readonly Service _service;
        public SensorTypeController(Service service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetListSensorType()
        {
            try
            {
                var sensortype = await _service.GetSensorTypesAsync();
                return Ok(sensortype);
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
