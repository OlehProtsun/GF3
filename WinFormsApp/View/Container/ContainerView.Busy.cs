using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
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
            yield return btnBackToContainerList;
            yield return btnBackToContainerListFromProfile;
            yield return btnCancelProfile;
            yield return btnCancelProfile2;

            yield return btnScheduleSearch;
            yield return btnScheduleAdd;
            yield return btnScheduleEdit;
            yield return btnScheduleDelete;
            yield return btnScheduleSave;
            yield return btnScheduleCancel;
            yield return btnBackToScheduleList;
            yield return btnBackToContainerProfileFromSheduleProfile;
            yield return btnGenerate;
            yield return btnSearchShopFromScheduleEdit;
            yield return btnSearchAvailabilityFromScheduleEdit;
            yield return btnSearchEmployeeInAvailabilityEdit;
            yield return btnAddEmployeeToGroup;
            yield return btnRemoveEmployeeFromGroup;
            yield return guna2Button30;
        }
    }
}
