using System.Linq;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using BaseApi.Models;
using BaseApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BaseApi.Auth
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAWSAuthAccessLayer(this IServiceCollection services)
        {
            services.AddTransient<AuthLayerInterface, AWSAuthAccessLayerInterface>();
            return services.AddTransient<AuthAccessService>();
        }
    }

    public class AWSAuthAccessLayerInterface : AuthLayerInterface
    {
        private readonly IAmazonCognitoIdentityProvider identity_;

        public AWSAuthAccessLayerInterface(IAmazonCognitoIdentityProvider identity)
        {
            identity_ = identity;
        }

        public async Task<User> GetUser(string accessToken)
        {
            var userDetails = await identity_.GetUserAsync(new Amazon.CognitoIdentityProvider.Model.GetUserRequest()
            {
                AccessToken = accessToken
            });

            return new User()
            {
                Name = userDetails.Username,
                Email = userDetails.UserAttributes.FirstOrDefault(a => a.Name == "email").Value
            };
        }
    }
}
