using insightsAPI.Models.DTOs;
using insightsAPI.Models.Entities;
using insightsAPI.Services;
using System.Collections.Generic;
using Xunit;

namespace insightsAPI.Tests
{
    public class ScoringEngineTests
    {
        private readonly ScoringEngine _engine;

        public ScoringEngineTests()
        {
            _engine = new ScoringEngine();
        }

        [Fact]
        public void ComputeScores_ShouldGiveHighRiskForNegativeSignals()
        {
            var benchmarks = new Dictionary<string, Benchmark>
            {
                { "TOT", new Benchmark { SniKod = "123", Storleksklass = "TOT", RorelsemarginalPct = 10, SoliditetPct = 40 } }
            };

            var results = new List<CompanyAnalysisResultDto>
            {
                new CompanyAnalysisResultDto
                {
                    Ar = 2023,
                    AnvandStorleksklass = "TOT",
                    RorelsemarginalPct = 0, // Lower than 10 (High Risk)
                    SoliditetPct = 20 // Lower than 40 (High Risk)
                }
            };

            var score = _engine.ComputeScores(results, benchmarks);

            Assert.True(score.RiskScore > 0);
            Assert.Equal(0, score.OpportunityScore);
        }

        [Fact]
        public void ComputeScores_MissingValues_ReducesDataQuality()
        {
            var benchmarks = new Dictionary<string, Benchmark>
            {
                { "TOT", new Benchmark { SniKod = "123", Storleksklass = "TOT", RorelsemarginalPct = 10 } }
            };

            var results = new List<CompanyAnalysisResultDto>
            {
                new CompanyAnalysisResultDto
                {
                    Ar = 2023,
                    AnvandStorleksklass = "TOT",
                    RorelsemarginalPct = 15,
                    SoliditetPct = null // Missing
                }
            };

            var score = _engine.ComputeScores(results, benchmarks);

            Assert.Equal("partiell_data", score.Datakvalitet); // 1 out of 4 is missing => "partiell_data"
            Assert.Equal(1, score.AnvandaNyckeltal);
        }
    }
}
