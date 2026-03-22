namespace insightsAPI.Models.Options
{
    public class BolagsverketOptions
    {
        public const string SectionName = "Bolagsverket";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Env { get; set; } = "prod";
        public string ProdBaseUrl { get; set; } = "https://gw.api.bolagsverket.se/vardefulla-datamangder/v1";
        public string TestBaseUrl { get; set; } = "https://gw-accept2.api.bolagsverket.se/vd/v1";
        public string TokenUrl { get; set; } = "https://portal.api.bolagsverket.se/oauth2/token";
        
        public string BaseUrl => Env.ToLower() == "test" ? TestBaseUrl : ProdBaseUrl;
    }
}
