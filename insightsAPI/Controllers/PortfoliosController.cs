using Microsoft.AspNetCore.Mvc;
using insightsAPI.Models.DTOs;
using insightsAPI.Services;

namespace insightsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfoliosController : ControllerBase
    {
        private readonly ICompanyInsightService _insightService;
        private readonly ILogger<PortfoliosController> _logger;

        public PortfoliosController(ICompanyInsightService insightService, ILogger<PortfoliosController> logger)
        {
            _insightService = insightService;
            _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzePortfolio([FromBody] PortfolioRequestDto req)
        {
            if (req.OrgnrLista == null || !req.OrgnrLista.Any() || req.OrgnrLista.Count > 200)
            {
                return UnprocessableEntity(new { error = "Lista med organisationsnummer måste innehålla mellan 1 och 200 st." });
            }

            var results = new List<Dictionary<string, object>>();
            var fel = new List<Dictionary<string, object>>();

            foreach (var orgnr in req.OrgnrLista)
            {
                try
                {
                    var analysis = await _insightService.BuildAnalysisAsync(orgnr);
                    var kpis = (List<CompanyAnalysisResultDto>)analysis["nyckeltal_historik"];
                    var senaste = kpis.LastOrDefault();
                    var scores = (ScoreResultDto)analysis["scores"];

                    dynamic bolag = analysis["bolag"];
                    var bolagObj = bolag.GetType();
                    string bNamn = bolagObj.GetProperty("namn").GetValue(bolag, null) as string ?? "";
                    string bSni = bolagObj.GetProperty("sni_kod").GetValue(bolag, null) as string ?? "";
                    string bBransch = bolagObj.GetProperty("bransch").GetValue(bolag, null) as string ?? "";
                    string bPostort = bolagObj.GetProperty("postort").GetValue(bolag, null) as string ?? "";

                    decimal? trend = null;
                    if (kpis.Count >= 2)
                    {
                        var curr = kpis.Last().RorelsemarginalPct;
                        var prev = kpis[kpis.Count - 2].RorelsemarginalPct;
                        if (curr.HasValue && prev.HasValue) trend = Math.Round(curr.Value - prev.Value, 2);
                    }

                    results.Add(new Dictionary<string, object>
                    {
                        ["orgnr"] = orgnr,
                        ["namn"] = bNamn,
                        ["sni_kod"] = bSni,
                        ["bransch"] = bBransch,
                        ["postort"] = bPostort,
                        ["senaste_ar"] = senaste?.Ar,
                        ["omsattning"] = senaste?.Omsattning,
                        ["anstallda"] = senaste?.Anstallda,
                        ["rorelsemarginal_pct"] = senaste?.RorelsemarginalPct,
                        ["soliditet_pct"] = senaste?.SoliditetPct,
                        ["skuldsattning"] = senaste?.Skuldsattning,
                        ["omsattningstillvaxt_pct"] = senaste?.OmsattningstillvaxtPct,
                        ["omsattning_per_anstalld"] = senaste?.OmsattningPerAnstalld,
                        ["rorelsemarginal_trend"] = trend,
                        ["trend_signal"] = trend > 0 ? "positiv" : (trend < 0 ? "negativ" : "stabil"),
                        ["risk_score"] = scores.RiskScore,
                        ["opportunity_score"] = scores.OpportunityScore,
                        ["risk_niva"] = scores.RiskNiva,
                        ["opportunity_niva"] = scores.OpportunityNiva,
                        ["signals"] = scores.Signals,
                        ["datakvalitet"] = scores.Datakvalitet,
                        ["full_analys_url"] = $"/api/companies/{orgnr}/analyze"
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    fel.Add(new Dictionary<string, object> { ["orgnr"] = orgnr, ["status"] = 404, ["fel"] = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    fel.Add(new Dictionary<string, object> { ["orgnr"] = orgnr, ["status"] = 422, ["fel"] = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing portfolio analysis for orgnr {Orgnr}", orgnr);
                    fel.Add(new Dictionary<string, object> { ["orgnr"] = orgnr, ["status"] = 500, ["fel"] = ex.Message });
                }
            }

            var validSortKeys = new[] { "risk_score", "opportunity_score", "omsattning", "rorelsemarginal_pct" };
            var sortKey = validSortKeys.Contains(req.SorteraEfter) ? req.SorteraEfter : "risk_score";

            var sortedResults = req.SorteraFallande
                ? results.OrderByDescending(x => x.ContainsKey(sortKey) ? x[sortKey] : null).ToList()
                : results.OrderBy(x => x.ContainsKey(sortKey) ? x[sortKey] : null).ToList();

            return Ok(new
            {
                antal_analyserade = sortedResults.Count,
                antal_fel = fel.Count,
                sorterat_efter = sortKey,
                bolag = sortedResults,
                fel = fel.Any() ? fel : null,
                meta = new
                {
                    genererad = DateTime.UtcNow.ToString("O"),
                    datakalla = "Bolagsverket iXBRL + SCB " + DateTime.Now.Year
                }
            });
        }
    }
}
