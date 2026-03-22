using insightsAPI.Models.DTOs;
using insightsAPI.Models.Entities;
using insightsAPI.Services;
using System.Collections.Generic;
using Xunit;

namespace insightsAPI.Tests
{
    public class FinancialAnalyzerServiceTests
    {
        private readonly FinancialAnalyzerService _service;

        public FinancialAnalyzerServiceTests()
        {
            _service = new FinancialAnalyzerService();
        }

        [Fact]
        public void ComputeKpis_ShouldHandleDivisionByZero_Safely()
        {
            var company = new Company { Orgnr = "5560000000" };
            var facts = new List<Fact>
            {
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "rorelseresultat", ValueKr = 1000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "omsattning", ValueKr = 0m }, // zero!
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "eget_kapital", ValueKr = 500m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "tillgangar", ValueKr = 0m } // zero!
            };

            var results = _service.ComputeKpis(facts, company);

            Assert.Single(results);
            Assert.Null(results[0].RorelsemarginalPct);
            Assert.Null(results[0].SoliditetPct);
            Assert.Equal(500m, results[0].EgetKapital);
        }

        [Fact]
        public void ComputeKpis_ShouldCalculateDerivedValuesCorrectly()
        {
            var company = new Company { Orgnr = "5560000000" };
            var facts = new List<Fact>
            {
                new Fact { Orgnr = "5560000000", ArsBatch = 2022, TagNamn = "omsattning", ValueKr = 100000m },

                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "omsattning", ValueKr = 150000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "rorelseresultat", ValueKr = 15000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "tillgangar", ValueKr = 200000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "eget_kapital", ValueKr = 50000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "anstallda", ValueKr = 10m }
            };

            var results = _service.ComputeKpis(facts, company);

            Assert.Equal(2, results.Count); // 2022 and 2023
            
            var res2023 = results.Find(r => r.Ar == 2023);
            Assert.NotNull(res2023);
            
            Assert.Equal(10m, res2023.RorelsemarginalPct); // 15k / 150k * 100
            Assert.Equal(25m, res2023.SoliditetPct); // 50k / 200k * 100
            Assert.Equal(3m, res2023.Skuldsattning); // (200k - 50k) / 50k = 3
            Assert.Equal(50m, res2023.OmsattningstillvaxtPct); // (150k - 100k) / 100k * 100 = 50%
            Assert.Equal(15m, res2023.OmsattningPerAnstalld); // 150000 / 10 * 0.001 = 15.00 tkr
        }

        [Fact]
        public void ComputeKpis_ResolveAnstallda_PriorityChecks()
        {
            var company = new Company { Orgnr = "5560000000" };
            var factsWithoutAnstallda = new List<Fact>
            {
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "omsattning", ValueKr = 150000m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "anstallda_man", ValueKr = 12m },
                new Fact { Orgnr = "5560000000", ArsBatch = 2023, TagNamn = "anstallda_kvinna", ValueKr = 8m }
            };

            var results = _service.ComputeKpis(factsWithoutAnstallda, company);

            Assert.Single(results);
            Assert.Equal(20, results[0].Anstallda); // 12 + 8
            Assert.Equal("20_49_anst", results[0].Storleksklass);
            Assert.Equal("iXBRL (man+kvinna)", results[0].StorleksklassKalla);
        }
    }
}
