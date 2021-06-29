using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ThroughputSwitcher.Client.Services;

namespace ThroughputSwitcher.Client.Controllers
{
    [Route("api")]
    [ApiController]
    public class SomeController : ControllerBase
    {
        private readonly ISomeService _someService;

        public SomeController(ISomeService someService)
        {
            _someService = someService;
        }

        [HttpGet("run")]
        public async Task<IActionResult> Index()
        {
            await _someService.WriteSomethingToCosmosDb();
            return Ok();
        }
    }
}