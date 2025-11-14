using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class GenericRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<TEntity> _set;

        public GenericRepository(AppDbContext db)
        {
            _db = db;
            _set = _db.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _set.FindAsync(new object?[] { id }, ct);

        public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
            => await _set.AsNoTracking().ToListAsync(ct);

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
        {
            await _set.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);

            // Щоб контекст не тримав створену сутність у трекері
            _db.Entry(entity).State = EntityState.Detached;
            return entity;
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
        {
            // 1) Визначаємо первинний ключ і його значення в entity
            var et = _db.Model.FindEntityType(typeof(TEntity))!;
            var pk = et.FindPrimaryKey()!;
            var keyValues = pk.Properties.Select(p => p.PropertyInfo!.GetValue(entity)).ToArray();

            // 2) Пробуємо знайти вже відстежуваний екземпляр (FindAsync спершу дивиться в Local)
            var tracked = await _set.FindAsync(keyValues, ct);

            if (tracked is not null)
            {
                // 3a) Оновлюємо значення в уже відстежуваній сутності
                _db.Entry(tracked).CurrentValues.SetValues(entity);
            }
            else
            {
                // 3b) Якщо в трекері нема – просто attach + Modified
                _set.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync(ct);

            // (опційно) Відчепити, щоб контекст не набирав «боргів» трекінгу на WinForms-життєвому циклі
            //_db.Entry(tracked ?? entity).State = EntityState.Detached;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, ct);
            if (entity is null) return;
            _set.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
