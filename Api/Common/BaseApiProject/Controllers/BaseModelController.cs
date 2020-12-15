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
    public enum Action
    {
        Create,
        Update,
    }

    public abstract class BaseModelController<TModel, TCreateModel, TUpdateModel, TViewModel> : ControllerBase
        where TModel : BaseModel, new()
        where TCreateModel : BaseCreateModel<TModel>
        where TUpdateModel : BaseUpdateModel<TModel>
        where TViewModel : BaseViewModel<TModel>, new()
    {
        protected readonly DBDataAccessService dbDataAccessService_;

        protected virtual ICollection<Expression<Func<TModel, object>>> GetIncludes
        {
            get
            {
                return new List<Expression<Func<TModel, object>>>{
                };
            }
        }

        protected virtual ICollection<Expression<Func<TModel, object>>> DeleteIncludes
        {
            get
            {
                return new List<Expression<Func<TModel, object>>>
                {
                };
            }
        }

        public BaseModelController(
            IConfiguration configuration,
            DBDataAccessService dbDataAccessService)
         : base()
        {
            dbDataAccessService_ = dbDataAccessService;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Get()
        {
            var keys = await GetKeysFromRequest();
            var entity = await dbDataAccessService_.GetBaseModel<TModel>(keys, GetIncludes);
            if (entity == null)
            {
                return NotFound();
            }
            var viewModel = new TViewModel();
            viewModel.Convert(entity);
            await EnrichViewModel(viewModel, entity);
            return new JsonResult(viewModel);
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetById([FromQuery] Guid id)
        {
            var keys = await GetKeysFromRequest();
            if (keys.Count() > 1)
            {
                return BadRequest("Error: Object has more than one key use Get");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbDataAccessService_.GetBaseModel<TModel>(GetIncludes, id);
            if (entity == null)
            {
                return NotFound();
            }
            var viewModel = new TViewModel();
            viewModel.Convert(entity);
            await EnrichViewModel(viewModel, entity);
            return new JsonResult(viewModel);
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll([FromQuery] BaseState state, [FromQuery] int? skip = null, [FromQuery] int? take = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entities = await dbDataAccessService_.GetAllBaseModel(
                state == BaseState.Undefined ? (Expression<Func<TModel, Boolean>>)null : (Expression<Func<TModel, Boolean>>)(m => m.Status == state),
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

        [HttpGet]
        public virtual async Task<IActionResult> CountAll([FromQuery] BaseState state)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityCount = await dbDataAccessService_.CountAllBaseModel(
                state == BaseState.Undefined ? (Expression<Func<TModel, Boolean>>)null : (Expression<Func<TModel, Boolean>>)(m => m.Status == state));

            return new JsonResult(entityCount);
        }

        [Authorize]
        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] TCreateModel createModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!(await CanCreate(createModel)))
            {
                return BadRequest("Can't create");
            }

            var model = createModel.Create();
            await EnrichModel(model, Action.Create);
            var entity = await dbDataAccessService_.CreateBaseModel(model);
            var filledOutEntity = await dbDataAccessService_.GetBaseModel<TModel>(await GetKeysFromModel(entity), GetIncludes);
            await OnCreated(createModel, filledOutEntity);
            var viewModel = new TViewModel();
            viewModel.Convert(entity);
            await EnrichViewModel(viewModel, filledOutEntity);
            return new JsonResult(viewModel);
        }

        [Authorize]
        [HttpPost]
        public virtual async Task<IActionResult> Update([FromBody] TUpdateModel updateModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var modelKeys = updateModel.GetKeys();
            var model = await dbDataAccessService_.GetBaseModel<TModel>(modelKeys, GetIncludes);

            if (model == null)
            {
                return NotFound(); //Update cant be called on items that dont exist
            }

            if (!(await HasAccessToModel(model)))
            {
                return Forbid();
            }

            updateModel.Update(model);

            await EnrichModel(model, Action.Update);
            await dbDataAccessService_.UpdateBaseModel(model);
            await OnUpdated(updateModel, model);

            var viewModel = new TViewModel();
            viewModel.Convert(model);
            await EnrichViewModel(viewModel, model);

            return new JsonResult(viewModel);
        }

        [Authorize(Policy = "IsAdmin")]
        [HttpPost]
        public virtual async Task<IActionResult> UpdateStatusById([FromQuery] Guid id, [FromQuery] BaseState status)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var model = await dbDataAccessService_.GetBaseModel<TModel>(null, id);
            if (model == null)
            {
                return NotFound(); //Update cant be called on items that dont exist
            }

            if (!(await HasAccessToModel(model)))
            {
                return Forbid();
            }

            model.Status = status;
            await dbDataAccessService_.UpdateBaseModel(model);
            await OnUpdated(null, model);

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public virtual async Task<IActionResult> Delete()
        {
            var keys = await GetKeysFromRequest();
            var oldModel = await dbDataAccessService_.GetBaseModel<TModel>(keys, DeleteIncludes);

            if (oldModel == null)
            {
                return NotFound(); //Delete cant be called on items that dont exist
            }

            if (!(await HasAccessToModel(oldModel)))
            {
                return Forbid();
            }

            if (!(await CanDelete(oldModel)))
            {
                return BadRequest("unable");
            }

            await dbDataAccessService_.DeleteBaseModelByIds<TModel>(false, keys);
            await OnDeleted(oldModel);

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public virtual async Task<IActionResult> DeleteById([FromQuery] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var oldModel = await dbDataAccessService_.GetBaseModel<TModel>(DeleteIncludes, id);

            if (oldModel == null)
            {
                return NotFound(); //Delete cant be called on items that dont exist
            }

            if (!(await HasAccessToModel(oldModel)))
            {
                return Forbid();
            }

            if (!(await CanDelete(oldModel)))
            {
                return BadRequest("unable");
            }

            await dbDataAccessService_.DeleteBaseModel<TModel>(oldModel);
            await OnDeleted(oldModel);

            return Ok();
        }

        protected virtual async Task<ICollection<object>> GetKeysFromRequest()
        {
            var keys = new List<object>();

            var keyProperties = typeof(TModel).GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            foreach (var keyProp in keyProperties)
            {
                StringValues val;
                if (Request.Query.TryGetValue(keyProp.Name, out val))
                {
                    keys.Add(val.FirstOrDefault());
                }
            }

            if (keys.Count() < keyProperties.Count())
            {
                throw new Exception("Not enough keys");
            }

            return await Task.FromResult((ICollection<object>)keys);
        }

        protected virtual async Task<ICollection<object>> GetKeysFromModel(TModel model)
        {
            var keys = new List<object>();

            var keyProperties = typeof(TModel).GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            foreach (var keyProp in keyProperties)
            {
                var val = keyProp.GetValue(model);
                if (val != null)
                {
                    keys.Add(val);
                }
                else
                {
                    throw new Exception("Key missing value");
                }
            }

            if (keys.Count() < keyProperties.Count())
            {
                throw new Exception("Not enough keys");
            }

            return await Task.FromResult((ICollection<object>)keys);
        }

        protected virtual async Task<string> GenerateModelStateErrorMessage()
        {
            var builder = new StringBuilder();
            foreach (var modelValue in ModelState.Values)
            {
                foreach (var modelError in modelValue.Errors)
                {
                    builder.AppendLine($"{modelError.ErrorMessage}");
                }
            }

            return await Task.FromResult(builder.ToString());
        }

        protected virtual async Task EnrichModel(TModel model, Action action)
        {
            switch (action)
            {
                case Action.Create:
                    break;
                case Action.Update:
                    break;
            }
            await Task.CompletedTask;
        }

        protected virtual async Task EnrichViewModel(TViewModel viewModel, TModel entity)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task OnCreated(TCreateModel createModel, TModel entity)
        {
            //Override when you need to something special on model create
            await Task.CompletedTask;
        }

        protected virtual async Task OnDeleted(TModel entity)
        {
            //Override when you need to something special on model delete
            await Task.CompletedTask;
        }

        protected virtual async Task OnUpdated(TUpdateModel updateModel, TModel entity)
        {
            //Override when you need to something special on model delete
            await Task.CompletedTask;
        }

        protected virtual async Task<bool> CanDelete(TModel entity)
        {
            return await Task.FromResult(true);
        }

        protected virtual async Task<bool> CanCreate(TCreateModel createModel)
        {
            return await Task.FromResult(true);
        }

        protected virtual async Task<bool> HasAccessToModel(TModel model)
        {
            return await Task.FromResult(true);
        }
    }
}