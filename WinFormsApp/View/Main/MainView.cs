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

            btnAvailability.Click += async (_, __) =>
            {
                if (ShowAvailabilityView is not null)
                    await ShowAvailabilityView(CancellationToken.None);
            };

            btnShop.Click += async (_, __) =>
            {
                if (ShowShopView is not null)
                    await ShowShopView(CancellationToken.None);
            };
        }

        public event Func<CancellationToken, Task>? ShowEmployeeView;
        public event Func<CancellationToken, Task>? ShowAvailabilityView;
        public event Func<CancellationToken, Task>? ShowShopView;

    }
}
