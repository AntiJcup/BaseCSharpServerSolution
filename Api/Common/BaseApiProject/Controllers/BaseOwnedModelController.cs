using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using BaseApi.Models.Common;
using BaseApi.Services.Common;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

namespace BaseApi.Controllers.Common
{
    public abstract class BaseOwnedModelController<TModel, TCreateModel, TUpdateModel, TViewModel> :
        BaseModelController<TModel, TCreateModel, TUpdateModel, TViewModel>
            where TModel : BaseOwnedModel, new()
            where TCreateModel : BaseOwnedCreateModel<TModel>
            where TUpdateModel : BaseOwnedUpdateModel<TModel>
            where TViewModel : BaseOwnedViewModel<TModel>, new()
    {
        protected readonly AccountAccessService accountAccessService_;

        protected override ICollection<Expression<Func<TModel, object>>> GetIncludes
        {
            get
            {
                return new List<Expression<Func<TModel, object>>>{
                    p => p.OwnerAccount
                };
            }
        }

        protected virtual string UserName
        {
            get
            {
                if (!Request.Headers.ContainsKey("username"))
                {
                    return null;
                }

                var userNames = Request.Headers["username"];
                if (!userNames.Any())
                {
                    return null;
                }

                var userName = userNames.FirstOrDefault();
                return userName;
            }
        }

        protected readonly IConfiguration configuration_;

        public BaseOwnedModelController(
            IConfiguration configuration,
            DBDataAccessService dbDataAccessService,
            AccountAccessService accountAccessService)
         : base(configuration, dbDataAccessService)
        {
            accountAccessService_ = accountAccessService;
        }

        protected override async Task EnrichModel(TModel model, Action action)
        {
            switch (action)
            {
                case Action.Create:
                    model.Owner = UserName;
                    model.OwnerAccountId = (await accountAccessService_.GetAccount(UserName)).Id;
                    break;
                case Action.Update:
                    break;
            }
        }

        protected override async Task EnrichViewModel(TViewModel viewModel, TModel entity)
        {
            viewModel.Owner = entity.OwnerAccount == null ? null : entity.OwnerAccount.NickName;
            await Task.CompletedTask;
        }
    }
}