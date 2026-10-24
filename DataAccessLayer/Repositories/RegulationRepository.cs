using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public sealed class RegulationRepository : GenericRepository<RegulationDocumentModel>, IRegulationRepository
{
    public RegulationRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<RegulationDocumentModel>> ListDocumentsAsync(CancellationToken ct = default)
        => _set.AsNoTracking()
            .Include(document => document.Acceptances)
            .OrderByDescending(document => document.Id)
            .ToListAsync(ct);

    public Task<List<RegulationDocumentModel>> ListPendingAsync(string role, int accountId, CancellationToken ct = default)
        => _set.AsNoTracking()
            .Where(document => document.IsPublished && !document.Acceptances.Any(
                acceptance => acceptance.AccountRole == role && acceptance.AccountId == accountId))
            .OrderBy(document => document.Id)
            .ToListAsync(ct);

    public Task<List<RegulationAcceptanceModel>> ListAcceptancesAsync(CancellationToken ct = default)
        => _db.RegulationAcceptances.AsNoTracking()
            .Include(acceptance => acceptance.RegulationDocument)
            .OrderByDescending(acceptance => acceptance.Id)
            .ToListAsync(ct);

    public Task<List<RegulationAcceptanceModel>> ListAcceptancesAsync(
        string role,
        int accountId,
        CancellationToken ct = default)
        => _db.RegulationAcceptances.AsNoTracking()
            .Include(acceptance => acceptance.RegulationDocument)
            .Where(acceptance => acceptance.AccountRole == role && acceptance.AccountId == accountId)
            .OrderByDescending(acceptance => acceptance.Id)
            .ToListAsync(ct);

    public Task<bool> VersionExistsAsync(string version, int? exceptDocumentId = null, CancellationToken ct = default)
        => _set.AsNoTracking().AnyAsync(
            document => document.Version == version && (!exceptDocumentId.HasValue || document.Id != exceptDocumentId.Value),
            ct);

    public Task<bool> HasAcceptancesAsync(int documentId, CancellationToken ct = default)
        => _db.RegulationAcceptances.AsNoTracking().AnyAsync(
            acceptance => acceptance.RegulationDocumentId == documentId,
            ct);

    public async Task<RegulationAcceptanceModel> AddAcceptanceAsync(
        RegulationAcceptanceModel acceptance,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT OR IGNORE INTO regulation_acceptance (
                regulation_document_id, account_role, account_id,
                username_snapshot, display_name_snapshot, accepted_at_utc
            ) VALUES (
                {acceptance.RegulationDocumentId}, {acceptance.AccountRole}, {acceptance.AccountId},
                {acceptance.UsernameSnapshot}, {acceptance.DisplayNameSnapshot}, {acceptance.AcceptedAtUtc}
            )
            """,
            ct).ConfigureAwait(false);

        return await _db.RegulationAcceptances.AsNoTracking()
            .Include(item => item.RegulationDocument)
            .SingleAsync(item =>
                item.RegulationDocumentId == acceptance.RegulationDocumentId &&
                item.AccountRole == acceptance.AccountRole &&
                item.AccountId == acceptance.AccountId,
                ct)
            .ConfigureAwait(false);
    }
}
