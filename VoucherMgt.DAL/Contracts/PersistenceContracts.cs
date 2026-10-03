namespace VoucherMgt.DAL.Contracts;

public interface IBaseEntity<T>
{
    T Id { get; set; }
}

public interface ICreatableEntity
{
    DateTimeOffset? CreatedOn { get; set; }
    DateTimeOffset? LastUpdatedOn { get; set; }
}

public interface IOrganizationScopedEntity
{
    int OrganizationId { get; set; }
}

public interface IRepository
{
    IQueryable<T> Set<T>() where T : class;
    Task<T?> FindAsync<T>(params object[] keyValues) where T : class;
    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
