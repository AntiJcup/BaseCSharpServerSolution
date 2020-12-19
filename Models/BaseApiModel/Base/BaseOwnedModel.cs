using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseApi.Models
{
    public class BaseOwnedModel : BaseModel
    {
        [MinLength(1)]
        [MaxLength(1028)]
        public string Owner { get; set; }

        [ForeignKey("OwnerAccount")]
        public Guid? OwnerAccountId { get; set; }

        public virtual Account OwnerAccount { get; set; }
    }
}