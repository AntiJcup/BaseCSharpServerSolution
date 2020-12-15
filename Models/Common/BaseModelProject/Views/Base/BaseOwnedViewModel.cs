namespace BaseApi.Models.Common
{
    public abstract class BaseOwnedViewModel<TBaseModel> : BaseViewModel<TBaseModel> where TBaseModel : BaseOwnedModel
    {
        public string Owner { get; set; }

        public string OwnerId { get; set; }
    }
}