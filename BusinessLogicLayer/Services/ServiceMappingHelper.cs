namespace BusinessLogicLayer.Services;

/// <summary>
/// Small utility layer used by services that mostly map DAL entities to business contracts.
/// It keeps the repetitive "load -> normalize read cancellation -> map" flow in one place,
/// so the service methods can focus on business rules instead of plumbing.
/// </summary>
internal static class ServiceMappingHelper
{
    /// <summary>
    /// Read queries are often triggered by short-lived UI requests. We intentionally do not
    /// propagate cancelable tokens to read operations here, because the caller may cancel the
    /// HTTP/UI request after the query already started, while we still prefer a clean read over
    /// a half-aborted EF call.
    /// </summary>
    public static CancellationToken NormalizeReadCancellationToken(CancellationToken ct = default)
        => ct.CanBeCanceled ? CancellationToken.None : ct;

    /// <summary>
    /// Loads a single DAL entity and maps it to a business contract while preserving nullability.
    /// </summary>
    public static async Task<TContract?> GetMappedAsync<TDal, TContract>(
        Func<CancellationToken, Task<TDal?>> loader,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
        where TDal : class
        where TContract : class
    {
        var model = await loader(NormalizeReadCancellationToken(ct)).ConfigureAwait(false);
        return model is null ? null : mapper(model);
    }

    /// <summary>
    /// Loads a list of DAL entities and projects it to the business layer representation.
    /// </summary>
    public static async Task<List<TContract>> GetMappedListAsync<TDal, TContract>(
        Func<CancellationToken, Task<List<TDal>>> loader,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var items = await loader(NormalizeReadCancellationToken(ct)).ConfigureAwait(false);
        return items.Select(mapper).ToList();
    }

    /// <summary>
    /// Creates a DAL entity and immediately maps the persisted result back to the contract model.
    /// </summary>
    public static async Task<TContract> CreateMappedAsync<TDal, TContract>(
        TDal entity,
        Func<TDal, CancellationToken, Task<TDal>> creator,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var created = await creator(entity, ct).ConfigureAwait(false);
        return mapper(created);
    }

    /// <summary>
    /// Executes an arbitrary async action and maps the result when the service wants custom logic
    /// around the persistence call but still prefers to centralize the final projection.
    /// </summary>
    public static async Task<TContract> ExecuteAndMapAsync<TDal, TContract>(
        Func<CancellationToken, Task<TDal>> action,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var result = await action(ct).ConfigureAwait(false);
        return mapper(result);
    }
}
