using System;

namespace BaseApi.Models.Views
{
    public class AccountViewModel : BaseOwnedViewModel<Account>
    {
        public string Id { get; set; }

        public string NickName { get; set; }

        public string Email { get; set; }

        public DateTime AccountCreated { get; set; }

        public override void Convert(Account baseModel)
        {
            base.Convert(baseModel);

            Id = baseModel.Id.ToString();
            NickName = baseModel.NickName;
            Email = baseModel.Email;
            AccountCreated = baseModel.DateCreated;
        }
    }
}