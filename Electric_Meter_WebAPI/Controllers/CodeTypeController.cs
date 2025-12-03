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
    }
}
