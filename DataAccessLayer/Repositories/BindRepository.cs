using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for key/value binds.
/// Binds behave more like configuration entries than regular domain rows, so this repository
/// exposes a natural-key upsert flow in addition to the generic CRUD baseline.
/// </summary>
public class BindRepository : GenericRepository<BindModel>, IBindRepository
{
    public BindRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<BindModel>> GetAllAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .OrderBy(bind => bind.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        var normalizedKey = NormalizeRequiredText(key);
        return _set.AsNoTracking().FirstOrDefaultAsync(bind => bind.Key == normalizedKey, ct);
    }

    /// <inheritdoc />
    public Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .Where(bind => bind.IsActive)
            .OrderBy(bind => bind.Key)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        NormalizeModel(model);

        if (model.Id > 0)
        {
            await EnsureKeyIsUniqueForUpdateAsync(model, ct).ConfigureAwait(false);
            await UpdateAsync(model, ct).ConfigureAwait(false);
            return model;
        }

        var existing = await _set
            .AsNoTracking()
            .FirstOrDefaultAsync(bind => bind.Key == model.Key, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return await AddAsync(model, ct).ConfigureAwait(false);
        }

        model.Id = existing.Id;
        await UpdateAsync(model, ct).ConfigureAwait(false);
        return model;
    }

    private async Task EnsureKeyIsUniqueForUpdateAsync(BindModel model, CancellationToken ct)
    {
        var keyTaken = await _set
            .AsNoTracking()
            .AnyAsync(bind => bind.Key == model.Key && bind.Id != model.Id, ct)
            .ConfigureAwait(false);

        if (keyTaken)
        {
            throw new InvalidOperationException($"Key '{model.Key}' already exists.");
        }
    }

    private static void NormalizeModel(BindModel model)
    {
        model.Key = NormalizeRequiredText(model.Key, "Bind Key is empty.");
        model.Value = NormalizeRequiredText(model.Value, "Bind Value is empty.");
    }

    private static string NormalizeRequiredText(string? value, string errorMessage = "Value is empty.")
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(errorMessage);
        }

        return normalized;
    }
}
