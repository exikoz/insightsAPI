using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace insightsAPI.Models.Entities
{
    public class Benchmark
    {
        [StringLength(50)]
        public required string SniKod { get; set; }

        [StringLength(20)]
        public required string Storleksklass { get; set; } // "TOT", "1_19_anst", "20_49_anst"

        public decimal? RorelsemarginalPct { get; set; }
        
        public decimal? SoliditetPct { get; set; }
        
        public decimal? Skuldssattningsgrad { get; set; }
        
        public decimal? OmsattningstillvaxtPct { get; set; }
        
        public decimal? OmsattningPerAnstalldTkr { get; set; }
    }
}
