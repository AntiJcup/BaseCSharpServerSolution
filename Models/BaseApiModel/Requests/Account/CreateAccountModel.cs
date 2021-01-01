using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BaseApi.Models.Requests
{
    public class CreateAccountModel : BaseOwnedCreateModel<Account>
    {
        [Required]
        [MaxLength(256)]
        [MinLength(4)]
        public string NickName { get; set; }

        public bool AcceptOffers { get; set; }

        public override Account Create()
        {
            return new Account()
            {
                NickName = NickName,
                AcceptOffers = AcceptOffers
            };
        }
    }
}