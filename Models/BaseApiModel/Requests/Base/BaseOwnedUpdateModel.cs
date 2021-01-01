namespace BaseApi.Models
{
    public abstract class BaseOwnedUpdateModel<TBaseModel> : BaseUpdateModel<TBaseModel> 
            where TBaseModel : BaseOwnedModel, new()
    {
    }
}