using System.Windows.Forms;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class FakeForm : Form
{
    public int ShowCalls { get; private set; }
    public int BringToFrontCalls { get; private set; }

    public override void Show()
    {
        ShowCalls++;
    }

    public override void BringToFront()
    {
        BringToFrontCalls++;
    }
}
