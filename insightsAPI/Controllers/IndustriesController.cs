using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using insightsAPI.Data;
using Asp.Versioning;

namespace insightsAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class IndustriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IndustriesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves industry benchmarks for a given SNI code and size class.
        /// </summary>
        /// <param name="sniCode">The industry's SNI code (e.g., 62010 or 62.01)</param>
        /// <param name="storleksklass">Size class (default: "TOT")</param>
        /// <returns>Industry benchmarks and KPIs.</returns>
        /// <response code="200">Returns the industry benchmark</response>
        /// <response code="404">If no benchmark was found</response>
        [HttpGet("{sniCode}/benchmark")]
        [ProducesResponseType(typeof(insightsAPI.Models.DTOs.BenchmarkResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var responseDto = new insightsAPI.Models.DTOs.BenchmarkResponseDto
            {
                SniKod = benchmark.SniKod,
                Storleksklass = benchmark.Storleksklass,
                RorelsemarginalPct = benchmark.RorelsemarginalPct,
                SoliditetPct = benchmark.SoliditetPct,
                Skuldssattningsgrad = benchmark.Skuldssattningsgrad,
                OmsattningstillvaxtPct = benchmark.OmsattningstillvaxtPct,
                OmsattningPerAnstalldTkr = benchmark.OmsattningPerAnstalldTkr
            };

            return Ok(responseDto);
        }
    }
}
