using insightsAPI.Models.DTOs;
using insightsAPI.Models.Entities;

namespace insightsAPI.Services
{
    public interface IFinancialAnalyzerService
    {
        List<CompanyAnalysisResultDto> ComputeKpis(List<Fact> facts, Company company);
        List<CompanyAnalysisResultDto> CompareToBenchmark(List<CompanyAnalysisResultDto> kpis, Dictionary<string, Benchmark> benchmarks);
    }

    public class FinancialAnalyzerService : IFinancialAnalyzerService
    {
        public List<CompanyAnalysisResultDto> ComputeKpis(List<Fact> facts, Company company)
        {
            if (facts == null || !facts.Any()) return new List<CompanyAnalysisResultDto>();

            var pivot = facts.GroupBy(f => f.ArsBatch)
                             .ToDictionary(g => g.Key, g => g.ToDictionary(f => f.TagNamn, f => f.ValueKr));

            var years = pivot.Keys.OrderBy(y => y).ToList();
            var results = new List<CompanyAnalysisResultDto>();

            for (int i = 0; i < years.Count; i++)
            {
                var year = years[i];
                var yearData = pivot[year];
                var prevYearData = i > 0 ? pivot[years[i - 1]] : null;

                var omsattning = GetVal(yearData, "omsattning");
                var rorelseresultat = GetVal(yearData, "rorelseresultat");
                var tillgangar = GetVal(yearData, "tillgangar");
                var egetKapital = GetVal(yearData, "eget_kapital");
                var personalkostnad = GetVal(yearData, "personalkostnader");

                var (anstallda, klass, kalla) = ResolveAnstallda(yearData, company);

                var rorelsemarginal = SafeDiv(rorelseresultat, omsattning, 100);
                var soliditet = SafeDiv(egetKapital, tillgangar, 100);
                var skuldsattning = SafeDiv((tillgangar.HasValue && egetKapital.HasValue) ? (tillgangar - egetKapital) : null, egetKapital);

                decimal? omsattningTillvaxt = null;
                if (prevYearData != null)
                {
                    var prevOm = GetVal(prevYearData, "omsattning");
                    omsattningTillvaxt = SafeDiv((omsattning.HasValue && prevOm.HasValue) ? (omsattning - prevOm) : null, prevOm, 100);
                }

                var omsattningPerAnstalld = SafeDiv(omsattning, anstallda, 0.001m);
                var personalKostnadPerAnstalld = SafeDiv(personalkostnad, anstallda, 0.001m);

                results.Add(new CompanyAnalysisResultDto
                {
                    Ar = year,
                    Omsattning = omsattning,
                    Rorelseresultat = rorelseresultat,
                    Tillgangar = tillgangar,
                    EgetKapital = egetKapital,
                    Personalkostnad = personalkostnad,
                    Anstallda = (int?)anstallda,
                    StorleksklassKalla = kalla,
                    Storleksklass = klass,

                    RorelsemarginalPct = Round(rorelsemarginal),
                    SoliditetPct = Round(soliditet),
                    Skuldsattning = Round(skuldsattning),
                    OmsattningstillvaxtPct = Round(omsattningTillvaxt),
                    OmsattningPerAnstalld = Round(omsattningPerAnstalld),
                    PersonalkostnadPerAnstalld = Round(personalKostnadPerAnstalld)
                });
            }

            return results;
        }

        public List<CompanyAnalysisResultDto> CompareToBenchmark(List<CompanyAnalysisResultDto> kpis, Dictionary<string, Benchmark> benchmarks)
        {
            foreach (var row in kpis)
            {
                var klass = row.Storleksklass ?? "TOT";
                var anvandKlass = benchmarks.ContainsKey(klass) ? klass : "TOT";
                row.KlassApproximerad = anvandKlass != klass;
                
                if (!benchmarks.TryGetValue(anvandKlass, out var benchmark)) continue;

                row.AnvandStorleksklass = anvandKlass;

                row.Jamforelser = new List<ComparisonDto>();

                AddComparison(row.Jamforelser, "Rörelsemarginal", "%", true, false, row.RorelsemarginalPct, benchmark.RorelsemarginalPct);
                AddComparison(row.Jamforelser, "Soliditet", "%", true, false, row.SoliditetPct, benchmark.SoliditetPct);
                AddComparison(row.Jamforelser, "Skuldsättning", "x", false, false, row.Skuldsattning, benchmark.Skuldssattningsgrad);
                AddComparison(row.Jamforelser, "Omsättningstillväxt", "%", true, false, row.OmsattningstillvaxtPct, benchmark.OmsattningstillvaxtPct);
                AddComparison(row.Jamforelser, "Omsättning/anställd", "tkr", true, true, row.OmsattningPerAnstalld, benchmark.OmsattningPerAnstalldTkr);
            }
            return kpis;
        }

        private void AddComparison(List<ComparisonDto> list, string label, string unit, bool hib, bool kravAnstallda, decimal? bolagVal, decimal? branschVal)
        {
            var comp = new ComparisonDto
            {
                Nyckeltal = label,
                Enhet = unit,
                Bolag = bolagVal,
                BranschMedian = branschVal,
                HigherIsBetter = hib
            };

            if (bolagVal == null)
            {
                comp.Signal = "no_data";
                comp.Orsak = kravAnstallda ? "Antal anställda saknas i årsredovisningen" : "Värde saknas i årsredovisningen";
                list.Add(comp);
                return;
            }

            if (branschVal == null)
            {
                comp.Signal = "no_benchmark";
                comp.Orsak = "Branschsnitt saknas";
                list.Add(comp);
                return;
            }

            comp.Diff = Math.Round(bolagVal.Value - branschVal.Value, 2);
            comp.Signal = GetSignalString(comp.Diff.Value, hib);
            list.Add(comp);
        }

        private string GetSignalString(decimal diff, bool hib)
        {
            if (hib ? diff > 0 : diff < 0) return "positive";
            if (diff == 0) return "neutral";
            return "negative";
        }

        private (decimal?, string, string) ResolveAnstallda(Dictionary<string, decimal> dict, Company comp)
        {
            if (dict.TryGetValue("anstallda", out var a)) return (a, GetStorleksklass(a), "iXBRL");
            
            var m = dict.TryGetValue("anstallda_man", out var man) ? (decimal?)man : null;
            var w = dict.TryGetValue("anstallda_kvinna", out var wom) ? (decimal?)wom : null;

            if (m.HasValue && w.HasValue) return (m + w, GetStorleksklass(m + w), "iXBRL (man+kvinna)");
            if (m.HasValue) return (m, GetStorleksklass(m), "iXBRL (man+kvinna)");
            if (w.HasValue) return (w, GetStorleksklass(w), "iXBRL (man+kvinna)");

            return (null, "TOT", "okänd");
        }

        private string GetStorleksklass(decimal? anstallda)
        {
            if (anstallda == null) return "TOT";
            if (anstallda <= 19) return "1_19_anst";
            if (anstallda <= 49) return "20_49_anst";
            return "TOT";
        }

        private decimal? GetVal(Dictionary<string, decimal> d, string key) => d.TryGetValue(key, out var v) ? v : (decimal?)null;

        private decimal? SafeDiv(decimal? num, decimal? den, decimal scale = 1m)
        {
            if (num == null || den == null || Math.Abs(den.Value) < 0.001m) return null;
            return (num.Value / den.Value) * scale;
        }

        private decimal? Round(decimal? val) => val.HasValue ? Math.Round(val.Value, 2) : null;
    }
}
