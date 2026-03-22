using insightsAPI.ApiClients;
using insightsAPI.Data;
using insightsAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace insightsAPI.Services
{
    public interface ICompanyInsightService
    {
        Task<Dictionary<string, object>> BuildAnalysisAsync(string orgnr);
        Task<Dictionary<string, object>> GenerateInsightAsync(string orgnr);
    }

    public class CompanyInsightService : ICompanyInsightService
    {
        private readonly AppDbContext _context;
        private readonly IFinancialAnalyzerService _analyzer;
        private readonly IScoringEngine _scoring;
        private readonly IGeminiClient _gemini;
        private readonly ILogger<CompanyInsightService> _logger;

        public CompanyInsightService(
            AppDbContext context,
            IFinancialAnalyzerService analyzer,
            IScoringEngine scoring,
            IGeminiClient gemini,
            ILogger<CompanyInsightService> logger)
        {
            _context = context;
            _analyzer = analyzer;
            _scoring = scoring;
            _gemini = gemini;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildAnalysisAsync(string orgnr)
        {
            var cleanOrgnr = orgnr.Replace("-", "").Replace(" ", "").Trim();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Orgnr == cleanOrgnr);
            if (company == null)
            {
                throw new KeyNotFoundException($"Bolag med orgnr {orgnr} hittades inte.");
            }

            if (string.IsNullOrEmpty(company.SniKod) || company.SniKod == "00000")
            {
                throw new ArgumentException("Bolaget saknar SNI-kod och kan inte analyseras.");
            }

            var latestFactBatch = await _context.Facts
                .Where(f => f.Orgnr == cleanOrgnr)
                .OrderByDescending(f => f.ArsBatch)
                .Select(f => f.ArsBatch)
                .FirstOrDefaultAsync();

            if (latestFactBatch == 0)
            {
                throw new KeyNotFoundException("Inga finansiella data hittades för detta bolag. Bolaget lämnar troligen inte digital årsredovisning (iXBRL).");
            }

            var facts = await _context.Facts
                .Where(f => f.Orgnr == cleanOrgnr && f.ArsBatch >= latestFactBatch - 4)
                .ToListAsync();

            var kpis = _analyzer.ComputeKpis(facts, company);
            if (!kpis.Any())
            {
                throw new ArgumentException("Kunde inte beräkna nyckeltal för detta bolag.");
            }

            var scbSniFormat = company.SniKod.Length >= 3 ? $"{company.SniKod.Substring(0, 2)}.{company.SniKod.Substring(2)}" : company.SniKod;
            var sni2 = company.SniKod.Substring(0, 2);

            var benchmarksDb = await _context.Benchmarks
                .Where(b => b.SniKod == scbSniFormat || b.SniKod == sni2)
                .ToListAsync();

            if (!benchmarksDb.Any())
            {
                throw new KeyNotFoundException($"Inget branschsnitt hittades för SNI-kod {company.SniKod}.");
            }

            var benchmarks = new Dictionary<string, Models.Entities.Benchmark>();
            foreach (var b in benchmarksDb)
            {
                // Prefer exact SNI over 2-digit fallback if both exist
                if (!benchmarks.ContainsKey(b.Storleksklass) || b.SniKod == scbSniFormat) 
                {
                    benchmarks[b.Storleksklass] = b;
                }
            }

            var jamforelser = _analyzer.CompareToBenchmark(kpis, benchmarks);
            var scores = _scoring.ComputeScores(kpis, benchmarks);

            var targetBenchmark = benchmarks.GetValueOrDefault(kpis.Last().AnvandStorleksklass ?? "TOT") ?? benchmarks.Values.FirstOrDefault();

            return new Dictionary<string, object>
            {
                ["bolag"] = new
                {
                    orgnr = company.Orgnr,
                    namn = company.Namn,
                    sni_kod = company.SniKod,
                    bransch = targetBenchmark?.SniKod ?? "",
                    postort = company.Postort,
                    organisationsform = company.Organisationsform,
                    aktiv = company.Aktiv
                },
                ["bransch"] = new
                {
                    sni_kod = targetBenchmark?.SniKod,
                    storleksklass = targetBenchmark?.Storleksklass
                },
                ["nyckeltal_historik"] = kpis, // This includes jamforelser now actually, which represents Python's "jamforelser_per_ar"
                ["jamforelser_per_ar"] = jamforelser.Select(k => new { ar = k.Ar, storleksklass = k.Storleksklass, storleksklass_kalla = k.StorleksklassKalla, klass_approximerad = k.KlassApproximerad, jamforelser = k.Jamforelser}),
                ["scores"] = scores,
                ["meta"] = new
                {
                    genererad = DateTime.UtcNow.ToString("O"),
                    datakalla = "Bolagsverket iXBRL + SCB Branschnyckeltal 2024",
                    senaste_bokslut_ar = kpis.LastOrDefault()?.Ar
                }
            };
        }

        public async Task<Dictionary<string, object>> GenerateInsightAsync(string orgnr)
        {
            var analysis = await BuildAnalysisAsync(orgnr);
            var bolag = analysis["bolag"] as dynamic;
            var historik = (List<CompanyAnalysisResultDto>)analysis["nyckeltal_historik"];
            var scores = (ScoreResultDto)analysis["scores"];

            var last3Years = historik.TakeLast(3).ToList();

            var nyckeltalRader = new List<string>();
            foreach (var r in last3Years)
            {
                nyckeltalRader.Add($"  {r.Ar}: omsättning {FmtKr(r.Omsattning)} | rörelsemarginal {r.RorelsemarginalPct}% | soliditet {r.SoliditetPct}% | tillväxt {r.OmsattningstillvaxtPct}% | anställda {r.Anstallda}");
            }

            var senasteJamfArr = new List<string>();
            var lastYearJamf = last3Years.LastOrDefault()?.Jamforelser;
            if (lastYearJamf != null)
            {
                foreach (var j in lastYearJamf)
                {
                    if (j.Bolag != null && j.BranschMedian != null)
                    {
                        senasteJamfArr.Add($"  {j.Nyckeltal}: {j.Bolag} {j.Enhet} (bransch: {j.BranschMedian} {j.Enhet}, {j.Signal})");
                    }
                }
            }

            var signalsText = string.Join("\n", scores.Signals.Select(s => $"  [{s.Typ.ToUpper()}] {s.Label}: {s.Meddelande}"));

            string nyckeltalStr = string.Join("\n", nyckeltalRader);
            string senasteJamfStr = senasteJamfArr.Any() ? string.Join("\n", senasteJamfArr) : "  Saknas";

            int senasteAnstallda = last3Years.LastOrDefault()?.Anstallda ?? 0;
            string enmansbolagInstruktion = senasteAnstallda <= 1
                ? "\n6. OBS! Bolaget är ett enmansbolag (0-1 anställda). Ett tapp i omsättning kan bero på personliga skäl (sjukdom, föräldraledighet, deltid) snarare än att affärsmodellen kraschar. Ge råd anpassade för soloföretagare och var försiktig med att dra för stora växlar på minskad omsättning."
                : "";

            int riskScore = scores.RiskScore ?? 50;
            string tonInstruktion;
            if (riskScore < 30)
                tonInstruktion = "\n7. TONLÄGE: Kalibrera tonläget till att vara rådgivande och stöttande, inte larmande, eftersom risk-scoren är låg.";
            else if (riskScore > 70)
                tonInstruktion = "\n7. TONLÄGE: Använd ett allvarligt och uppmanande tonläge, eftersom risk-scoren är hög och bolaget har utmaningar.";
            else
                tonInstruktion = "\n7. TONLÄGE: Håll en neutral, objektiv och analytisk ton.";

            // Dynamic objects don't resolve extension properties nicely in C#, reflection fallback or explicit casting needed.
            // But we know it's an anonymous object from BuildAnalysisAsync.
            var bolagObj = bolag.GetType();
            string bNamn = bolagObj.GetProperty("namn").GetValue(bolag, null) as string ?? "";
            string bBransch = bolagObj.GetProperty("bransch").GetValue(bolag, null) as string ?? "";
            string bSni = bolagObj.GetProperty("sni_kod").GetValue(bolag, null) as string ?? "";

            string prompt = 
$@"Du är en strikt och analytisk svensk affärsrådgivare. Analysera nedanstående data för ett bolag och ge konkreta, datadrivna strategiska råd.

BOLAGSDATA:
Namn: {bNamn}
Bransch: {bBransch} (SNI: {bSni})
Storlek: {last3Years.LastOrDefault()?.Storleksklass ?? "okänd"}

NYCKELTAL (senaste 3 åren):
{nyckeltalStr}

JÄMFÖRELSE MOT BRANSCHMEDIAN (senaste år):
{senasteJamfStr}

SIGNALER:
{(string.IsNullOrEmpty(signalsText) ? "  Inga signaler" : signalsText)}

Risk Score: {scores.RiskScore}/100
Opportunity Score: {scores.OpportunityScore}/100

STRIKTA INSTRUKTIONER FÖR DITT SVAR:
1. Inled INTE med artighetsfraser eller introduktioner (t.ex. ""Här är en analys..."", ""Baserat på datan...""). Börja svara på punkt 2 direkt.
2. Formatera ditt svar exakt med tre tydliga rubriker: ""### Största risken"", ""### Främsta möjligheten"" och ""### Rekommenderad åtgärd"".
3. Var extremt koncis och affärsmässig. Håll dig till max 250 ord totalt.
4. Avsluta med en fullständig mening, lämna inget halvfärdigt.
5. Håll dig strikt till den bransch bolaget verkar i. Gör inga antaganden om att de försöker vara en annan typ av bolag (t.ex. mjukvarubolag) om datan inte stöder det.{enmansbolagInstruktion}{tonInstruktion}";

            string insight = await _gemini.GenerateInsightAsync(prompt);

            var skickadData = new
            {
                nyckeltal = last3Years,
                jamforelser = lastYearJamf ?? new List<ComparisonDto>(),
                scores = new
                {
                    risk_score = scores.RiskScore,
                    opportunity_score = scores.OpportunityScore,
                    signals = scores.Signals
                }
            };

            return new Dictionary<string, object>
            {
                ["orgnr"] = bolagObj.GetProperty("orgnr").GetValue(bolag, null) as string ?? "",
                ["namn"] = bNamn,
                ["insight"] = insight,
                ["skickad_data_till_ai"] = skickadData,
                ["meta"] = new
                {
                    genererad = DateTime.UtcNow.ToString("O"),
                    ai_modell = "gemini-3.1-flash-lite",
                    tokens_in_approx = prompt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length,
                    datakalla = "Bolagsverket iXBRL + SCB " + DateTime.Now.Year
                }
            };
        }

        private string FmtKr(decimal? val)
        {
            if (val == null) return "–";
            if (val >= 1_000_000) return $"{(val.Value / 1_000_000):F1}Mkr";
            if (val >= 1_000) return $"{(val.Value / 1_000):F0}tkr";
            return $"{val.Value:F0}kr";
        }
    }
}
