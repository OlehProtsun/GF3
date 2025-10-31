using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp.View.Main
{
    public partial class MainView : Form, IMainView
    {
        public MainView()
        {
            InitializeComponent();

            btnEmployee.Click += async (_, __) =>
            {
                if (ShowEmployeeView is not null)
                    await ShowEmployeeView(CancellationToken.None);
            };
        }

        public event Func<CancellationToken, Task>? ShowEmployeeView;

    }
}
