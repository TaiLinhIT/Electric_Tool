using Electric_Meter_WebAPI.Dto.CodeTypeDto;
using Electric_Meter_WebAPI.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Electric_Meter_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeTypeController : ControllerBase
    {
        private readonly Service _service;
        public CodeTypeController(Service service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetListCodeType()
        {
            try
            {
                var results = await _service.GetCodeTypeAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> PostCodeType(CodeTypeDto dto) {
            try
            {
                var result = await _service.AddCodeTypeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpPut]
        public async Task<IActionResult> PutCodeType(CodeTypeDto dto) {
            try
            {
                var result = await _service.UpdateCodeTypeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCodeType(int id) {
            try
            {
                var result = await _service.DeleteCodeTypeAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }
    }
}
