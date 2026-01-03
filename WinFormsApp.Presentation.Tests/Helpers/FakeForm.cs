using System.Windows.Forms;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class FakeForm : Form
{
    public int ShowCalls { get; private set; }
    public int BringToFrontCalls { get; private set; }

    // Use 'new' to hide the inherited non-virtual method
    public new void Show()
    {
        ShowCalls++;
    }

    public new void BringToFront()
    {
        BringToFrontCalls++;
    }
}
