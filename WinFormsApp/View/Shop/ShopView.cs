using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView : BusyForm, IShopView
    {
        public ShopView()
        {
            InitializeComponent();
            ConfigureGrid();
            AssociateAndRaiseViewEvents();
        }
    }
}
