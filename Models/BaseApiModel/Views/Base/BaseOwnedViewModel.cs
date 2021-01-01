namespace BaseApi.Models
{
    public abstract class BaseOwnedViewModel<TBaseModel> : BaseViewModel<TBaseModel>
            where TBaseModel : BaseOwnedModel
    {
        public string Owner { get; set; }

        public string OwnerId { get; set; }

        public override void Convert(TBaseModel baseModel)
        {
            base.Convert(baseModel);
            Owner = baseModel.Owner;
            OwnerId = baseModel.OwnerAccountId?.ToString();
        }
    }
}