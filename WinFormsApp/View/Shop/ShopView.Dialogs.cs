using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView
    {
        private DialogResult ShowDialog(
            string text,
            Guna.UI2.WinForms.MessageDialogIcon icon,
            Guna.UI2.WinForms.MessageDialogButtons buttons,
            string? caption = null)
        {
            var oldCaption = MessageDialog.Caption;

            MessageDialog.Icon = icon;
            MessageDialog.Buttons = buttons;
            MessageDialog.Text = text;
            MessageDialog.Caption = string.IsNullOrWhiteSpace(caption) ? oldCaption : caption;

            var result = MessageDialog.Show();

            MessageDialog.Caption = oldCaption;
            return result;
        }

        public void ShowInfo(string text) =>
            ShowDialog(text, Guna.UI2.WinForms.MessageDialogIcon.Information, Guna.UI2.WinForms.MessageDialogButtons.OK);

        public void ShowError(string text) =>
            ShowDialog(text, Guna.UI2.WinForms.MessageDialogIcon.Error, Guna.UI2.WinForms.MessageDialogButtons.OK);

        public bool Confirm(string text, string? caption = null) =>
            ShowDialog(text, Guna.UI2.WinForms.MessageDialogIcon.Question, Guna.UI2.WinForms.MessageDialogButtons.YesNo, caption)
                == DialogResult.Yes;
    }
}
