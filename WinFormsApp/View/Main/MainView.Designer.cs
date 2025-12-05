namespace WinFormsApp.View.Main
{
    partial class MainView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Designer fields
        private Guna.UI2.WinForms.Guna2GradientButton btnEmployee;
        private Guna.UI2.WinForms.Guna2GradientButton btnAvailability;
        private Guna.UI2.WinForms.Guna2GradientButton btnShop;
        private System.Windows.Forms.Panel leftBar;
        private Guna.UI2.WinForms.Guna2ShadowPanel navShadowPanel;
        #endregion

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support — do not modify
        /// the contents of this method with the code editor.
        /// (This method replaces the previous InitializeComponent with an updated modern design.)
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            leftBar = new Panel();
            navShadowPanel = new Guna.UI2.WinForms.Guna2ShadowPanel();
            btnShop = new Guna.UI2.WinForms.Guna2GradientButton();
            btnAvailability = new Guna.UI2.WinForms.Guna2GradientButton();
            btnEmployee = new Guna.UI2.WinForms.Guna2GradientButton();
            leftBar.SuspendLayout();
            navShadowPanel.SuspendLayout();
            SuspendLayout();
            // 
            // leftBar
            // 
            leftBar.BackColor = Color.Transparent;
            leftBar.Controls.Add(navShadowPanel);
            leftBar.Dock = DockStyle.Left;
            leftBar.Location = new Point(0, 0);
            leftBar.Name = "leftBar";
            leftBar.Size = new Size(90, 620);
            leftBar.TabIndex = 2;
            // 
            // navShadowPanel
            // 
            navShadowPanel.BackColor = Color.Transparent;
            navShadowPanel.Controls.Add(btnShop);
            navShadowPanel.Controls.Add(btnAvailability);
            navShadowPanel.Controls.Add(btnEmployee);
            navShadowPanel.Dock = DockStyle.Fill;
            navShadowPanel.FillColor = Color.FromArgb(24, 24, 30);
            navShadowPanel.Location = new Point(0, 0);
            navShadowPanel.Name = "navShadowPanel";
            navShadowPanel.Padding = new Padding(8);
            navShadowPanel.Radius = 18;
            navShadowPanel.ShadowColor = Color.FromArgb(40, 40, 45);
            navShadowPanel.ShadowShift = 6;
            navShadowPanel.Size = new Size(90, 620);
            navShadowPanel.TabIndex = 10;
            //
            // btnShop
            //
            btnShop.AccessibleName = "btnShop";
            btnShop.AutoRoundedCorners = true;
            btnShop.BackColor = Color.Transparent;
            btnShop.BorderRadius = 20;
            btnShop.CustomizableEdges = customizableEdges5;
            btnShop.DisabledState.BorderColor = Color.DarkGray;
            btnShop.DisabledState.CustomBorderColor = Color.DarkGray;
            btnShop.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnShop.DisabledState.FillColor2 = Color.FromArgb(141, 141, 141);
            btnShop.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnShop.FillColor = Color.FromArgb(124, 197, 118);
            btnShop.FillColor2 = Color.FromArgb(46, 204, 113);
            btnShop.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnShop.ForeColor = Color.Black;
            btnShop.HoverState.FillColor = Color.FromArgb(110, 182, 105);
            btnShop.HoverState.FillColor2 = Color.FromArgb(39, 182, 104);
            btnShop.ImageAlign = HorizontalAlignment.Left;
            btnShop.ImageSize = new Size(18, 18);
            btnShop.Location = new Point(10, 124);
            btnShop.Name = "btnShop";
            btnShop.ShadowDecoration.BorderRadius = 20;
            btnShop.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnShop.ShadowDecoration.Depth = 6;
            btnShop.ShadowDecoration.Enabled = true;
            btnShop.Size = new Size(70, 42);
            btnShop.TabIndex = 3;
            btnShop.Text = "Shop";
            btnShop.UseTransparentBackground = true;
            //
            // btnAvailability
            //
            btnAvailability.AccessibleName = "btnAvailability";
            btnAvailability.AutoRoundedCorners = true;
            btnAvailability.BackColor = Color.Transparent;
            btnAvailability.BorderRadius = 20;
            btnAvailability.CustomizableEdges = customizableEdges1;
            btnAvailability.DisabledState.BorderColor = Color.DarkGray;
            btnAvailability.DisabledState.CustomBorderColor = Color.DarkGray;
            btnAvailability.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnAvailability.DisabledState.FillColor2 = Color.FromArgb(141, 141, 141);
            btnAvailability.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnAvailability.FillColor = Color.FromArgb(253, 203, 110);
            btnAvailability.FillColor2 = Color.FromArgb(245, 124, 0);
            btnAvailability.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnAvailability.ForeColor = Color.Black;
            btnAvailability.HoverState.FillColor = Color.FromArgb(245, 190, 90);
            btnAvailability.HoverState.FillColor2 = Color.FromArgb(230, 100, 10);
            btnAvailability.ImageAlign = HorizontalAlignment.Left;
            btnAvailability.ImageSize = new Size(18, 18);
            btnAvailability.Location = new Point(10, 70);
            btnAvailability.Name = "btnAvailability";
            btnAvailability.ShadowDecoration.BorderRadius = 20;
            btnAvailability.ShadowDecoration.CustomizableEdges = customizableEdges2;
            btnAvailability.ShadowDecoration.Depth = 6;
            btnAvailability.ShadowDecoration.Enabled = true;
            btnAvailability.Size = new Size(70, 42);
            btnAvailability.TabIndex = 2;
            btnAvailability.Text = "Availability";
            btnAvailability.UseTransparentBackground = true;
            // 
            // btnEmployee
            // 
            btnEmployee.AccessibleName = "btnEmployee";
            btnEmployee.AutoRoundedCorners = true;
            btnEmployee.BackColor = Color.Transparent;
            btnEmployee.BorderRadius = 20;
            btnEmployee.CustomizableEdges = customizableEdges3;
            btnEmployee.DisabledState.BorderColor = Color.DarkGray;
            btnEmployee.DisabledState.CustomBorderColor = Color.DarkGray;
            btnEmployee.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnEmployee.DisabledState.FillColor2 = Color.FromArgb(141, 141, 141);
            btnEmployee.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnEmployee.FillColor = Color.FromArgb(78, 205, 196);
            btnEmployee.FillColor2 = Color.FromArgb(142, 68, 173);
            btnEmployee.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnEmployee.ForeColor = Color.White;
            btnEmployee.HoverState.FillColor = Color.FromArgb(64, 196, 186);
            btnEmployee.HoverState.FillColor2 = Color.FromArgb(122, 55, 150);
            btnEmployee.ImageAlign = HorizontalAlignment.Left;
            btnEmployee.ImageSize = new Size(18, 18);
            btnEmployee.Location = new Point(10, 16);
            btnEmployee.Name = "btnEmployee";
            btnEmployee.ShadowDecoration.BorderRadius = 20;
            btnEmployee.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnEmployee.ShadowDecoration.Depth = 6;
            btnEmployee.ShadowDecoration.Enabled = true;
            btnEmployee.Size = new Size(70, 42);
            btnEmployee.TabIndex = 1;
            btnEmployee.Text = "Employee";
            btnEmployee.UseTransparentBackground = true;
            // 
            // MainView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 250, 252);
            ClientSize = new Size(1154, 620);
            Controls.Add(leftBar);
            IsMdiContainer = true;
            Name = "MainView";
            Text = "MainView";
            leftBar.ResumeLayout(false);
            navShadowPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
