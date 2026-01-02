using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private void ToggleScheduleInfo()
        {
            _scheduleInfoExpanded = !_scheduleInfoExpanded;
            ApplyScheduleInfoState(_scheduleInfoExpanded);
        }

        private void ApplyScheduleInfoState(bool expanded)
        {
            // збережемо скрол, щоб не "смикало"
            var scroll = panel1.AutoScrollPosition;

            panel1.SuspendLayout();
            try
            {
                guna2GroupBox5.Height = expanded ? _scheduleInfoExpandedHeight : ScheduleInfoCollapsedHeight;

                // щоб у "згорнутому" стані не було таб-стопу по прихованих полях
                foreach (Control c in guna2GroupBox5.Controls)
                {
                    // лишаємо видимими кнопку Show/Hide і guna2Button6
                    if (ReferenceEquals(c, btnShowHideInfo) || ReferenceEquals(c, guna2Button6))
                        continue;

                    c.Visible = expanded;
                    c.TabStop = expanded;
                }

                btnShowHideInfo.Text = expanded ? "Hide" : "Show";
            }
            finally
            {
                panel1.ResumeLayout(true);

                // AutoScrollPosition працює з "мінусами"
                panel1.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
            }
        }
    }
}
