using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView
    {
        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEditAdnCreate;
            Mode = ShopViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = ShopViewModel.List;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = ShopViewModel.Profile;
        }

        public void SetProfile(ShopModel m)
        {
            labelName.Text = m.Name;
            labelEmail.Text = string.IsNullOrWhiteSpace(m.Address) ? "—" : m.Address;
            labelPhone.Text = string.IsNullOrWhiteSpace(m.Description) ? "—" : m.Description;
            labelId.Text = m.Id.ToString();
        }

        public void SetShopListBindingSource(BindingSource shopList) =>
            dataGrid.DataSource = shopList;
    }
}
