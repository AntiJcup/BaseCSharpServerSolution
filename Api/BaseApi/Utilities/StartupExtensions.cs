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

namespace BaseApi
{
    public static class Startup
    {
        public static IServiceCollection ConfigureAWSS3Service(this IServiceCollection services, IConfiguration Configuration)
        {
            var awsOptions = Configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);
            return services
                    .AddDefaultAWSOptions(awsOptions)
                    .AddAWSService<IAmazonS3>()
                    .AddS3FileDataAccessLayer();
        }

        public static IServiceCollection ConfigureAWSCognitoService(this IServiceCollection services, IConfiguration Configuration)
        {
            var awsOptions = Configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);

            // The following 3 variables are null
            var userPoolId = Configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.UserPoolIdKey);
            var userPoolClientId = Configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.UserPoolClientIdKey);
            var userPoolAuthority = Configuration.GetSection(Constants.Configuration.Sections.SettingsKey)
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

            return services.AddAWSAuthAccessLayer();
        }
    }
}