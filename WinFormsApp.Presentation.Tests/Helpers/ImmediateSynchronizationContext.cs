using System;
using System.Threading;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class ImmediateSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state) => d(state);
    public override void Send(SendOrPostCallback d, object? state) => d(state);
}

internal sealed class SynchronizationContextScope : IDisposable
{
    private readonly SynchronizationContext? _original;

    public SynchronizationContextScope(SynchronizationContext? context = null)
    {
        _original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context ?? new ImmediateSynchronizationContext());
    }

    public void Dispose()
    {
        SynchronizationContext.SetSynchronizationContext(_original);
    }
}
