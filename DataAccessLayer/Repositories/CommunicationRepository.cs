using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public sealed class CommunicationRepository : GenericRepository<CommunicationMessageModel>, ICommunicationRepository
{
    public CommunicationRepository(AppDbContext db)
        : base(db)
    {
    }

    public Task<List<CommunicationMessageModel>> GetAllForManagerAsync(CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .OrderByDescending(message => message.Id)
            .ToListAsync(ct);

    public Task<List<CommunicationMessageModel>> GetPendingForEmployeeAsync(
        int employeeId,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
        => _set
            .FromSqlInterpolated(
                $"""
                SELECT
                    m.id,
                    m.title,
                    m.body,
                    m.visible_from_utc,
                    m.deadline_at_utc,
                    m.created_at_utc,
                    m.created_by_manager_id,
                    m.created_by_manager_name
                FROM communication_message AS m
                WHERE COALESCE(m.visible_from_utc, m.created_at_utc) <= {nowUtc}
                  AND m.deadline_at_utc > {nowUtc}
                  AND NOT EXISTS (
                      SELECT 1
                      FROM employee_communication_dismissal AS d
                      WHERE d.communication_message_id = m.id
                        AND d.employee_id = {employeeId}
                  )
                ORDER BY m.deadline_at_utc, m.created_at_utc, m.id
                """)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(int communicationId, CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .AnyAsync(message => message.Id == communicationId, ct);

    public Task<bool> DismissalExistsAsync(int communicationId, int employeeId, CancellationToken ct = default)
        => _db.EmployeeCommunicationDismissals
            .AsNoTracking()
            .AnyAsync(
                dismissal =>
                    dismissal.CommunicationMessageId == communicationId &&
                    dismissal.EmployeeId == employeeId,
                ct);

    public async Task AddDismissalAsync(EmployeeCommunicationDismissalModel dismissal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dismissal);

        await _db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT OR IGNORE INTO employee_communication_dismissal (
                    communication_message_id,
                    employee_id,
                    dismissed_at_utc
                )
                VALUES (
                    {dismissal.CommunicationMessageId},
                    {dismissal.EmployeeId},
                    {dismissal.DismissedAtUtc}
                )
                """,
                ct)
            .ConfigureAwait(false);
    }
}
