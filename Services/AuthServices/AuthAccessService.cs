using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BaseApi.Models;

namespace BaseApi.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAuthService(this IServiceCollection services)
        {
            return services.AddTransient<AuthAccessService>();
        }
    }

    public class AuthAccessService
    {
        private readonly IAuthLayer authLayer_;
        private readonly IConfiguration configuration_;

        private readonly bool useAWS_;
        private readonly bool localAdmin_;
        private readonly string googleUserGroup_;

        public AuthAccessService(IConfiguration configuration, IAuthLayer authLayer)
        {
            authLayer_ = authLayer;
            configuration_ = configuration;

            useAWS_ = configuration_.GetSection(Constants.Configuration.Sections.SettingsKey)
                        .GetValue(Constants.Configuration.Sections.Settings.UseAWSKey, false);

            localAdmin_ = configuration_.GetSection(Constants.Configuration.Sections.SettingsKey)
                        .GetValue(Constants.Configuration.Sections.Settings.LocalAdminKey, false);

            googleUserGroup_ = configuration_.GetSection(Constants.Configuration.Sections.SettingsKey)
                        .GetValue<string>(Constants.Configuration.Sections.Settings.GoogleExternalGroupNameKey);
        }

        public async Task<User> GetUser(string accountToken)
        {
            return await authLayer_.GetUser(accountToken);
        }

        public bool IsExternalLogin(System.Security.Claims.ClaimsPrincipal user)
        {
            if (!useAWS_ || string.IsNullOrWhiteSpace(googleUserGroup_))
            {
                return false;
            }

            //TODO Other external logins need to be checked here when added
            return user.HasClaim(c => c.Type == "cognito:groups" && (c.Value == googleUserGroup_));
        }

        public string GetAccessToken(Microsoft.AspNetCore.Http.IHeaderDictionary headers)
        {
            if (!headers.ContainsKey("Authorization"))
            {
                return null;
            }

            var authorizationHeaderValues = headers["Authorization"];
            if (!authorizationHeaderValues.Any())
            {
                return null;
            }

            var authorizationHeader = authorizationHeaderValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return null;
            }

            var accessToken = authorizationHeader.Split(' ').Skip(1).FirstOrDefault();
            return accessToken;
        }

        public string GetUserName(System.Security.Claims.ClaimsPrincipal user)
        {
            if (!useAWS_)
            {
                return "Local";
            }

            return user.Claims.FirstOrDefault(c => c.Type == "username").Value;
        }

        public bool IsAdmin(System.Security.Claims.ClaimsPrincipal user)
        {
            return useAWS_ ? user.HasClaim(c => c.Type == "cognito:groups" &&
                                            c.Value == "Admin") : localAdmin_;
        }
    }
}
