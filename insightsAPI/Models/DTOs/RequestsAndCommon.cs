using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    public class AnalyzeRequestDto
    {
        [JsonPropertyName("ar")]
        public int? Ar { get; set; }

        [JsonPropertyName("storleksklass")]
        public string Storleksklass { get; set; } = "TOT";
    }

    public class PortfolioRequestDto
    {
        [JsonPropertyName("orgnr_lista")]
        public required List<string> OrgnrLista { get; set; }

        [JsonPropertyName("sortera_efter")]
        public string SorteraEfter { get; set; } = "risk_score";

        [JsonPropertyName("sortera_fallande")]
        public bool SorteraFallande { get; set; } = true;
    }

    public class SignalDto
    {
        [JsonPropertyName("typ")]
        public required string Typ { get; set; }

        [JsonPropertyName("label")]
        public required string Label { get; set; }

        [JsonPropertyName("meddelande")]
        public required string Meddelande { get; set; }
    }
}
