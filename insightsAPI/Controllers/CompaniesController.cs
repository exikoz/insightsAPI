using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using insightsAPI.Models.DTOs;
using insightsAPI.Models.Entities;
using insightsAPI.Services;
using Asp.Versioning;
using insightsAPI.Data;

namespace insightsAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyInsightService _insightService;
        private readonly AppDbContext _context;
        private readonly HybridCache _cache;

        public CompaniesController(ICompanyInsightService insightService, AppDbContext context, HybridCache cache)
        {
            _insightService = insightService;
            _context = context;
            _cache = cache;
        }

        /// <summary>
        /// Retrieves a list of companies with support for filtering and pagination.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of companies per page (default: 20)</param>
        /// <param name="sniKod">Filter by SNI code</param>
        /// <param name="postort">Filter by city (postort)</param>
        /// <returns>A paginated list of companies.</returns>
        /// <response code="200">Returns the list of companies</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedListDto<CompanyResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCompanies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sniKod = null,
            [FromQuery] string? postort = null)
        {
            // Validating pagination
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var cacheKey = $"companies_{page}_{pageSize}_{sniKod ?? "all"}_{postort ?? "all"}";

            var result = await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                var query = _context.Companies.AsQueryable();

                if (!string.IsNullOrWhiteSpace(sniKod))
                {
                    query = query.Where(c => c.SniKod == sniKod);
                }

                if (!string.IsNullOrWhiteSpace(postort))
                {
                    query = query.Where(c => c.Postort == postort);
                }

                var totalCount = await query.CountAsync(token);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var items = await query
                    .OrderBy(c => c.Orgnr)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(company => new CompanyResponseDto
                    {
                        Orgnr = company.Orgnr,
                        Namn = company.Namn,
                        SniKod = company.SniKod,
                        Organisationsform = company.Organisationsform,
                        JurformKod = company.JurformKod,
                        Postort = company.Postort,
                        Postnr = company.Postnr,
                        Registreringsdatum = company.Registreringsdatum,
                        Avregistreringsdatum = company.Avregistreringsdatum,
                        Konkurs = company.Konkurs,
                        Aktiv = company.Aktiv
                    })
                    .ToListAsync(token);

                return new PaginatedListDto<CompanyResponseDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) }, tags: new[] { "companies_list" });

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific company based on its organization number.
        /// </summary>
        /// <param name="orgNr">The company's organization number</param>
        /// <returns>Company information</returns>
        /// <response code="200">Returns the company data</response>
        /// <response code="404">If the company is not found</response>
        [HttpGet("{orgNr}")]
        [ProducesResponseType(typeof(CompanyResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCompany(string orgNr)
        {
            var cleanOrgnr = orgNr.Replace("-", "").Replace(" ", "").Trim();
            var company = await _context.Companies.FindAsync(cleanOrgnr);
            
            if (company == null)
            {
                return NotFound(new { error = $"Bolag med orgnr {orgNr} hittades inte." });
            }

            var responseDto = new CompanyResponseDto
            {
                Orgnr = company.Orgnr,
                Namn = company.Namn,
                SniKod = company.SniKod,
                Organisationsform = company.Organisationsform,
                JurformKod = company.JurformKod,
                Postort = company.Postort,
                Postnr = company.Postnr,
                Registreringsdatum = company.Registreringsdatum,
                Avregistreringsdatum = company.Avregistreringsdatum,
                Konkurs = company.Konkurs,
                Aktiv = company.Aktiv
            };

            return Ok(responseDto);
        }

        /// <summary>
        /// Updates company information and invalidates the cache for the company list.
        /// </summary>
        /// <param name="orgNr">The company's organization number</param>
        /// <param name="dto">Updated company data</param>
        /// <response code="204">Company updated successfully</response>
        /// <response code="404">If the company is not found</response>
        [HttpPut("{orgNr}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCompany(string orgNr, [FromBody] UpdateCompanyRequestDto dto)
        {
            var cleanOrgnr = orgNr.Replace("-", "").Replace(" ", "").Trim();
            var company = await _context.Companies.FindAsync(cleanOrgnr);
            
            if (company == null) return NotFound(new { error = $"Bolag med orgnr {orgNr} hittades inte." });

            company.Namn = dto.Namn;
            company.Postort = dto.Postort;
            company.Aktiv = dto.Aktiv;
            
            await _context.SaveChangesAsync();

            // VG: Invalidate Cache by Tag ("Eviction by tag")
            await _cache.RemoveByTagAsync("companies_list");

            return NoContent();
        }

        /// <summary>
        /// Deletes a company and all related data. Invalidates the company list cache.
        /// </summary>
        /// <param name="orgNr">The company's organization number</param>
        /// <response code="204">Company deleted successfully</response>
        /// <response code="404">If the company is not found</response>
        [HttpDelete("{orgNr}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCompany(string orgNr)
        {
            var cleanOrgnr = orgNr.Replace("-", "").Replace(" ", "").Trim();
            var company = await _context.Companies.FindAsync(cleanOrgnr);

            if (company == null) return NotFound(new { error = $"Bolag med orgnr {orgNr} hittades inte." });

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            // VG: Invalidate Cache by Tag ("Eviction by tag")
            await _cache.RemoveByTagAsync("companies_list");

            return NoContent();
        }

        /// <summary>
        /// Performs a financial analysis for a given company.
        /// </summary>
        /// <param name="orgNr">The company's organization number</param>
        /// <param name="req">Analysis parameters</param>
        /// <response code="200">Returns the analysis result</response>
        /// <response code="404">If the company is not found</response>
        /// <response code="422">If the data is invalid for analysis</response>
        [HttpPost("{orgNr}/analyze")]
        [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

        /// <summary>
        /// Generates an AI-driven business insight for a specific company using Google Gemini.
        /// </summary>
        /// <param name="orgNr">The company's organization number</param>
        /// <response code="200">Returns the AI insight</response>
        /// <response code="404">If the company is not found</response>
        /// <response code="422">If the data is invalid for AI generation</response>
        /// <response code="503">If the AI service is unavailable</response>
        [HttpPost("{orgNr}/insight")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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
