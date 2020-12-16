using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BaseApi.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BaseState
    {
        Undefined,
        Active,
        Inactive,
        Deleted,
    }

    public abstract class BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DateModified { get; set; }

        [MaxLength(1028)]
        public string Notes { get; set; }

        public BaseState Status { get; set; } = BaseState.Inactive;
    }
}