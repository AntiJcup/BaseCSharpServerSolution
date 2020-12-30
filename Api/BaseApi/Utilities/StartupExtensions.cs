using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Amazon.S3;
using BaseApi.Storage;
using System;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Authorization;
using BaseApi.Utilities.AWS.Auth;
using BaseApi.Auth;
using Microsoft.EntityFrameworkCore;
using BaseApi.Services;

namespace BaseApi
{
    public static class Startup
    {
        public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            var corsDomains = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string[]>(Constants.Configuration.Sections.Settings.CorsDomainArrayKey);
            return services.AddCors(options =>
            {
                options.AddPolicy("baseapi",
                    builder =>
                    {
                        builder.WithOrigins(corsDomains).AllowAnyHeader().AllowAnyMethod();
                    });
            });
        }

        public static IServiceCollection ConfigureMicrosoftSQLService<TDBContext>(this IServiceCollection services, IConfiguration configuration)
            where TDBContext : TemplateMicrosoftSQLDbContext, new()
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            connectionString = connectionString.Replace("<UID>", Environment.GetEnvironmentVariable("SQL_UID"));
            connectionString = connectionString.Replace("<PWD>", Environment.GetEnvironmentVariable("SQL_PWD"));
            services.AddDbContext<TDBContext>(item => item.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("MicrosoftSQL"))
                );

            return services.AddMicrosoftSQLDBDataAccessLayer<TDBContext>();
        }

        public static IServiceCollection ConfigureAccountService(this IServiceCollection services)
        {
            return services.AddAccountService();
        }

        public static IServiceCollection ConfigureAWSS3Service(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);
            return services
                    .AddDefaultAWSOptions(awsOptions)
                    .AddAWSService<IAmazonS3>()
                    .AddS3FileDataAccessLayer();
        }

        public static IServiceCollection ConfigureLocalWindowsFileService(this IServiceCollection services)
        {
            return services.AddWindowsFileDataAccessLayer();
        }

        public static IServiceCollection ConfigureLocalAuthService(this IServiceCollection services)
        {
            return services.AddLocalAuthAccessLayer();
        }

        public static IServiceCollection ConfigureAWSCognitoService(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);

            // The following 3 variables are null
            var userPoolId = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.UserPoolIdKey);
            var userPoolClientId = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.UserPoolClientIdKey);
            var userPoolAuthority = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.UserPoolAuthorityKey);
            var userPoolClientSecret = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_CLIENT_SECRET");

            var amazonCognitoIdentityProvider = new AmazonCognitoIdentityProviderClient(awsOptions.Credentials,
                                                                                        awsOptions.Region);
            var cognitoUserPool = new CognitoUserPool(userPoolId, userPoolClientId, amazonCognitoIdentityProvider,
                                                    userPoolClientSecret);

            services.AddSingleton<IAmazonCognitoIdentityProvider>(x => amazonCognitoIdentityProvider);
            services.AddSingleton<CognitoUserPool>(x => cognitoUserPool);

            services.AddAuthentication("Bearer")
                .AddJwtBearer(options =>
                {
                    options.Authority = userPoolAuthority;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidateAudience = false //Amazon doesnt provide an audience
                    };
                });

            // add a singleton of our cognito authorization handler
            services.AddSingleton<IAuthorizationHandler, CognitoGroupAuthorizationHandler>();

            services.AddAuthorization(
                options => options.AddPolicy("IsAdmin", policy => policy.Requirements.Add(new CognitoGroupAuthorizationRequirement("Admin")))
            );

            return services.AddAWSAuthAccessLayer();
        }
    }
}