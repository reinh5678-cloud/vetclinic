using VetClinic.Domain.Common;

namespace VetClinic.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    IQueryable<T> Query();
}