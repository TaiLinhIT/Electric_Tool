using Electric_Meter_WebAPI.Dto.SensorTypeDto;
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSensorTypeById(int id)
        {
            try
            {
                var data = await _service.GetSensorTypeByIdAsync(id);
                return Ok(data);    
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> PostCodeType(SensorTypeDto dto)
        {
            try
            {
                var result = await _service.AddSensorTypeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut]
        public async Task<IActionResult> PutSensorType(SensorTypeDto dto)
        {
            try
            {
                var result = await _service.UpdateSensorTypeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSensorType(int id)
        {
            try
            {
                var result = await _service.DeleteSensorTypeAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
    }
}
