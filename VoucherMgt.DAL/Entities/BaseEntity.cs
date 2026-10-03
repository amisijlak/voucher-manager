using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoucherMgt.DAL.Contracts;

namespace VoucherMgt.DAL.Entities;

public abstract class BaseEntity : IBaseEntity<int>, ICreatableEntity
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("CreatedOn")]
    public DateTimeOffset? CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [Column("LastUpdatedOn")]
    public DateTimeOffset? LastUpdatedOn { get; set; }
}
