namespace BaseApi.Models.Common
{
    public abstract class BaseViewModel<TBaseModel> where TBaseModel : BaseModel
    {
        public double DateCreated { get; set; }

        public abstract void Convert(TBaseModel baseModel);
    }
}