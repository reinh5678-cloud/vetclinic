using Microsoft.EntityFrameworkCore;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Common;
using VetClinic.Infrastructure.Data;

namespace VetClinic.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly VetClinicDbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(VetClinicDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Set.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
        return entity;
    }

    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await Db.SaveChangesAsync(ct);
}