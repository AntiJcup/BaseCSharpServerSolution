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
            base.Convert(baseModel);

            Id = baseModel.Id.ToString();
            UserId = baseModel.Owner;
            NickName = baseModel.NickName;
            Email = baseModel.Email;
            AccountCreated = baseModel.DateCreated;
        }
    }
}