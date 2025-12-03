using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActiveTypeController : ControllerBase
    {
        private readonly Service _service;
        public ActiveTypeController(Service service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetListActiveType()
        {
            try
            {
                var activetype = await _service.GetActiveTypesAsync();
                return Ok(activetype);
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
