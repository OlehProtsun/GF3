using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        public void ShowInfo(string text)
        {
            MessageDialog.Caption = null;
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Information;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public void ShowError(string text)
        {
            MessageDialog.Caption = null;
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Error;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public bool Confirm(string text, string? caption = null)
        {
            MessageDialog.Caption = caption;
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Question;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.YesNo;
            MessageDialog.Text = text;

            return MessageDialog.Show() == DialogResult.Yes;
        }
    }
}
