using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BaseApi.Models;

namespace BaseApi.Services
{
    public class DBDataAccessService
    {
        private readonly IDBDataLayer dataLayer_;

        public DBDataAccessService(IDBDataLayer dataLayer)
        {
            dataLayer_ = dataLayer;
        }

        public async Task<ICollection<TModel>> GetAllBaseModel<TModel>(Expression<Func<TModel, bool>> where = null,
                                                                       int? skip = null,
                                                                       int? take = null,
                                                                       ICollection<Expression<Func<TModel, object>>> includes = null)
                where TModel : BaseModel, new()
        {
            return await dataLayer_.GetAll(includes, where, skip, take);
        }

        public async Task<int> CountAllBaseModel<TModel>(Expression<Func<TModel, bool>> where = null)
                where TModel : BaseModel, new()
        {
            return await dataLayer_.CountAll(where);
        }

        public async Task<ICollection<TModel>> GetAllOwnedModel<TModel>(string userName,
                                                                        int? skip = null,
                                                                        int? take = null,
                                                                        ICollection<Expression<Func<TModel, object>>> includes = null)
                where TModel : BaseOwnedModel, new()
        {
            return await dataLayer_.GetAll(
                includes, m => m.Owner == userName && m.Status != BaseState.Deleted, skip, take);
        }

        public async Task<TModel> GetBaseModel<TModel>(ICollection<Expression<Func<TModel, object>>> includes = null,
                                                       params object[] keys)
                where TModel : BaseModel, new()
        {
            return await dataLayer_.Get(includes, keys);
        }

        public async Task<TModel> GetBaseModel<TModel>(ICollection<object> keys,
                                                       ICollection<Expression<Func<TModel, object>>> includes = null)
                where TModel : BaseModel, new()
        {
            return await dataLayer_.Get(includes, keys);
        }

        public async Task<TModel> CreateBaseModel<TModel>(TModel model)
                where TModel : BaseModel, new()
        {
            return await dataLayer_.Create(model);
        }

        public async Task UpdateBaseModel<TModel>(TModel model)
                where TModel : BaseModel, new()
        {
            await dataLayer_.Update(model);
        }

        public async Task DeleteBaseModel<TModel>(TModel model,
                                                  bool removeFromDB = false)
                where TModel : BaseModel, new()
        {
            if (removeFromDB)
            {
                await dataLayer_.Delete(model);
            }
            else
            {
                model.Status = BaseState.Deleted;
                await dataLayer_.Update(model);
            }
        }

        public async Task DeleteBaseModelByIds<TModel>(bool removeFromDB = false,
                                                       params object[] keys)
                where TModel : BaseModel, new()
        {
            var model = await dataLayer_.Get<TModel>(keys);
            if (model == null)
            {
                throw new Exception("Model doesnt exist");
            }
            await DeleteBaseModel(model, removeFromDB);
        }
    }
}