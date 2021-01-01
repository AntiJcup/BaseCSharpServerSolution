using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseApi.Models;
using BaseApi.Services;
using System.Linq.Expressions;

namespace BaseApi.Controllers
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

        public BaseOwnedModelController(DBDataAccessService dbDataAccessService,
                                        AccountAccessService accountAccessService)
         : base(dbDataAccessService)
        {
            accountAccessService_ = accountAccessService;
        }

        protected override async Task EnrichModel(TModel model,
                                                  Action action)
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

        protected override async Task EnrichViewModel(TViewModel viewModel,
                                                      TModel entity)
        {
            viewModel.Owner = entity.OwnerAccount?.NickName;
            await Task.CompletedTask;
        }
    }
}