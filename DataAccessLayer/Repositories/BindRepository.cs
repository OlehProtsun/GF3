using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories
{
    public class BindRepository : GenericRepository<BindModel>, IBindRepository
    {
        public BindRepository(AppDbContext db) : base(db) { }

        public override async Task<List<BindModel>> GetAllAsync(CancellationToken ct = default)
            => await _set.AsNoTracking()
                         .OrderBy(x => x.Key)
                         .ToListAsync(ct)
                         .ConfigureAwait(false);

        public Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            key = (key ?? string.Empty).Trim();
            return _set.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);
        }

        public Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
            => _set.AsNoTracking()
                   .Where(x => x.IsActive)
                   .OrderBy(x => x.Key)
                   .ToListAsync(ct);

        public async Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
        {
            model.Key = (model.Key ?? string.Empty).Trim();
            model.Value = (model.Value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(model.Key))
                throw new ArgumentException("Bind Key is empty.");
            if (string.IsNullOrWhiteSpace(model.Value))
                throw new ArgumentException("Bind Value is empty.");

            // якщо редагуємо існуючий (по Id) — просто Update,
            // але перевіримо, що новий Key не зайнятий іншим записом
            if (model.Id > 0)
            {
                bool keyTaken = await _set.AsNoTracking()
                    .AnyAsync(x => x.Key == model.Key && x.Id != model.Id, ct)
                    .ConfigureAwait(false);

                if (keyTaken)
                    throw new InvalidOperationException($"Key '{model.Key}' already exists.");

                await UpdateAsync(model, ct).ConfigureAwait(false);
                return model;
            }

            // створюємо/оновлюємо по Key
            var existing = await _set.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == model.Key, ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                return await AddAsync(model, ct).ConfigureAwait(false);
            }

            model.Id = existing.Id;
            await UpdateAsync(model, ct).ConfigureAwait(false);
            return model;
        }
    }
}
