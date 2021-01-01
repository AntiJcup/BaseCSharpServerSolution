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
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
using System.Linq;

namespace BaseApi
{
    public static class StartupServiceExtensions
    {
        #region Individual Controls
        public static IServiceCollection ConfigureMVCService(this IServiceCollection services,
                                                             bool development)
        {
            if (development)
            {
                services.AddMvc(options =>
                {
                    options.Filters.Add(new AllowAnonymousFilter());
                }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            }
            else
            {
                services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            }

            return services;
        }

        public static IServiceCollection ConfigureSwaggerService(this IServiceCollection services,
                                                                 IConfiguration configuration)
        {
            var swaggerTitle = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.ProjectNameKey);
            var swaggerVersion = configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.ProjectVersionKey);

            return services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = swaggerTitle, Version = $"v{swaggerVersion}" });
                c.AddSecurityDefinition("Bearer",
                                        new ApiKeyScheme
                                        {
                                            In = "header",
                                            Description = "Please enter into field the word 'Bearer' following by space and JWT",
                                            Name = "Authorization",
                                            Type = "apiKey"
                                        });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                                                        { "Bearer", Enumerable.Empty<string>() },
                                        });
            });
        }

        public static IServiceCollection ConfigureCorsService(this IServiceCollection services,
                                                              IConfiguration configuration)
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

        public static IServiceCollection ConfigureMicrosoftSQLService<TDBContext>(this IServiceCollection services,
                                                                                  IConfiguration configuration)
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

        public static IServiceCollection ConfigureAWSS3Service(this IServiceCollection services,
                                                               IConfiguration configuration)
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
            services.AddLocalAuthAccessLayer();

            return services.AddAuthorization(
                options => options.AddPolicy("IsAdmin", policy => policy.Requirements.Add(new CognitoGroupAuthorizationRequirement("Admin")))
            );
        }

        public static IServiceCollection ConfigureAWSCognitoService(this IServiceCollection services,
                                                                    IConfiguration configuration)
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

            services.AddAWSAuthAccessLayer();

            return services.AddAuthorization(
                options => options.AddPolicy("IsAdmin", policy => policy.Requirements.Add(new CognitoGroupAuthorizationRequirement("Admin")))
            );
        }
        #endregion Individual Controls

        #region Premade Environments
        public static IServiceCollection ConfigureLocalDefaulDevelopmentEnv<TSQLDBContext>(this IServiceCollection services,
                                                                                           IConfiguration configuration)
                where TSQLDBContext : TemplateMicrosoftSQLDbContext, new()
        {
            return services
                .ConfigureMVCService(true)
                .ConfigureSwaggerService(configuration)
                .ConfigureCorsService(configuration)
                .ConfigureMicrosoftSQLService<TSQLDBContext>(configuration)
                .ConfigureAccountService()
                .ConfigureLocalWindowsFileService()
                .ConfigureLocalAuthService();
        }

        public static IServiceCollection ConfigureAWSDefaulDevelopmentEnv<TSQLDBContext>(this IServiceCollection services,
                                                                                         IConfiguration configuration)
                where TSQLDBContext : TemplateMicrosoftSQLDbContext, new()
        {
            return services
                .ConfigureMVCService(true)
                .ConfigureSwaggerService(configuration)
                .ConfigureCorsService(configuration)
                .ConfigureMicrosoftSQLService<TSQLDBContext>(configuration)
                .ConfigureAccountService()
                .ConfigureAWSS3Service(configuration)
                .ConfigureAWSCognitoService(configuration);
        }

        public static IServiceCollection ConfigureAWSDefaulProdEnv<TSQLDBContext>(this IServiceCollection services,
                                                                                  IConfiguration configuration)
                where TSQLDBContext : TemplateMicrosoftSQLDbContext, new()
        {
            return services
                .ConfigureMVCService(false)
                .ConfigureSwaggerService(configuration)
                .ConfigureCorsService(configuration)
                .ConfigureMicrosoftSQLService<TSQLDBContext>(configuration)
                .ConfigureAccountService()
                .ConfigureAWSS3Service(configuration)
                .ConfigureAWSCognitoService(configuration);
        }
        #endregion Premade Environments
    }
}