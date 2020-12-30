using System.Threading.Tasks;
using BaseApi.Models;
using BaseApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseApi.Auth
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLocalAuthAccessLayer(this IServiceCollection services)
        {
            services.AddTransient<AuthLayerInterface, LocalAuthAccessLayerInterface>();
            return services.AddTransient<AuthAccessService>();
        }
    }
    public class LocalAuthAccessLayerInterface : AuthLayerInterface
    {
        private readonly IConfiguration configuration_;
        private readonly string localUserName_;
        private readonly string localUserEmail_;

        public LocalAuthAccessLayerInterface(IConfiguration config)
        {
            configuration_ = config;

            localUserName_ = configuration_.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.LocalAuthUserNameKey);

            localUserEmail_ = configuration_.GetSection(Constants.Configuration.Sections.SettingsKey)
                    .GetValue<string>(Constants.Configuration.Sections.Settings.LocalAuthUserEmailKey);

            if (string.IsNullOrWhiteSpace(localUserName_) || string.IsNullOrWhiteSpace(localUserEmail_))
            {
                throw new System.Exception("LocalUserName or LocalUserEmail is not configured");
            }
        }

        public async Task<User> GetUser(string accessToken)
        {
            return await Task.FromResult(new User()
            {
                Name = localUserName_,
                Email = localUserEmail_
            });
        }
    }
}
