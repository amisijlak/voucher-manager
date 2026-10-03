using VoucherMgt.DAL.Contracts;
using Microsoft.EntityFrameworkCore;

namespace VoucherMgt.DAL.Repositories;

public class VoucherMgtDbRepository : IRepository
{
    private readonly VoucherMgtDbContext _dbContext;

    public VoucherMgtDbRepository(VoucherMgtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<T> Set<T>() where T : class => _dbContext.Set<T>();

    public async Task<T?> FindAsync<T>(params object[] keyValues) where T : class =>
        await _dbContext.Set<T>().FindAsync(keyValues);

    public void Add<T>(T entity) where T : class => _dbContext.Set<T>().Add(entity);

    public void Update<T>(T entity) where T : class => _dbContext.Set<T>().Update(entity);

    public void Remove<T>(T entity) where T : class => _dbContext.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
