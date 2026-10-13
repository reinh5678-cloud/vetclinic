using Microsoft.EntityFrameworkCore.Storage;
using VetClinic.Application.Interfaces;

namespace VetClinic.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly VetClinicDbContext _db;

    public UnitOfWork(VetClinicDbContext db) => _db = db;

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var current = _db.Database.CurrentTransaction;
        if (current is not null)
            return new DbTransactionScope(current, ownsTransaction: false);
        var transaction = await _db.Database.BeginTransactionAsync(ct);
        return new DbTransactionScope(transaction, ownsTransaction: true);
    }
}

public sealed class DbTransactionScope : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;
    private readonly bool _ownsTransaction;

    public DbTransactionScope(IDbContextTransaction transaction, bool ownsTransaction)
    {
        _transaction = transaction;
        _ownsTransaction = ownsTransaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_ownsTransaction) await _transaction.CommitAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_ownsTransaction) await _transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsTransaction) await _transaction.DisposeAsync();
    }
}