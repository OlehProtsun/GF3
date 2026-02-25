namespace BusinessLogicLayer.Services;

internal static class ServiceMappingHelper
{
    public static async Task<TContract?> GetMappedAsync<TDal, TContract>(
        Func<CancellationToken, Task<TDal?>> loader,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
        where TDal : class
        where TContract : class
    {
        var model = await loader(ct).ConfigureAwait(false);
        return model is null ? null : mapper(model);
    }

    public static async Task<List<TContract>> GetMappedListAsync<TDal, TContract>(
        Func<CancellationToken, Task<List<TDal>>> loader,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var items = await loader(ct).ConfigureAwait(false);
        return items.Select(mapper).ToList();
    }

    public static async Task<TContract> CreateMappedAsync<TDal, TContract>(
        TDal entity,
        Func<TDal, CancellationToken, Task<TDal>> creator,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var created = await creator(entity, ct).ConfigureAwait(false);
        return mapper(created);
    }

    public static async Task<TContract> ExecuteAndMapAsync<TDal, TContract>(
        Func<CancellationToken, Task<TDal>> action,
        Func<TDal, TContract> mapper,
        CancellationToken ct = default)
    {
        var result = await action(ct).ConfigureAwait(false);
        return mapper(result);
    }
}
