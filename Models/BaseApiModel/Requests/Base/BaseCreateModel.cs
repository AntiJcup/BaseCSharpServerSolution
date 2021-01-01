namespace BaseApi.Models
{
    public abstract class BaseCreateModel<TBaseModel> 
            where TBaseModel : BaseModel, new()
    {
        public abstract TBaseModel Create();
    }
}