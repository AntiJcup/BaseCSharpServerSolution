using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BaseApi.Models;

namespace BaseApi.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAccountService(this IServiceCollection services)
        {
            return services.AddTransient<AccountAccessService>();
        }
    }

    public class AccountAccessService
    {
        private readonly DBDataAccessService dbService_;

        public AccountAccessService(DBDataAccessService dbService)
        {
            dbService_ = dbService;
        }

        public async Task<Account> GetAccount(string userId)
        {
            return (await dbService_.GetAllOwnedModel<Account>(userId, 0, 1)).FirstOrDefault();
        }

        public async Task<Account> GetAccount(Guid accountId)
        {
            return await dbService_.GetBaseModel<Account>(null, accountId);
        }

        public async Task<Account> CreateAccount(User user,
                                                 string nickName = null)
        {
            var account = new Account()
            {
                Owner = user.Name,
                Email = user.Email,
                NickName = ValidateNickName(nickName) && !await DoesNickNameExist(nickName) ? nickName : $"Default_{Guid.NewGuid().GetHashCode()}",
                AcceptOffers = false,
                Status = BaseState.Active
            };

            return await dbService_.CreateBaseModel(account);
        }

        public bool ValidateNickName(string nickName)
        {
            var regex = new Regex("^[a-z0-9._-]+$", RegexOptions.IgnoreCase);
            return !string.IsNullOrWhiteSpace(nickName) && regex.IsMatch(nickName);
        }

        public async Task<bool> DoesNickNameExist(string nickName)
        {
            return (await dbService_.GetAllBaseModel((Expression<Func<Account, bool>>)(m => m.NickName == nickName)))
                .Any();
        }
    }
}
