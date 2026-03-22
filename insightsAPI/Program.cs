using Microsoft.EntityFrameworkCore;
using insightsAPI.Data;
using insightsAPI.Middleware;
using insightsAPI.Models.Options;
using insightsAPI.ApiClients;
using insightsAPI.Services;
var builder = WebApplication.CreateBuilder(args);

// Options Pattern
builder.Services.Configure<BolagsverketOptions>(builder.Configuration.GetSection(BolagsverketOptions.SectionName));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

// DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=InsightsAPI;Trusted_Connection=True;"));

// Services & Seeders
builder.Services.AddScoped<IDatabaseSeeder, DataSeeder>();
builder.Services.AddScoped<IFinancialAnalyzerService, FinancialAnalyzerService>();
builder.Services.AddScoped<IScoringEngine, ScoringEngine>();
builder.Services.AddScoped<ICompanyInsightService, CompanyInsightService>();

// .NET 9 Hybrid Cache
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018


// HTTP Clients (Basic scaffold)
builder.Services.AddHttpClient<IBolagsverketClient, BolagsverketClient>()
    .AddStandardResilienceHandler();
    
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>()
    .AddStandardResilienceHandler(); // Adds retry, timeout, etc automatically.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run Seeder on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // context.Database.EnsureDeleted();
    context.Database.EnsureCreated(); // Just for MVP / dev
    
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    var pythonDataPath = Path.Combine(Directory.GetParent(app.Environment.ContentRootPath)!.FullName, "python", "data");
    await seeder.SeedAsync(pythonDataPath);
}

// Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

