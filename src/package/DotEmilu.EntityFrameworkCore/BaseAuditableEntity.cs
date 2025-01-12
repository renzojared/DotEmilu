namespace DotEmilu.EntityFrameworkCore;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
    public DateTimeOffset Deleted { get; set; }
    public Guid? DeletedBy { get; set; }
}