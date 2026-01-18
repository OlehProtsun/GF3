using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView
    {
        public override CancellationToken LifetimeToken => _lifetimeCts.Token;

        protected override IEnumerable<Control> BusyControls()
        {
            yield return btnSearch;
            yield return btnAdd;
            yield return btnEdit;
            yield return btnDelete;
            yield return btnSave;
            yield return btnCancel;
            yield return btnCancelProfile;
            yield return btnBackToShopList;
            yield return btnBackToShopListFromProfile;
        }
    }
}
