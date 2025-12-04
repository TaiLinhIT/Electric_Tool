using Electric_Meter_WebAPI.Dto.ControlcodeDto;
using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlcodeController : ControllerBase
    {
        private readonly Service _service;
        public ControlcodeController(Service service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetListControlcode()
        {
            try
            {
                var result = await _service.GetListControlcode();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateControlCodeAsync([FromBody] CreateControlcodeDto dto)
        {
            try
            {
                var result = await _service.CreateControlcodeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpPut]
        public async Task<IActionResult> UpdateControlCodeAsync([FromBody] EditControlcodeDto dto)
        {
            try
            {
                var result = await _service.EditControlcodeAsync(dto);
                return Ok(result);  
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpDelete("{codeid}")]
        public async Task<IActionResult> DeleteControlCodeAsync(int codeid)
        {
            try
            {
                var result = await _service.DeleteControlcodeAsync(codeid);
                return Ok(result);  
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetControlcodeByDevidAsync(int id)
        {
            try
            {
                var result = await _service.GetControlcodeByDevidAsync(id);
                return Ok(result;
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
    }
}
