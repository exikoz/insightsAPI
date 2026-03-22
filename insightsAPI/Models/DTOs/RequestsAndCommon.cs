using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    /// <summary>
    /// Parameters for a financial analysis
    /// </summary>
    public class AnalyzeRequestDto
    {
        /// <summary>
        /// Specific year to analyze (leave empty for the latest available year)
        /// </summary>
        /// <example>2023</example>
        [JsonPropertyName("ar")]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
        public int? Ar { get; set; }

        /// <summary>
        /// Filter the analysis based on company size in relation to SCB industry averages
        /// </summary>
        /// <example>TOT</example>
        [JsonPropertyName("storleksklass")]
        [StringLength(20, ErrorMessage = "Size class must be maximum 20 characters.")]
        public string Storleksklass { get; set; } = "TOT";
    }

    /// <summary>
    /// Represents a request for a bulk analysis of a company portfolio
    /// </summary>
    public class PortfolioRequestDto
    {
        /// <summary>
        /// A list of up to 200 company organization numbers
        /// </summary>
        /// <example>["5560360793", "5560128085"]</example>
        [JsonPropertyName("orgnr_lista")]
        [Required(ErrorMessage = "OrgnrLista is required.")]
        [MinLength(1, ErrorMessage = "The list must contain at least one organization number.")]
        [MaxLength(200, ErrorMessage = "The list can contain a maximum of 200 organization numbers.")]
        public required List<string> OrgnrLista { get; set; }

        /// <summary>
        /// The field to sort the final results by (default is risk_score)
        /// </summary>
        /// <example>risk_score</example>
        [JsonPropertyName("sortera_efter")]
        [StringLength(50)]
        public string SorteraEfter { get; set; } = "risk_score";

        /// <summary>
        /// If true, sorts the field descending (highest to lowest)
        /// </summary>
        /// <example>true</example>
        [JsonPropertyName("sortera_fallande")]
        public bool SorteraFallande { get; set; } = true;
    }

    /// <summary>
    /// Shows a signal-generated observation based on data
    /// </summary>
    public class SignalDto
    {
        /// <summary>
        /// The type of signal (risk, opportunity, or information)
        /// </summary>
        /// <example>risk</example>
        [JsonPropertyName("typ")]
        public required string Typ { get; set; }

        /// <summary>
        /// Short label for what the signal concerns
        /// </summary>
        /// <example>Falling Margin</example>
        [JsonPropertyName("label")]
        public required string Label { get; set; }

        /// <summary>
        /// Detailed text description of the business signal
        /// </summary>
        /// <example>The operating margin has decreased three years in a row by more than 30%.</example>
        [JsonPropertyName("meddelande")]
        public required string Meddelande { get; set; }
    }
}
