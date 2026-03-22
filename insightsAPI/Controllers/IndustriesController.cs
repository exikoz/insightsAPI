using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using insightsAPI.Data;

namespace insightsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndustriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IndustriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{sniCode}/benchmark")]
        public async Task<IActionResult> GetBenchmark(string sniCode, [FromQuery] string storleksklass = "TOT")
        {
            var scbSniFormat = sniCode.Length >= 3 ? $"{sniCode.Substring(0, 2)}.{sniCode.Substring(2)}" : sniCode;
            var sni2 = sniCode.Length >= 2 ? sniCode.Substring(0, 2) : sniCode;

            var benchmark = await _context.Benchmarks
                .FirstOrDefaultAsync(b => b.SniKod == scbSniFormat && b.Storleksklass == storleksklass);

            if (benchmark == null)
            {
                benchmark = await _context.Benchmarks
                    .FirstOrDefaultAsync(b => b.SniKod == sni2 && b.Storleksklass == storleksklass);
            }

            // Fallback to TOT if specific class wasn't found
            if (benchmark == null && storleksklass != "TOT")
            {
                benchmark = await _context.Benchmarks
                    .FirstOrDefaultAsync(b => (b.SniKod == scbSniFormat || b.SniKod == sni2) && b.Storleksklass == "TOT");
            }

            if (benchmark == null)
            {
                return NotFound(new { error = $"Inget branschsnitt hittades för SNI-kod {sniCode}." });
            }

            return Ok(benchmark);
        }
    }
}
