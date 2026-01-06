using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp.View.Container.Helpers
{
    public class NoFocusScrollPanel : Panel
    {
        public NoFocusScrollPanel()
        {
            AutoScroll = true;               // скрол є
            HorizontalScroll.Enabled = false; // якщо горизонтальний не треба
            HorizontalScroll.Visible = false;
        }

        protected override Point ScrollToControl(Control activeControl)
        {
            // але панель НЕ стрибає до сфокусовного контролу
            return DisplayRectangle.Location;
        }
    }
}
