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
using BaseApi.Constants;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

namespace BaseApi.Controllers.Common
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

        private string userName_;
        protected override string UserName
        {
            get
            {
                if (!useAWS)
                {
                    return "Local";
                }

                if (string.IsNullOrWhiteSpace(userName_))
                {
                    userName_ = this.User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                }
                return userName_;
            }
        }

        protected bool IsExternalLogin
        {
            get
            {
                if (!useAWS || string.IsNullOrWhiteSpace(googleUserGroup_))
                {
                    return false;
                }

                //TODO Other external logins need to be checked here when added
                return this.User.HasClaim(c => c.Type == "cognito:groups" && (c.Value == googleUserGroup_));
            }
        }

        protected string AccessToken
        {
            get
            {
                if (!Request.Headers.ContainsKey("Authorization"))
                {
                    return null;
                }

                var authorizationHeaderValues = Request.Headers["Authorization"];
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
        }

        protected readonly bool useAWS;
        protected readonly bool localAdmin;
        private readonly string googleUserGroup_;

        public BaseAuthOwnedModelController(
            IConfiguration configuration,
            DBDataAccessService dbDataAccessService,
            AccountAccessService accountAccessService)
         : base(configuration, dbDataAccessService, accountAccessService)
        {
            useAWS = configuration_.GetSection(BaseApi.Constants.Common.Configuration.Sections.SettingsKey)
                        .GetValue<bool>(BaseApi.Constants.Common.Configuration.Sections.Settings.UseAWSKey, false);

            localAdmin = configuration_.GetSection(BaseApi.Constants.Common.Configuration.Sections.SettingsKey)
                        .GetValue<bool>(BaseApi.Constants.Common.Configuration.Sections.Settings.LocalAdminKey, false);

            googleUserGroup_ = configuration_.GetSection(BaseApi.Constants.Common.Configuration.Sections.SettingsKey)
                        .GetValue<string>(BaseApi.Constants.Common.Configuration.Sections.Settings.GoogleExternalGroupNameKey);
        }

        [Authorize]
        [HttpGet]
        public virtual async Task<IActionResult> GetAllByOwner([FromQuery] int? skip = null, [FromQuery] int? take = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entities = await dbDataAccessService_.GetAllOwnedModel<TModel>(
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
        public override async Task<IActionResult> UpdateStatusById([FromQuery] Guid id, [FromQuery] BaseState status)
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
            return useAWS ? this.User.HasClaim(c => c.Type == "cognito:groups" &&
                                            c.Value == "Admin") : localAdmin;
        }

        protected override async Task<bool> HasAccessToModel(TModel model)
        {
            return await Task.FromResult(model.Owner == UserName || IsAdmin());
        }

    }
}