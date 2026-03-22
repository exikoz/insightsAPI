using insightsAPI.Models.DTOs;
using insightsAPI.Models.Entities;

namespace insightsAPI.Services
{
    public interface IScoringEngine
    {
        ScoreResultDto ComputeScores(List<CompanyAnalysisResultDto> kpis, Dictionary<string, Benchmark> benchmarks);
    }

    public class ScoringEngine : IScoringEngine
    {
        public ScoreResultDto ComputeScores(List<CompanyAnalysisResultDto> kpis, Dictionary<string, Benchmark> benchmarks)
        {
            if (kpis == null || !kpis.Any()) return new ScoreResultDto { Datakvalitet = "ingen_data", Signals = new List<SignalDto>() };

            var latest = kpis.Last();
            var klass = latest.AnvandStorleksklass ?? "TOT";
            if (!benchmarks.TryGetValue(klass, out var benchmark)) 
                return new ScoreResultDto { Datakvalitet = "inget_branschsnitt", Signals = new List<SignalDto>() };

            var signals = new List<SignalDto>();
            decimal riskP = 0, oppP = 0, riskMax = 0, oppMax = 0;
            int anvandaMetrics = 0;

            var metrics = new List<(decimal? Bv, decimal? Bm, string Label, int Weight, bool Hib)>
            {
                (latest.RorelsemarginalPct, benchmark.RorelsemarginalPct, "Rörelsemarginal", 30, true),
                (latest.SoliditetPct, benchmark.SoliditetPct, "Soliditet", 25, true),
                (latest.Skuldsattning, benchmark.Skuldssattningsgrad, "Skuldsättning", 25, false),
                (latest.OmsattningstillvaxtPct, benchmark.OmsattningstillvaxtPct, "Omsättningstillväxt", 20, true)
            };

            foreach (var m in metrics)
            {
                if (m.Bv == null || m.Bm == null) continue;

                anvandaMetrics++;
                var diff = m.Bv.Value - m.Bm.Value;
                var severity = Math.Min(Math.Abs(diff) / (Math.Abs(m.Bm.Value) + 0.001m), 1.0m);
                
                riskMax += m.Weight;
                oppMax += m.Weight;

                if (m.Hib ? diff < 0 : diff > 0)
                {
                    riskP += (int)(m.Weight * severity);
                    signals.Add(new SignalDto { Typ = "risk", Label = m.Label, Meddelande = $"{m.Label} är {(m.Hib ? "lägre" : "högre")} än branschsnittet" });
                }
                else if (m.Hib ? diff > 0 : diff < 0)
                {
                    oppP += (int)(m.Weight * severity);
                    signals.Add(new SignalDto { Typ = "opportunity", Label = m.Label, Meddelande = $"{m.Label} är {(m.Hib ? "högre" : "lägre")} än branschsnittet" });
                }
            }

            string datakvalitet = anvandaMetrics == 0 ? "otillracklig_data" : (anvandaMetrics < metrics.Count ? "partiell_data" : "komplett");

            return new ScoreResultDto
            {
                RiskScore = riskMax > 0 ? (int)((riskP / riskMax) * 100) : 0,
                OpportunityScore = oppMax > 0 ? (int)((oppP / oppMax) * 100) : 0,
                RiskNiva = GetLabel(riskMax > 0 ? (int)((riskP / riskMax) * 100) : 0, true),
                OpportunityNiva = GetLabel(oppMax > 0 ? (int)((oppP / oppMax) * 100) : 0, false),
                Signals = signals,
                Datakvalitet = datakvalitet,
                AnvandaNyckeltal = anvandaMetrics,
                MaxNyckeltal = metrics.Count
            };
        }

        private string GetLabel(int score, bool invert)
        {
            if (invert)
            {
                if (score >= 70) return "hög";
                if (score >= 40) return "medel";
                return "låg";
            }
            if (score >= 70) return "hög";
            if (score >= 40) return "medel";
            return "låg";
        }
    }
}
