using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    /// <summary>
    /// Request body for updating a company's information
    /// </summary>
    public class UpdateCompanyRequestDto
    {
        /// <summary>
        /// The company's registered name
        /// </summary>
        /// <example>Hennes &amp; Mauritz Aktiebolag</example>
        [JsonPropertyName("namn")]
        [Required(ErrorMessage = "Company name (namn) is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Company name must be between 1 and 200 characters.")]
        public required string Namn { get; set; }

        /// <summary>
        /// City where the company's head office is located
        /// </summary>
        /// <example>Stockholm</example>
        [JsonPropertyName("postort")]
        [StringLength(100, ErrorMessage = "Postort must be at most 100 characters.")]
        public string? Postort { get; set; }

        /// <summary>
        /// Whether the company is currently operationally active
        /// </summary>
        /// <example>true</example>
        [JsonPropertyName("aktiv")]
        [Required(ErrorMessage = "Active status (aktiv) is required.")]
        public bool Aktiv { get; set; }
    }
}
