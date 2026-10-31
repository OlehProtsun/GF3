using System.Reflection;

namespace GF3.Tests;

public sealed class ServiceMappingHelperCoverageTests
{
    [Fact]
    public void NormalizeReadCancellationToken_DropsCancelableTokensButKeepsDefault()
    {
        var helperType = GetHelperType();
        using var cts = new CancellationTokenSource();

        var cancelable = (CancellationToken)helperType
            .GetMethod("NormalizeReadCancellationToken", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [cts.Token])!;
        var defaultToken = (CancellationToken)helperType
            .GetMethod("NormalizeReadCancellationToken", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [CancellationToken.None])!;

        Assert.Equal(CancellationToken.None, cancelable);
        Assert.Equal(CancellationToken.None, defaultToken);
    }

    [Fact]
    public async Task GetMappedAsync_MapsLoadedEntityAndNormalizesReadCancellation()
    {
        var helperType = GetHelperType();
        using var cts = new CancellationTokenSource();
        CancellationToken observedToken = cts.Token;

        var result = await InvokeGenericAsync<SourceModel, ContractModel, ContractModel?>(
            helperType,
            "GetMappedAsync",
            new Func<CancellationToken, Task<SourceModel?>>(token =>
            {
                observedToken = token;
                return Task.FromResult<SourceModel?>(new SourceModel("loaded"));
            }),
            new Func<SourceModel, ContractModel>(source => new ContractModel(source.Value.ToUpperInvariant())),
            cts.Token);

        Assert.Equal(CancellationToken.None, observedToken);
        Assert.NotNull(result);
        Assert.Equal("LOADED", result!.Value);
    }

    [Fact]
    public async Task GetMappedAsync_ReturnsNullWhenLoaderDoesNotFindEntity()
    {
        var helperType = GetHelperType();

        var result = await InvokeGenericAsync<SourceModel, ContractModel, ContractModel?>(
            helperType,
            "GetMappedAsync",
            new Func<CancellationToken, Task<SourceModel?>>(_ => Task.FromResult<SourceModel?>(null)),
            new Func<SourceModel, ContractModel>(source => new ContractModel(source.Value)),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMappedListAsync_MapsEveryItemAndNormalizesReadCancellation()
    {
        var helperType = GetHelperType();
        using var cts = new CancellationTokenSource();
        CancellationToken observedToken = cts.Token;

        var result = await InvokeGenericAsync<SourceModel, ContractModel, List<ContractModel>>(
            helperType,
            "GetMappedListAsync",
            new Func<CancellationToken, Task<List<SourceModel>>>(token =>
            {
                observedToken = token;
                return Task.FromResult(new List<SourceModel>
                {
                    new("first"),
                    new("second"),
                });
            }),
            new Func<SourceModel, ContractModel>(source => new ContractModel(source.Value)),
            cts.Token);

        Assert.Equal(CancellationToken.None, observedToken);
        Assert.Equal(["first", "second"], result.Select(item => item.Value));
    }

    [Fact]
    public async Task CreateMappedAsync_AndExecuteAndMapAsync_PreserveWritableCancellationToken()
    {
        var helperType = GetHelperType();
        using var cts = new CancellationTokenSource();
        CancellationToken createToken = CancellationToken.None;
        CancellationToken executeToken = CancellationToken.None;

        var created = await InvokeGenericAsync<SourceModel, ContractModel, ContractModel>(
            helperType,
            "CreateMappedAsync",
            new SourceModel("draft"),
            new Func<SourceModel, CancellationToken, Task<SourceModel>>((source, token) =>
            {
                createToken = token;
                return Task.FromResult(new SourceModel($"{source.Value}-saved"));
            }),
            new Func<SourceModel, ContractModel>(source => new ContractModel(source.Value)),
            cts.Token);
        var executed = await InvokeGenericAsync<SourceModel, ContractModel, ContractModel>(
            helperType,
            "ExecuteAndMapAsync",
            new Func<CancellationToken, Task<SourceModel>>(token =>
            {
                executeToken = token;
                return Task.FromResult(new SourceModel("executed"));
            }),
            new Func<SourceModel, ContractModel>(source => new ContractModel(source.Value)),
            cts.Token);

        Assert.Equal(cts.Token, createToken);
        Assert.Equal(cts.Token, executeToken);
        Assert.Equal("draft-saved", created.Value);
        Assert.Equal("executed", executed.Value);
    }

    private static Type GetHelperType()
        => typeof(BusinessLogicLayer.Services.EmployeeService).Assembly
            .GetType("BusinessLogicLayer.Services.ServiceMappingHelper", throwOnError: true)!;

    private static async Task<TResult> InvokeGenericAsync<TDal, TContract, TResult>(
        Type helperType,
        string methodName,
        params object?[] arguments)
    {
        var method = helperType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == methodName && method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(typeof(TDal), typeof(TContract));

        var task = (Task<TResult>)method.Invoke(null, arguments)!;
        return await task.ConfigureAwait(false);
    }

    private sealed record SourceModel(string Value);

    private sealed record ContractModel(string Value);
}
