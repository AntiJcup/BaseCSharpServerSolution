using System;

namespace BaseApi.Models
{
    public abstract class BaseViewModel<TBaseModel>
            where TBaseModel : BaseModel
    {
        public double DateCreated { get; set; }

        public virtual void Convert(TBaseModel baseModel)
        {
            DateCreated = baseModel.DateCreated.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ).TotalMilliseconds;
        }
    }
}