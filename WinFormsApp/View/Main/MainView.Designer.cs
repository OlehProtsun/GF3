namespace WinFormsApp.View.Main
{
    partial class MainView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            btnEmployee = new Guna.UI2.WinForms.Guna2Button();
            leftBar = new Panel();
            btnAvailability = new Guna.UI2.WinForms.Guna2Button();
            leftBar.SuspendLayout();
            SuspendLayout();
            // 
            // btnEmployee
            // 
            btnEmployee.AutoRoundedCorners = true;
            btnEmployee.BackColor = Color.Transparent;
            btnEmployee.CustomizableEdges = customizableEdges1;
            btnEmployee.DisabledState.BorderColor = Color.DarkGray;
            btnEmployee.DisabledState.CustomBorderColor = Color.DarkGray;
            btnEmployee.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnEmployee.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnEmployee.FillColor = Color.Lime;
            btnEmployee.Font = new Font("Segoe UI", 9F);
            btnEmployee.ForeColor = Color.Black;
            btnEmployee.Location = new Point(4, 4);
            btnEmployee.Name = "btnEmployee";
            btnEmployee.ShadowDecoration.BorderRadius = 19;
            btnEmployee.ShadowDecoration.CustomizableEdges = customizableEdges2;
            btnEmployee.ShadowDecoration.Depth = 5;
            btnEmployee.ShadowDecoration.Enabled = true;
            btnEmployee.Size = new Size(63, 38);
            btnEmployee.TabIndex = 1;
            btnEmployee.Text = "Employee";
            // 
            // leftBar
            // 
            leftBar.Controls.Add(btnAvailability);
            leftBar.Controls.Add(btnEmployee);
            leftBar.Dock = DockStyle.Left;
            leftBar.Location = new Point(0, 0);
            leftBar.Name = "leftBar";
            leftBar.Size = new Size(71, 499);
            leftBar.TabIndex = 2;
            // 
            // btnAvailability
            // 
            btnAvailability.AutoRoundedCorners = true;
            btnAvailability.BackColor = Color.Transparent;
            btnAvailability.CustomizableEdges = customizableEdges3;
            btnAvailability.DisabledState.BorderColor = Color.DarkGray;
            btnAvailability.DisabledState.CustomBorderColor = Color.DarkGray;
            btnAvailability.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnAvailability.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnAvailability.FillColor = Color.Lime;
            btnAvailability.Font = new Font("Segoe UI", 9F);
            btnAvailability.ForeColor = Color.Black;
            btnAvailability.Location = new Point(4, 48);
            btnAvailability.Name = "btnAvailability";
            btnAvailability.ShadowDecoration.BorderRadius = 19;
            btnAvailability.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnAvailability.ShadowDecoration.Depth = 5;
            btnAvailability.ShadowDecoration.Enabled = true;
            btnAvailability.Size = new Size(63, 38);
            btnAvailability.TabIndex = 2;
            btnAvailability.Text = "Avaialbility";
            // 
            // MainView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(866, 499);
            Controls.Add(leftBar);
            IsMdiContainer = true;
            Name = "MainView";
            Text = "MainView";
            leftBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Guna.UI2.WinForms.Guna2Button btnEmployee;
        private Panel leftBar;
        private Guna.UI2.WinForms.Guna2Button btnAvailability;
    }
}