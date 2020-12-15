namespace BaseApi.Models.Common
{
    public abstract class BaseOwnedCreateModel<TBaseModel> : BaseCreateModel<TBaseModel> where TBaseModel : BaseOwnedModel, new()
    {
    }
}