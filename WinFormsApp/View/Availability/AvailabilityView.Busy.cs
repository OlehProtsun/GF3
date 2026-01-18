using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
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
            yield return btnBackToAvailabilityList;
            yield return btnBackToAvailabilityListFromProfile;
            yield return btnCancelProfile2;
            yield return btnCacnelAvailabilityEdit2;
            yield return btnCacnelAvailabilityEdit3;
            yield return btnAddEmployeeToGroup;
            yield return btnRemoveEmployeeFromGroup;
            yield return btnAddNewBind;
            yield return btnDeleteBind;
        }
    }
}
