using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FraudAssessment.Application
{
    public static class ApplicationDependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                          cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));

            services.AddAutoMapper(cfg =>
                cfg.AddMaps(typeof(ApplicationDependencyInjection).Assembly));
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());


            return services;

        }
    }
}