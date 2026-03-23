namespace insightsAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a restrictive CORS policy that only allows requests from specific origins.
        /// Does NOT use AllowAnyOrigin.
        /// </summary>
        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
