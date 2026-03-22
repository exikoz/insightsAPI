using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    public class CompanyAnalysisResultDto
    {
        [JsonPropertyName("ar")] public int Ar { get; set; }
        [JsonPropertyName("omsattning")] public decimal? Omsattning { get; set; }
        [JsonPropertyName("rorelseresultat")] public decimal? Rorelseresultat { get; set; }
        [JsonPropertyName("tillgangar")] public decimal? Tillgangar { get; set; }
        [JsonPropertyName("eget_kapital")] public decimal? EgetKapital { get; set; }
        [JsonPropertyName("anstallda")] public int? Anstallda { get; set; }
        [JsonPropertyName("personalkostnad")] public decimal? Personalkostnad { get; set; }
        
        [JsonIgnore] public string? StorleksklassKalla { get; set; }
        [JsonPropertyName("storleksklass")] public string? Storleksklass { get; set; }
        [JsonIgnore] public string? AnvandStorleksklass { get; set; }
        [JsonIgnore] public bool KlassApproximerad { get; set; }

        [JsonPropertyName("rorelsemarginal_pct")] public decimal? RorelsemarginalPct { get; set; }
        [JsonPropertyName("soliditet_pct")] public decimal? SoliditetPct { get; set; }
        [JsonPropertyName("skuldsattning")] public decimal? Skuldsattning { get; set; }
        [JsonPropertyName("omsattningstillvaxt_pct")] public decimal? OmsattningstillvaxtPct { get; set; }
        [JsonPropertyName("omsattning_per_anstalld")] public decimal? OmsattningPerAnstalld { get; set; }
        [JsonPropertyName("personalkostnad_per_anstalld")] public decimal? PersonalkostnadPerAnstalld { get; set; }
        
        [JsonPropertyName("jamforelser")] public List<ComparisonDto>? Jamforelser { get; set; }
    }

    public class ComparisonDto
    {
        [JsonPropertyName("nyckeltal")] public required string Nyckeltal { get; set; }
        [JsonPropertyName("enhet")] public required string Enhet { get; set; }
        [JsonPropertyName("bolag")] public decimal? Bolag { get; set; }
        [JsonPropertyName("bransch_median")] public decimal? BranschMedian { get; set; }
        [JsonPropertyName("diff")] public decimal? Diff { get; set; }
        [JsonPropertyName("signal")] public string? Signal { get; set; }
        [JsonPropertyName("orsak")] public string? Orsak { get; set; }
        [JsonPropertyName("higher_is_better")] public bool HigherIsBetter { get; set; }
    }

    public class ScoreResultDto
    {
        [JsonPropertyName("risk_score")] public int? RiskScore { get; set; }
        [JsonPropertyName("opportunity_score")] public int? OpportunityScore { get; set; }
        [JsonPropertyName("risk_niva")] public string? RiskNiva { get; set; }
        [JsonPropertyName("opportunity_niva")] public string? OpportunityNiva { get; set; }
        [JsonPropertyName("signals")] public required List<SignalDto> Signals { get; set; }
        [JsonPropertyName("datakvalitet")] public required string Datakvalitet { get; set; }
        [JsonPropertyName("anvanda_nyckeltal")] public int AnvandaNyckeltal { get; set; }
        [JsonPropertyName("max_nyckeltal")] public int MaxNyckeltal { get; set; }
    }
}
