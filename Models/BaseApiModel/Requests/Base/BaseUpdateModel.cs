using System.Collections.Generic;

namespace BaseApi.Models
{
    public abstract class BaseUpdateModel<TBaseModel> where TBaseModel : BaseModel, new()
    {
        public abstract ICollection<object> GetKeys();
        public abstract void Update(TBaseModel model);
    }
}