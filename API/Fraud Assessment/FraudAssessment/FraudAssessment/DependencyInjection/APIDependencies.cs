namespace FraudAssessment.API.DependencyInjection
{
    public static class APIDependencies
    {
        public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
        {
            #region CORS
            services.AddCors(options =>
                     options.AddPolicy("CorsPolicy", policyBuilder =>
                         policyBuilder.SetIsOriginAllowed(_ => true)

                                      .AllowAnyMethod()
                                      .AllowAnyHeader().AllowCredentials())

                 );
            #endregion
            return services;

        }
    }
}
