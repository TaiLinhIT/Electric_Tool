using Electric_Meter_WebAPI.Dto.SensorDataDto;
using Electric_Meter_WebAPI.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorDataController : ControllerBase
    {
        private readonly IService _service;
        public SensorDataController(IService service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> InsertToSensorData(SensorDataDto dto)
        {
            try
            {
                var result = await _service.InsertToSensorDataAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
    }
}
