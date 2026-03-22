using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace insightsAPI.Models.Entities
{
    public class Fact
    {
        [StringLength(10)]
        public required string Orgnr { get; set; }

        public required int ArsBatch { get; set; }

        [StringLength(100)]
        public required string TagNamn { get; set; }

        public decimal ValueKr { get; set; }
    }
}
