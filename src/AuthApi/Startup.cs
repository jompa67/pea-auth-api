using Amazon.KeyManagementService;
using Amazon.SecretsManager;
using AuthApi.Extensions;
using AuthApi.Services;
using AuthApi.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Reflection;
using Amazon.Extensions.NETCore.Setup;
using AuthApi.Repositories;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Interface;
using AuthApi.Settings;

namespace AuthApi;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    private static void ConfigureLogging(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("app", "jm-auth-api")
            .CreateLogger();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureLogging(Configuration);

        services
            .AddDynamoDb()
            .AddAWSService<IAmazonKeyManagementService>()
            .AddAWSService<IAmazonSecretsManager>()
            .AddSingleton<ISecretsManagerService, SecretsManagerService>()
            .AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>()
            .AddSingleton<IUserProfileRepository, UserProfileRepository>()
            .AddSingleton<IUserLoginRepository, UserLoginRepository>()
            .AddSingleton<ILoginService, LoginService>()
            .AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>()
            .AddScoped<IEmailService, EmailService>()
            .AddScoped<IVerificationTokenService, VerificationTokenService>()
            .AddEndpointsApiExplorer()
            .AddSwaggerWithJwtAuth()
            .AddSingleton<IBCryptWrapper, BCryptWrapper>() // Add BCryptWrapper service
            .AddSerilog()
            .AddJwtAuthentication(Configuration)
            .AddControllers()
            .AddFluentValidation(fv =>
            {
                fv.RegisterValidatorsFromAssemblyContaining<LoginRequestValidator>();
                fv.DisableDataAnnotationsValidation = true;
                fv.ImplicitlyValidateChildProperties = true;
            });
        
        services
            .AddOptions<EmailSettings>()
            .Configure<ILogger<Program>>((settings, logger) =>
            {
                Configuration.GetSection("EmailSettings").Bind(settings);
                logger.LogInformation("Email settings loaded: SMTP Server={Server}, Port={Port}, SSL={SSL}",
                    settings.SmtpServer, settings.SmtpPort, settings.EnableSsl);
            })
            .ValidateFluentValidation();
        
        services.Configure<ValidationSettings>(Configuration.GetSection("ValidationSettings"));

        // Register all validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
            .UseSwagger()
            .UseSwaggerUI()
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}