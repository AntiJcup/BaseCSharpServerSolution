using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BaseApi.Models;
using BaseApi.Services;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

namespace BaseApi.Controllers
{
    public abstract class BaseAuthOwnedModelController<TModel, TCreateModel, TUpdateModel, TViewModel> :
        BaseOwnedModelController<TModel, TCreateModel, TUpdateModel, TViewModel>
            where TModel : BaseOwnedModel, new()
            where TCreateModel : BaseOwnedCreateModel<TModel>
            where TUpdateModel : BaseOwnedUpdateModel<TModel>
            where TViewModel : BaseOwnedViewModel<TModel>, new()
    {
        protected override ICollection<Expression<Func<TModel, object>>> GetIncludes
        {
            get
            {
                return new List<Expression<Func<TModel, object>>>{
                    p => p.OwnerAccount
                };
            }
        }

        protected override string UserName
        {
            get
            {
                return authAccessService_.GetUserName(User); ;
            }
        }

        protected bool IsExternalLogin
        {
            get
            {
                return authAccessService_.IsExternalLogin(User);
            }
        }

        protected string AccessToken
        {
            get
            {
                return authAccessService_.GetAccessToken(Request.Headers);
            }
        }

        private readonly AuthAccessService authAccessService_;

        public BaseAuthOwnedModelController(DBDataAccessService dbDataAccessService,
                                            AccountAccessService accountAccessService,
                                            AuthAccessService authAccessService)
         : base(dbDataAccessService, accountAccessService)
        {
            authAccessService_ = authAccessService;
        }

        [Authorize]
        [HttpGet]
        public virtual async Task<IActionResult> GetAllByOwner([FromQuery] int? skip = null,
                                                               [FromQuery] int? take = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entities = await dbDataAccessService_.GetAllOwnedModel(
                UserName,
                skip,
                take,
                GetIncludes);
            var viewModels = new List<TViewModel>();

            foreach (var entity in entities)
            {
                var viewModel = new TViewModel();
                viewModel.Convert(entity);
                await EnrichViewModel(viewModel, entity);
                viewModels.Add(viewModel);
            }
            return new JsonResult(viewModels);
        }

        [Authorize]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TCreateModel createModel)
        {
            return await base.Create(createModel);
        }

        [Authorize]
        [HttpPost]
        public override async Task<IActionResult> Update([FromBody] TUpdateModel updateModel)
        {
            return await base.Update(updateModel);
        }

        [Authorize(Policy = "IsAdmin")]
        [HttpPost]
        public override async Task<IActionResult> UpdateStatusById([FromQuery] Guid id,
                                                                   [FromQuery] BaseState status)
        {
            return await base.UpdateStatusById(id, status);
        }

        [Authorize]
        [HttpPost]
        public override async Task<IActionResult> Delete()
        {
            return await base.Delete();
        }

        [Authorize]
        [HttpPost]
        public override async Task<IActionResult> DeleteById([FromQuery] Guid id)
        {
            return await base.DeleteById(id);
        }

        protected virtual bool IsAdmin()
        {
            return authAccessService_.IsAdmin(User);
        }

        protected override async Task<bool> HasAccessToModel(TModel model)
        {
            return await Task.FromResult(model.Owner == UserName || IsAdmin());
        }

    }
}