using FraudAssessment.API.DependencyInjection;
using FraudAssessment.API.Middlewares;
using FraudAssessment.Application;
using FraudAssessment.Infrastructure.DependencyInjection;
using FraudAssessment.Infrastructure.Messaging.RabbitMq;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using Serilog;
namespace FraudAssessment
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Configuration.AddYamlFile(
               "appsettings.Development.yml",
                  optional: true,
            reloadOnChange: true);


                #region Logging
                var logLevel = builder.Environment.IsDevelopment()
               ? Serilog.Events.LogEventLevel.Debug
                : Serilog.Events.LogEventLevel.Information;
                Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Is(logLevel)

      // Ignore framework Debug/Information noise
      .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
      .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)

      .WriteTo.Console()
      .WriteTo.File(
          path: "Logs/log-.txt",
          rollingInterval: RollingInterval.Day,
          retainedFileCountLimit: 30,
          shared: true,
          outputTemplate:
          "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
      .CreateLogger();

                builder.Host.UseSerilog();
                #endregion 




                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                #region Dependency Injection
                builder.Services.AddInfrastructure(builder.Configuration).AddApi(builder.Configuration).AddApplication();
                #endregion


                var app = builder.Build();

                #region Database and Messaging Initialization
                using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider
                        .GetRequiredService<MongoDbInitializer>();

                    await initializer.InitializeAsync();

                    var rabbitInitializer = scope.ServiceProvider
                   .GetRequiredService<RabbitMqTopologyInitializer>();

                    await rabbitInitializer.InitializeAsync();
                }
                #endregion


                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                app.UseMiddleware<GlobalExceptionMiddleware>();
                app.UseAuthorization();


                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}