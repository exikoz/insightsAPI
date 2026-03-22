using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace insightsAPI.Models.Entities
{
    public class Company
    {
        [Key]
        [StringLength(10)]
        public required string Orgnr { get; set; }
        
        public string? Namn { get; set; }
        
        [StringLength(5)]
        public string? SniKod { get; set; }
        
        public string? Organisationsform { get; set; }
        
        public string? JurformKod { get; set; }
        
        public string? Postort { get; set; }
        
        public string? Postnr { get; set; }
        
        public DateTime? Registreringsdatum { get; set; }
        
        public DateTime? Avregistreringsdatum { get; set; }
        
        public bool Konkurs { get; set; }
        
        public bool Aktiv { get; set; }
    }
}
