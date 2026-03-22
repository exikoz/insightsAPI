using insightsAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace insightsAPI.Data
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync(string basePath);
    }

    public class DataSeeder : IDatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(string basePath)
        {
            if (await _context.Companies.AnyAsync())
            {
                _logger.LogInformation("Database already seeded. Skipping seeder.");
                return;
            }

            _logger.LogInformation("Starting database seeding sequence...");

            // 1. Seed Companies
            var companiesPath = Path.Combine(basePath, "companies", "companies.parquet");
            if (File.Exists(companiesPath))
            {
                _logger.LogInformation("Seeding Companies from Parquet...");
                await SeedCompaniesAsync(companiesPath);
            }
            else
            {
                _logger.LogWarning("Companies parquet file not found at {Path}", companiesPath);
            }

            // 2. Seed Benchmarks (Riksnyckeltal)
            var benchmarksPath = Path.Combine(basePath, "benchmarks", "riksnyckeltal.parquet");
            if (File.Exists(benchmarksPath))
            {
                _logger.LogInformation("Seeding Benchmarks from Parquet...");
                await SeedBenchmarksAsync(benchmarksPath);
            }
            else
            {
                _logger.LogWarning("Benchmarks parquet file not found at {Path}", benchmarksPath);
            }

            // 3. Seed Facts
            var factsDir = Path.Combine(basePath, "facts");
            if (Directory.Exists(factsDir))
            {
                var files = Directory.GetFiles(factsDir, "*.parquet", SearchOption.AllDirectories);
                _logger.LogInformation("Found {Count} facts parquet files. Triggering seeding...", files.Length);
                _logger.LogInformation("Loading valid Orgnrs for foreign key validation...");
                var validOrgnrs = new HashSet<string>(await _context.Companies.Select(c => c.Orgnr).ToListAsync());
                
                var existingKeys = new HashSet<(string, int, string)>();
                foreach (var file in files)
                {
                    await SeedFactsAsync(file, existingKeys, validOrgnrs);
                }
            }
            else
            {
                _logger.LogWarning("Facts directory not found at {Path}", factsDir);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeding sequence completed.");
        }

        private async Task SeedCompaniesAsync(string filePath)
        {
            using var stream = System.IO.File.OpenRead(filePath);
            using var reader = await ParquetReader.CreateAsync(stream);

            for (int i = 0; i < reader.RowGroupCount; i++)
            {
                using var rowGroupReader = reader.OpenRowGroupReader(i);
                
                // Read all necessary columns
                var orgnrCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "orgnr"));
                var namnCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "namn"));
                var sniKodCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "sni_kod"));
                var orgFormCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "organisationsform"));
                var jurformKodCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "jurform_kod"));
                var postortCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "postort"));
                var postnrCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "postnr"));
                
                var orgnrs = (string[])orgnrCol.Data;
                var namns = (string[])namnCol.Data;
                var sniKods = (string[])sniKodCol.Data;
                var orgForms = (string[])orgFormCol.Data;
                var jurformKods = (string[])jurformKodCol.Data;
                var postorts = (string[])postortCol.Data;
                var postnrs = (string[])postnrCol.Data;

                var dtAktivCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "aktiv"));
                var dtKonkursCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "konkurs"));
                
                // Parquet stores booleans often as bool?[]
                var aktivs = dtAktivCol.Data as bool?[] ?? new bool?[orgnrs.Length];
                var konkurses = dtKonkursCol.Data as bool?[] ?? new bool?[orgnrs.Length];

                var batch = new List<Company>();
                for (int j = 0; j < orgnrs.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(orgnrs[j])) continue;

                    string cleanOrgnr = orgnrs[j].Replace("-", "").Replace(" ", "").Trim();
                    
                    batch.Add(new Company
                    {
                        Orgnr = cleanOrgnr,
                        Namn = namns?.ElementAtOrDefault(j),
                        SniKod = sniKods?.ElementAtOrDefault(j),
                        Organisationsform = orgForms?.ElementAtOrDefault(j),
                        JurformKod = jurformKods?.ElementAtOrDefault(j),
                        Postort = postorts?.ElementAtOrDefault(j),
                        Postnr = postnrs?.ElementAtOrDefault(j),
                        Aktiv = aktivs?.ElementAtOrDefault(j) ?? false,
                        Konkurs = konkurses?.ElementAtOrDefault(j) ?? false
                    });
                }
                await _context.Companies.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
        }

        private async Task SeedBenchmarksAsync(string filePath)
        {
            using var stream = System.IO.File.OpenRead(filePath);
            using var reader = await ParquetReader.CreateAsync(stream);

            for (int i = 0; i < reader.RowGroupCount; i++)
            {
                using var rowGroupReader = reader.OpenRowGroupReader(i);
                
                var sniCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "sni_kod"));
                var klassCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "storleksklass"));
                var romCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "rorelsemarginal_pct"));
                var solCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "soliditet_pct"));
                var skuCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "skuldssattningsgrad"));
                var tillCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "omsattningstillvaxt_pct"));
                var omsCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "omsattning_per_anstalld_tkr"));

                var snis = (string[])sniCol.Data;
                var classes = (string[])klassCol.Data;
                var roms = (double?[])romCol.Data;
                var sols = (double?[])solCol.Data;
                var skus = (double?[])skuCol.Data;
                var tills = (double?[])tillCol.Data;
                var omS = (double?[])omsCol.Data;

                var batch = new List<Benchmark>();
                for (int j = 0; j < snis.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(snis[j]) || string.IsNullOrWhiteSpace(classes[j])) continue;

                    batch.Add(new Benchmark
                    {
                        SniKod = snis[j],
                        Storleksklass = classes[j],
                        RorelsemarginalPct = roms?[j].HasValue == true ? (decimal)roms[j]!.Value : null,
                        SoliditetPct = sols?[j].HasValue == true ? (decimal)sols[j]!.Value : null,
                        Skuldssattningsgrad = skus?[j].HasValue == true ? (decimal)skus[j]!.Value : null,
                        OmsattningstillvaxtPct = tills?[j].HasValue == true ? (decimal)tills[j]!.Value : null,
                        OmsattningPerAnstalldTkr = omS?[j].HasValue == true ? (decimal)omS[j]!.Value : null,
                    });
                }

                await _context.Benchmarks.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
        }

        private async Task SeedFactsAsync(string filePath, HashSet<(string, int, string)> existingKeys, HashSet<string> validOrgnrs)
        {
            using var stream = System.IO.File.OpenRead(filePath);
            using var reader = await ParquetReader.CreateAsync(stream);

            for (int i = 0; i < reader.RowGroupCount; i++)
            {
                using var rowGroupReader = reader.OpenRowGroupReader(i);
                
                var orgnrCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "orgnr"));
                var batchCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "ars_batch"));
                var tagCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "tag"));
                var valCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "value_raw"));
                var decCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "decimals"));
                var signCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "sign"));
                var contextCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "context_ref"));
                var typeCol = await rowGroupReader.ReadColumnAsync(reader.Schema.DataFields.First(f => f.Name == "data_type"));

                var orgnrs = (string[])orgnrCol.Data;
                // handle dynamic cast since parquet longs map to longs, etc.
                var batches = ConvertToIntArray(batchCol.Data);
                var tags = (string[])tagCol.Data;
                var vals = (string[])valCol.Data;
                var decs = (string?[])decCol.Data;
                var signs = (string?[])signCol.Data;
                var contexts = (string?[])contextCol.Data;
                var types = (string?[])typeCol.Data;

                var wantedTags = new Dictionary<string, string>
                {
                    { "se-gen-base:Nettoomsattning", "omsattning" },
                    { "se-gen-base:Rorelseresultat", "rorelseresultat" },
                    { "se-gen-base:AretsResultat", "arets_resultat" },
                    { "se-gen-base:Tillgangar", "tillgangar" },
                    { "se-gen-base:EgetKapital", "eget_kapital" },
                    { "se-gen-base:KortfristigaSkulder", "kortfristiga_skulder" },
                    { "se-gen-base:Skulder", "skulder" },
                    { "se-gen-base:Personalkostnader", "personalkostnader" },
                    { "se-gen-base:MedelantaletAnstallda", "anstallda" },
                    { "se-gen-base:MedelantaletAnstalldaMan", "anstallda_man" },
                    { "se-gen-base:MedelantaletAnstalldaKvinnor", "anstallda_kvinna" },
                    { "se-gen-base:ResultatEfterFinansiellaPoster", "resultat_efter_fin" }
                };

                var batch = new List<Fact>();
                for (int j = 0; j < orgnrs.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(orgnrs[j]) || string.IsNullOrWhiteSpace(tags[j])) continue;
                    if (types[j] != "numeric") continue;
                    if (contexts[j] != "period0" && contexts[j] != "balans0" && !string.IsNullOrEmpty(contexts[j])) continue;
                    if (!wantedTags.TryGetValue(tags[j], out var tagNamn)) continue;

                    string cleanOrgnr = orgnrs[j].Replace("-", "").Replace(" ", "").Trim();
                    if (!validOrgnrs.Contains(cleanOrgnr)) continue;
                    
                    // Parse values
                    string cleanVal = vals[j]?.Replace(" ", "") ?? "";
                    if (!decimal.TryParse(cleanVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valueNum))
                        continue;

                    decimal signNum = 1;
                    if (!string.IsNullOrEmpty(signs[j]) && decimal.TryParse(signs[j], out decimal s)) signNum = s;

                    int decimalsNum = 0;
                    if (!string.IsNullOrEmpty(decs[j]) && int.TryParse(decs[j], out int d)) decimalsNum = d;

                    // Fix: employee tags shouldn't scale up
                    if (tags[j] == "se-gen-base:MedelantaletAnstallda" || 
                        tags[j] == "se-gen-base:MedelantaletAnstalldaMan" || 
                        tags[j] == "se-gen-base:MedelantaletAnstalldaKvinnor")
                    {
                        if (decimalsNum > 0) decimalsNum = 0;
                    }

                    decimal valKr = valueNum * (decimal)Math.Pow(10, decimalsNum) * signNum;

                    // Exists check or hash sets can be tracked, but using AddRange
                    batch.Add(new Fact
                    {
                        Orgnr = cleanOrgnr,
                        ArsBatch = batches[j],
                        TagNamn = tagNamn,
                        ValueKr = valKr
                    });
                }
                
                // Keep the one closest to 0 decimals on duplicates
                var distinctBatch = batch.OrderBy(f => Math.Abs(f.ValueKr)).GroupBy(f => new { f.Orgnr, f.ArsBatch, f.TagNamn }).Select(g => g.First()).Where(f => existingKeys.Add((f.Orgnr, f.ArsBatch, f.TagNamn))).ToList();
                await _context.Facts.AddRangeAsync(distinctBatch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
        }
        
        private int[] ConvertToIntArray(Array data)
        {
            var result = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                var val = data.GetValue(i);
                result[i] = val == null ? 0 : Convert.ToInt32(val);
            }
            return result;
        }
    }
}
