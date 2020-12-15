using System.Collections.Generic;

namespace BaseApi.Models.Common
{
    public abstract class BaseOwnedUpdateModel<TBaseModel> : BaseUpdateModel<TBaseModel> where TBaseModel : BaseOwnedModel, new()
    {
    }
}