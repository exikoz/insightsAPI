using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    /// <summary>
    /// Contains data about average industry performance
    /// </summary>
    public class BenchmarkResponseDto
    {
        /// <summary>
        /// The industry's identifying SNI code
        /// </summary>
        /// <example>62010</example>
        [JsonPropertyName("sni_kod")]
        public required string SniKod { get; set; }

        /// <summary>
        /// Target group for the statistics based on number of employees (e.g., 'TOT', '1_19_anst')
        /// </summary>
        /// <example>TOT</example>
        [JsonPropertyName("storleksklass")]
        public required string Storleksklass { get; set; }

        /// <summary>
        /// The average operating margin for the entire industry
        /// </summary>
        /// <example>8.5</example>
        [JsonPropertyName("rorelsemarginal_pct")]
        public decimal? RorelsemarginalPct { get; set; }
        
        /// <summary>
        /// Average equity ratio (%) in the industry
        /// </summary>
        /// <example>35.2</example>
        [JsonPropertyName("soliditet_pct")]
        public decimal? SoliditetPct { get; set; }
        
        /// <summary>
        /// Debt-to-equity ratio for the average company in the group
        /// </summary>
        /// <example>1.5</example>
        [JsonPropertyName("skuldssattningsgrad")]
        public decimal? Skuldssattningsgrad { get; set; }
        
        /// <summary>
        /// Annual growth (%) for the industry's total turnover
        /// </summary>
        /// <example>4.2</example>
        [JsonPropertyName("omsattningstillvaxt_pct")]
        public decimal? OmsattningstillvaxtPct { get; set; }
        
        /// <summary>
        /// Average annual revenue per employee in thousands of SEK (TKR)
        /// </summary>
        /// <example>1450.5</example>
        [JsonPropertyName("omsattning_per_anstalld_tkr")]
        public decimal? OmsattningPerAnstalldTkr { get; set; }
    }
}
