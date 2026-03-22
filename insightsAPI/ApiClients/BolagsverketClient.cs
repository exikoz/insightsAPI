using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using insightsAPI.Models.Options;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace insightsAPI.ApiClients
{
    public interface IBolagsverketClient
    {
        Task<Dictionary<string, object>?> GetCompanyInfoAsync(string orgnr);
        Task<List<Dictionary<string, object>>> GetDocumentListAsync(string orgnr);
        Task<byte[]> DownloadDocumentAsync(string documentId);
    }

    public class BolagsverketClient : IBolagsverketClient
    {
        private readonly HttpClient _httpClient;
        private readonly BolagsverketOptions _options;
        private readonly HybridCache _cache;
        private readonly ILogger<BolagsverketClient> _logger;

        public BolagsverketClient(HttpClient httpClient, IOptions<BolagsverketOptions> options, HybridCache cache, ILogger<BolagsverketClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _cache = cache;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            return await _cache.GetOrCreateAsync("bolagsverket_token", async token =>
            {
                _logger.LogInformation("Fetching new Bolagsverket access token.");
                
                var authBytes = Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
                var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
                request.Headers.Authorization = authHeader;
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "vardefulla-datamangder:read")
                });

                var response = await _httpClient.SendAsync(request, token);
                response.EnsureSuccessStatusCode();

                var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token));
                var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString()!;
                var expiresIn = jsonDoc.RootElement.GetProperty("expires_in").GetInt32();

                return accessToken;
            });
        }

        private async Task SetApiHeaders(HttpRequestMessage request)
        {
            var token = await GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
        }

        public async Task<Dictionary<string, object>?> GetCompanyInfoAsync(string orgnr)
        {
            var orgnrClean = orgnr.Replace("-", "").Replace(" ", "");
            var url = $"{_options.BaseUrl}/organisationer";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { identitetsbeteckning = orgnrClean }), Encoding.UTF8, "application/json")
            };
            await SetApiHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        }

        public async Task<List<Dictionary<string, object>>> GetDocumentListAsync(string orgnr)
        {
            var orgnrClean = orgnr.Replace("-", "").Replace(" ", "");
            var url = $"{_options.BaseUrl}/dokumentlista";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { identitetsbeteckning = orgnrClean }), Encoding.UTF8, "application/json")
            };
            await SetApiHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<Dictionary<string, object>>();

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content) ?? new List<Dictionary<string, object>>();
        }

        public async Task<byte[]> DownloadDocumentAsync(string documentId)
        {
            var url = $"{_options.BaseUrl}/dokument/{documentId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            await SetApiHeaders(request);
            // Replace Accept for zip
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/zip"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
