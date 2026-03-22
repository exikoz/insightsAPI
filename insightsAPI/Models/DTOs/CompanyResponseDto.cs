using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    /// <summary>
    /// Represents basic metadata for a public company
    /// </summary>
    public class CompanyResponseDto
    {
        /// <summary>
        /// The 10-digit organization number
        /// </summary>
        /// <example>5560360793</example>
        [JsonPropertyName("orgnr")]
        public required string Orgnr { get; set; }
        
        /// <summary>
        /// The company's registered name
        /// </summary>
        /// <example>Hennes &amp; Mauritz Aktiebolag</example>
        [JsonPropertyName("namn")]
        public string? Namn { get; set; }
        
        /// <summary>
        /// The company's primary industry according to Standard for Swedish Industrial Classification (SNI)
        /// </summary>
        /// <example>47711</example>
        [JsonPropertyName("sni_kod")]
        public string? SniKod { get; set; }
        
        /// <summary>
        /// Descriptive organizational form
        /// </summary>
        /// <example>Aktiebolag</example>
        [JsonPropertyName("organisationsform")]
        public string? Organisationsform { get; set; }
        
        /// <summary>
        /// Legal structure code according to Bolagsverket
        /// </summary>
        /// <example>41</example>
        [JsonPropertyName("jurform_kod")]
        public string? JurformKod { get; set; }
        
        /// <summary>
        /// City where the company's head office is located
        /// </summary>
        /// <example>Stockholm</example>
        [JsonPropertyName("postort")]
        public string? Postort { get; set; }
        
        /// <summary>
        /// Zip code linked to the company's seat
        /// </summary>
        /// <example>10638</example>
        [JsonPropertyName("postnr")]
        public string? Postnr { get; set; }
        
        /// <summary>
        /// The date when the company was officially created at Bolagsverket
        /// </summary>
        /// <example>1947-01-01T00:00:00Z</example>
        [JsonPropertyName("registreringsdatum")]
        public DateTime? Registreringsdatum { get; set; }
        
        /// <summary>
        /// Date when the company was potentially closed or deregistered
        /// </summary>
        /// <example>null</example>
        [JsonPropertyName("avregistreringsdatum")]
        public DateTime? Avregistreringsdatum { get; set; }
        
        /// <summary>
        /// Indicates if the company has been declared bankrupt
        /// </summary>
        /// <example>false</example>
        [JsonPropertyName("konkurs")]
        public bool Konkurs { get; set; }
        
        /// <summary>
        /// Indicates if the company is currently operationally active
        /// </summary>
        /// <example>true</example>
        [JsonPropertyName("aktiv")]
        public bool Aktiv { get; set; }
    }
}
