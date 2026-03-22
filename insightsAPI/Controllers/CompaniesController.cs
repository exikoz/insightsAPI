using Microsoft.AspNetCore.Mvc;
using insightsAPI.Models.DTOs;
using insightsAPI.Services;
using insightsAPI.Data;

namespace insightsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyInsightService _insightService;
        private readonly AppDbContext _context;

        public CompaniesController(ICompanyInsightService insightService, AppDbContext context)
        {
            _insightService = insightService;
            _context = context;
        }

        [HttpGet("{orgNr}")]
        public async Task<IActionResult> GetCompany(string orgNr)
        {
            var cleanOrgnr = orgNr.Replace("-", "").Replace(" ", "").Trim();
            var company = await _context.Companies.FindAsync(cleanOrgnr);
            
            if (company == null)
            {
                return NotFound(new { error = $"Bolag med orgnr {orgNr} hittades inte." });
            }

            return Ok(company);
        }

        [HttpPost("{orgNr}/analyze")]
        public async Task<IActionResult> AnalyzeCompany(string orgNr, [FromBody] AnalyzeRequestDto req)
        {
            try
            {
                var result = await _insightService.BuildAnalysisAsync(orgNr); // Handles math
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
        }

        [HttpPost("{orgNr}/insight")]
        public async Task<IActionResult> CompanyInsight(string orgNr)
        {
            try
            {
                var result = await _insightService.GenerateInsightAsync(orgNr);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
        }
    }
}
