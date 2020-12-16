namespace BaseApi.Models
{
    public abstract class BaseOwnedCreateModel<TBaseModel> : BaseCreateModel<TBaseModel> where TBaseModel : BaseOwnedModel, new()
    {
    }
}