using System;

namespace BaseApi.Models.Views
{
    public class AccountViewModel : BaseViewModel<Account>
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string NickName { get; set; }

        public string Email { get; set; }

        public DateTime AccountCreated { get; set; }

        public override void Convert(Account baseModel)
        {
            Id = baseModel.Id.ToString();
            UserId = baseModel.Owner;
            NickName = baseModel.NickName;
            Email = baseModel.Email;
            AccountCreated = baseModel.DateCreated;
            DateCreated = baseModel.DateCreated.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ).TotalMilliseconds;
        }
    }
}