using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BaseApi.Utilities.AWS.Auth
{
    public class CognitoGroupAuthorizationHandler : AuthorizationHandler<CognitoGroupAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CognitoGroupAuthorizationRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "cognito:groups" &&
                                           c.Value == requirement.CognitoGroup))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
