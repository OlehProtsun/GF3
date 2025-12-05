using Guna.UI2.WinForms;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp.View.Container
{
    partial class ContainerView
    {
        /// <summary>
        ///  Required designer variable.
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

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            tabList = new TabPage();
            btnDelete = new Guna.UI2.WinForms.Guna2Button();
            btnEdit = new Guna.UI2.WinForms.Guna2Button();
            btnAdd = new Guna.UI2.WinForms.Guna2Button();
            btnSearch = new Guna.UI2.WinForms.Guna2Button();
            inputSearch = new Guna.UI2.WinForms.Guna2TextBox();
            containerGrid = new Guna.UI2.WinForms.Guna2DataGridView();
            tabEdit = new TabPage();
            btnCancel = new Guna.UI2.WinForms.Guna2Button();
            btnSave = new Guna.UI2.WinForms.Guna2Button();
            inputContainerNote = new Guna.UI2.WinForms.Guna2TextBox();
            label3 = new Label();
            inputContainerName = new Guna.UI2.WinForms.Guna2TextBox();
            label2 = new Label();
            numberContainerId = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label1 = new Label();
            tabProfile = new TabPage();
            btnOpenScheduleProfile = new Guna.UI2.WinForms.Guna2Button();
            btnScheduleDelete = new Guna.UI2.WinForms.Guna2Button();
            btnScheduleEdit = new Guna.UI2.WinForms.Guna2Button();
            btnScheduleAdd = new Guna.UI2.WinForms.Guna2Button();
            btnScheduleSearch = new Guna.UI2.WinForms.Guna2Button();
            inputScheduleSearch = new Guna.UI2.WinForms.Guna2TextBox();
            scheduleGrid = new Guna.UI2.WinForms.Guna2DataGridView();
            label5 = new Label();
            label4 = new Label();
            lblContainerNote = new Label();
            lblContainerName = new Label();
            tabScheduleEdit = new TabPage();
            btnScheduleCancel = new Guna.UI2.WinForms.Guna2Button();
            btnScheduleSave = new Guna.UI2.WinForms.Guna2Button();
            slotGrid = new Guna.UI2.WinForms.Guna2DataGridView();
            btnGenerate = new Guna.UI2.WinForms.Guna2Button();
            checkedAvailabilities = new Guna.UI2.WinForms.Guna2CheckedListBox();
            comboStatus = new Guna.UI2.WinForms.Guna2ComboBox();
            label21 = new Label();
            comboShop = new Guna.UI2.WinForms.Guna2ComboBox();
            inputScheduleComment = new Guna.UI2.WinForms.Guna2TextBox();
            label20 = new Label();
            inputMaxFull = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label19 = new Label();
            inputMaxConsecutiveFull = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label18 = new Label();
            inputMaxConsecutiveDays = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label17 = new Label();
            inputMaxHours = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label16 = new Label();
            inputShift2 = new Guna.UI2.WinForms.Guna2TextBox();
            label15 = new Label();
            inputShift1 = new Guna.UI2.WinForms.Guna2TextBox();
            label14 = new Label();
            inputPeoplePerShift = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label13 = new Label();
            inputMonth = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label12 = new Label();
            inputYear = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label11 = new Label();
            inputScheduleName = new Guna.UI2.WinForms.Guna2TextBox();
            label10 = new Label();
            numberScheduleId = new Guna.UI2.WinForms.Guna2NumericUpDown();
            label9 = new Label();
            tabScheduleProfile = new TabPage();
            scheduleSlotProfileGrid = new Guna.UI2.WinForms.Guna2DataGridView();
            lblScheduleSummary = new Label();
            errorProviderContainer = new ErrorProvider(components);
            errorProviderSchedule = new ErrorProvider(components);
            tabControl.SuspendLayout();
            tabList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)containerGrid).BeginInit();
            tabEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numberContainerId).BeginInit();
            tabProfile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)scheduleGrid).BeginInit();
            tabScheduleEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)slotGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxFull).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxConsecutiveFull).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxConsecutiveDays).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxHours).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputPeoplePerShift).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputMonth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)inputYear).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numberScheduleId).BeginInit();
            tabScheduleProfile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)scheduleSlotProfileGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProviderContainer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProviderSchedule).BeginInit();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabList);
            tabControl.Controls.Add(tabEdit);
            tabControl.Controls.Add(tabProfile);
            tabControl.Controls.Add(tabScheduleEdit);
            tabControl.Controls.Add(tabScheduleProfile);
            tabControl.Alignment = TabAlignment.Left;
            tabControl.Dock = DockStyle.Fill;
            tabControl.ItemSize = new Size(180, 40);
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1024, 640);
            tabControl.TabButtonHoverState.BorderColor = Color.Empty;
            tabControl.TabButtonHoverState.FillColor = Color.FromArgb(40, 52, 70);
            tabControl.TabButtonHoverState.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            tabControl.TabButtonHoverState.ForeColor = Color.White;
            tabControl.TabButtonHoverState.InnerColor = Color.FromArgb(40, 52, 70);
            tabControl.TabButtonIdleState.BorderColor = Color.Empty;
            tabControl.TabButtonIdleState.FillColor = Color.FromArgb(33, 42, 57);
            tabControl.TabButtonIdleState.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            tabControl.TabButtonIdleState.ForeColor = Color.FromArgb(156, 160, 167);
            tabControl.TabButtonIdleState.InnerColor = Color.FromArgb(33, 42, 57);
            tabControl.TabButtonSelectedState.BorderColor = Color.Empty;
            tabControl.TabButtonSelectedState.FillColor = Color.FromArgb(76, 132, 255);
            tabControl.TabButtonSelectedState.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            tabControl.TabButtonSelectedState.ForeColor = Color.White;
            tabControl.TabButtonSelectedState.InnerColor = Color.FromArgb(76, 132, 255);
            tabControl.TabButtonSize = new Size(180, 40);
            tabControl.TabIndex = 0;
            tabControl.TabMenuBackColor = Color.FromArgb(33, 42, 57);
            tabControl.TabMenuOrientation = TabMenuOrientation.VerticalLeft;
            // 
            // tabList
            // 
            tabList.Controls.Add(btnDelete);
            tabList.Controls.Add(btnEdit);
            tabList.Controls.Add(btnAdd);
            tabList.Controls.Add(btnSearch);
            tabList.Controls.Add(inputSearch);
            tabList.Controls.Add(containerGrid);
            tabList.Location = new Point(4, 24);
            tabList.Name = "tabList";
            tabList.Padding = new Padding(3);
            tabList.Size = new Size(1016, 612);
            tabList.TabIndex = 0;
            tabList.Text = "Containers";
            tabList.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.BorderRadius = 8;
            btnDelete.FillColor = Color.FromArgb(231, 76, 60);
            btnDelete.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnDelete.ForeColor = Color.White;
            btnDelete.Location = new Point(374, 16);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(90, 30);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            //
            // btnEdit
            //
            btnEdit.BorderRadius = 8;
            btnEdit.FillColor = Color.FromArgb(51, 152, 219);
            btnEdit.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnEdit.ForeColor = Color.White;
            btnEdit.Location = new Point(278, 16);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(90, 30);
            btnEdit.TabIndex = 4;
            btnEdit.Text = "Edit";
            //
            // btnAdd
            //
            btnAdd.BorderRadius = 8;
            btnAdd.FillColor = Color.FromArgb(46, 204, 113);
            btnAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnAdd.ForeColor = Color.White;
            btnAdd.Location = new Point(182, 16);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(90, 30);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add";
            //
            // btnSearch
            //
            btnSearch.BorderRadius = 8;
            btnSearch.FillColor = Color.FromArgb(76, 132, 255);
            btnSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnSearch.ForeColor = Color.White;
            btnSearch.Location = new Point(86, 16);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(90, 30);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Search";
            //
            // inputSearch
            //
            inputSearch.BorderRadius = 8;
            inputSearch.Location = new Point(8, 16);
            inputSearch.Name = "inputSearch";
            inputSearch.Size = new Size(172, 30);
            inputSearch.TabIndex = 1;
            // 
            // containerGrid
            // 
            containerGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            containerGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            containerGrid.BackgroundColor = Color.White;
            containerGrid.BorderStyle = BorderStyle.None;
            containerGrid.ColumnHeadersHeight = 32;
            containerGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            containerGrid.Location = new Point(8, 56);
            containerGrid.MultiSelect = false;
            containerGrid.Name = "containerGrid";
            containerGrid.RowHeadersVisible = false;
            containerGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            containerGrid.Size = new Size(1000, 548);
            containerGrid.TabIndex = 0;
            // 
            // tabEdit
            // 
            tabEdit.Controls.Add(btnCancel);
            tabEdit.Controls.Add(btnSave);
            tabEdit.Controls.Add(inputContainerNote);
            tabEdit.Controls.Add(label3);
            tabEdit.Controls.Add(inputContainerName);
            tabEdit.Controls.Add(label2);
            tabEdit.Controls.Add(numberContainerId);
            tabEdit.Controls.Add(label1);
            tabEdit.Location = new Point(4, 24);
            tabEdit.Name = "tabEdit";
            tabEdit.Padding = new Padding(3);
            tabEdit.Size = new Size(1016, 612);
            tabEdit.TabIndex = 1;
            tabEdit.Text = "Edit";
            tabEdit.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.BorderRadius = 8;
            btnCancel.FillColor = Color.FromArgb(231, 76, 60);
            btnCancel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(104, 200);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            //
            // btnSave
            //
            btnSave.BorderRadius = 8;
            btnSave.FillColor = Color.FromArgb(46, 204, 113);
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(23, 200);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 30);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            // 
            // inputContainerNote
            // 
            inputContainerNote.BorderRadius = 8;
            inputContainerNote.Location = new Point(23, 122);
            inputContainerNote.Multiline = true;
            inputContainerNote.Name = "inputContainerNote";
            inputContainerNote.Size = new Size(312, 60);
            inputContainerNote.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(23, 104);
            label3.Name = "label3";
            label3.Size = new Size(34, 15);
            label3.TabIndex = 4;
            label3.Text = "Note";
            // 
            // inputContainerName
            // 
            inputContainerName.BorderRadius = 8;
            inputContainerName.Location = new Point(23, 78);
            inputContainerName.Name = "inputContainerName";
            inputContainerName.Size = new Size(312, 30);
            inputContainerName.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(23, 60);
            label2.Name = "label2";
            label2.Size = new Size(39, 15);
            label2.TabIndex = 2;
            label2.Text = "Name";
            // 
            // numberContainerId
            // 
            numberContainerId.BorderRadius = 6;
            numberContainerId.Location = new Point(23, 34);
            numberContainerId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberContainerId.Name = "numberContainerId";
            numberContainerId.Size = new Size(120, 30);
            numberContainerId.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 16);
            label1.Name = "label1";
            label1.Size = new Size(57, 15);
            label1.TabIndex = 0;
            label1.Text = "Container";
            // 
            // tabProfile
            // 
            tabProfile.Controls.Add(btnOpenScheduleProfile);
            tabProfile.Controls.Add(btnScheduleDelete);
            tabProfile.Controls.Add(btnScheduleEdit);
            tabProfile.Controls.Add(btnScheduleAdd);
            tabProfile.Controls.Add(btnScheduleSearch);
            tabProfile.Controls.Add(inputScheduleSearch);
            tabProfile.Controls.Add(scheduleGrid);
            tabProfile.Controls.Add(label5);
            tabProfile.Controls.Add(label4);
            tabProfile.Controls.Add(lblContainerNote);
            tabProfile.Controls.Add(lblContainerName);
            tabProfile.Location = new Point(4, 24);
            tabProfile.Name = "tabProfile";
            tabProfile.Padding = new Padding(3);
            tabProfile.Size = new Size(1016, 612);
            tabProfile.TabIndex = 2;
            tabProfile.Text = "Profile";
            tabProfile.UseVisualStyleBackColor = true;
            // 
            // btnOpenScheduleProfile
            // 
            btnOpenScheduleProfile.BorderRadius = 8;
            btnOpenScheduleProfile.FillColor = Color.FromArgb(76, 132, 255);
            btnOpenScheduleProfile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnOpenScheduleProfile.ForeColor = Color.White;
            btnOpenScheduleProfile.Location = new Point(775, 40);
            btnOpenScheduleProfile.Name = "btnOpenScheduleProfile";
            btnOpenScheduleProfile.Size = new Size(120, 30);
            btnOpenScheduleProfile.TabIndex = 10;
            btnOpenScheduleProfile.Text = "Open profile";
            //
            // btnScheduleDelete
            //
            btnScheduleDelete.BorderRadius = 8;
            btnScheduleDelete.FillColor = Color.FromArgb(231, 76, 60);
            btnScheduleDelete.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleDelete.ForeColor = Color.White;
            btnScheduleDelete.Location = new Point(694, 40);
            btnScheduleDelete.Name = "btnScheduleDelete";
            btnScheduleDelete.Size = new Size(90, 30);
            btnScheduleDelete.TabIndex = 9;
            btnScheduleDelete.Text = "Delete";
            //
            // btnScheduleEdit
            //
            btnScheduleEdit.BorderRadius = 8;
            btnScheduleEdit.FillColor = Color.FromArgb(51, 152, 219);
            btnScheduleEdit.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleEdit.ForeColor = Color.White;
            btnScheduleEdit.Location = new Point(598, 40);
            btnScheduleEdit.Name = "btnScheduleEdit";
            btnScheduleEdit.Size = new Size(90, 30);
            btnScheduleEdit.TabIndex = 8;
            btnScheduleEdit.Text = "Edit";
            //
            // btnScheduleAdd
            //
            btnScheduleAdd.BorderRadius = 8;
            btnScheduleAdd.FillColor = Color.FromArgb(46, 204, 113);
            btnScheduleAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleAdd.ForeColor = Color.White;
            btnScheduleAdd.Location = new Point(502, 40);
            btnScheduleAdd.Name = "btnScheduleAdd";
            btnScheduleAdd.Size = new Size(90, 30);
            btnScheduleAdd.TabIndex = 7;
            btnScheduleAdd.Text = "Add";
            //
            // btnScheduleSearch
            //
            btnScheduleSearch.BorderRadius = 8;
            btnScheduleSearch.FillColor = Color.FromArgb(76, 132, 255);
            btnScheduleSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleSearch.ForeColor = Color.White;
            btnScheduleSearch.Location = new Point(406, 40);
            btnScheduleSearch.Name = "btnScheduleSearch";
            btnScheduleSearch.Size = new Size(90, 30);
            btnScheduleSearch.TabIndex = 6;
            btnScheduleSearch.Text = "Search";
            //
            // inputScheduleSearch
            //
            inputScheduleSearch.BorderRadius = 8;
            inputScheduleSearch.Location = new Point(328, 40);
            inputScheduleSearch.Name = "inputScheduleSearch";
            inputScheduleSearch.Size = new Size(172, 30);
            inputScheduleSearch.TabIndex = 5;
            // 
            // scheduleGrid
            // 
            scheduleGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            scheduleGrid.BackgroundColor = Color.White;
            scheduleGrid.BorderStyle = BorderStyle.None;
            scheduleGrid.ColumnHeadersHeight = 32;
            scheduleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            scheduleGrid.Location = new Point(19, 80);
            scheduleGrid.MultiSelect = false;
            scheduleGrid.Name = "scheduleGrid";
            scheduleGrid.RowHeadersVisible = false;
            scheduleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            scheduleGrid.Size = new Size(977, 510);
            scheduleGrid.TabIndex = 4;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(19, 40);
            label5.Name = "label5";
            label5.Size = new Size(105, 15);
            label5.TabIndex = 3;
            label5.Text = "Container schedules";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(19, 16);
            label4.Name = "label4";
            label4.Size = new Size(55, 15);
            label4.TabIndex = 2;
            label4.Text = "Overview";
            // 
            // lblContainerNote
            // 
            lblContainerNote.AutoSize = true;
            lblContainerNote.Location = new Point(228, 16);
            lblContainerNote.Name = "lblContainerNote";
            lblContainerNote.Size = new Size(34, 15);
            lblContainerNote.TabIndex = 1;
            lblContainerNote.Text = "Note";
            // 
            // lblContainerName
            // 
            lblContainerName.AutoSize = true;
            lblContainerName.Location = new Point(100, 16);
            lblContainerName.Name = "lblContainerName";
            lblContainerName.Size = new Size(39, 15);
            lblContainerName.TabIndex = 0;
            lblContainerName.Text = "Name";
            // 
            // tabScheduleEdit
            // 
            tabScheduleEdit.Controls.Add(btnScheduleCancel);
            tabScheduleEdit.Controls.Add(btnScheduleSave);
            tabScheduleEdit.Controls.Add(slotGrid);
            tabScheduleEdit.Controls.Add(btnGenerate);
            tabScheduleEdit.Controls.Add(checkedAvailabilities);
            tabScheduleEdit.Controls.Add(comboStatus);
            tabScheduleEdit.Controls.Add(label21);
            tabScheduleEdit.Controls.Add(comboShop);
            tabScheduleEdit.Controls.Add(inputScheduleComment);
            tabScheduleEdit.Controls.Add(label20);
            tabScheduleEdit.Controls.Add(inputMaxFull);
            tabScheduleEdit.Controls.Add(label19);
            tabScheduleEdit.Controls.Add(inputMaxConsecutiveFull);
            tabScheduleEdit.Controls.Add(label18);
            tabScheduleEdit.Controls.Add(inputMaxConsecutiveDays);
            tabScheduleEdit.Controls.Add(label17);
            tabScheduleEdit.Controls.Add(inputMaxHours);
            tabScheduleEdit.Controls.Add(label16);
            tabScheduleEdit.Controls.Add(inputShift2);
            tabScheduleEdit.Controls.Add(label15);
            tabScheduleEdit.Controls.Add(inputShift1);
            tabScheduleEdit.Controls.Add(label14);
            tabScheduleEdit.Controls.Add(inputPeoplePerShift);
            tabScheduleEdit.Controls.Add(label13);
            tabScheduleEdit.Controls.Add(inputMonth);
            tabScheduleEdit.Controls.Add(label12);
            tabScheduleEdit.Controls.Add(inputYear);
            tabScheduleEdit.Controls.Add(label11);
            tabScheduleEdit.Controls.Add(inputScheduleName);
            tabScheduleEdit.Controls.Add(label10);
            tabScheduleEdit.Controls.Add(numberScheduleId);
            tabScheduleEdit.Controls.Add(label9);
            tabScheduleEdit.Location = new Point(4, 24);
            tabScheduleEdit.Name = "tabScheduleEdit";
            tabScheduleEdit.Padding = new Padding(3);
            tabScheduleEdit.Size = new Size(1016, 612);
            tabScheduleEdit.TabIndex = 3;
            tabScheduleEdit.Text = "Schedule Edit";
            tabScheduleEdit.UseVisualStyleBackColor = true;
            // 
            // btnScheduleCancel
            // 
            btnScheduleCancel.BorderRadius = 8;
            btnScheduleCancel.FillColor = Color.FromArgb(231, 76, 60);
            btnScheduleCancel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleCancel.ForeColor = Color.White;
            btnScheduleCancel.Location = new Point(114, 566);
            btnScheduleCancel.Name = "btnScheduleCancel";
            btnScheduleCancel.Size = new Size(90, 30);
            btnScheduleCancel.TabIndex = 29;
            btnScheduleCancel.Text = "Cancel";
            //
            // btnScheduleSave
            //
            btnScheduleSave.BorderRadius = 8;
            btnScheduleSave.FillColor = Color.FromArgb(46, 204, 113);
            btnScheduleSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnScheduleSave.ForeColor = Color.White;
            btnScheduleSave.Location = new Point(33, 566);
            btnScheduleSave.Name = "btnScheduleSave";
            btnScheduleSave.Size = new Size(90, 30);
            btnScheduleSave.TabIndex = 28;
            btnScheduleSave.Text = "Save";
            // 
            // slotGrid
            // 
            slotGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            slotGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            slotGrid.BackgroundColor = Color.White;
            slotGrid.BorderStyle = BorderStyle.None;
            slotGrid.ColumnHeadersHeight = 32;
            slotGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            slotGrid.Location = new Point(368, 16);
            slotGrid.Name = "slotGrid";
            slotGrid.RowHeadersVisible = false;
            slotGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            slotGrid.Size = new Size(630, 536);
            slotGrid.TabIndex = 27;
            // 
            // btnGenerate
            // 
            btnGenerate.BorderRadius = 8;
            btnGenerate.FillColor = Color.FromArgb(76, 132, 255);
            btnGenerate.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnGenerate.ForeColor = Color.White;
            btnGenerate.Location = new Point(287, 509);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(90, 30);
            btnGenerate.TabIndex = 26;
            btnGenerate.Text = "Generate";
            // 
            // checkedAvailabilities
            // 
            checkedAvailabilities.BorderStyle = BorderStyle.FixedSingle;
            checkedAvailabilities.CheckOnClick = true;
            checkedAvailabilities.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            checkedAvailabilities.ItemHeight = 20;
            checkedAvailabilities.Location = new Point(17, 356);
            checkedAvailabilities.Name = "checkedAvailabilities";
            checkedAvailabilities.Size = new Size(345, 144);
            checkedAvailabilities.TabIndex = 25;
            // 
            // comboStatus
            // 
            comboStatus.BackColor = Color.Transparent;
            comboStatus.BorderRadius = 6;
            comboStatus.DrawMode = DrawMode.OwnerDrawFixed;
            comboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboStatus.FocusedColor = Color.FromArgb(94, 148, 255);
            comboStatus.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            comboStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            comboStatus.ForeColor = Color.FromArgb(68, 88, 112);
            comboStatus.ItemHeight = 30;
            comboStatus.Location = new Point(127, 327);
            comboStatus.Name = "comboStatus";
            comboStatus.Size = new Size(235, 36);
            comboStatus.TabIndex = 24;
            // 
            // comboShop
            //
            comboShop.BackColor = Color.Transparent;
            comboShop.BorderRadius = 6;
            comboShop.DrawMode = DrawMode.OwnerDrawFixed;
            comboShop.DropDownStyle = ComboBoxStyle.DropDownList;
            comboShop.FocusedColor = Color.FromArgb(94, 148, 255);
            comboShop.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
            comboShop.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            comboShop.ForeColor = Color.FromArgb(68, 88, 112);
            comboShop.ItemHeight = 30;
            comboShop.Location = new Point(127, 113);
            comboShop.Name = "comboShop";
            comboShop.Size = new Size(235, 36);
            comboShop.TabIndex = 8;
            // 
            // label21
            //
            label21.AutoSize = true;
            label21.Location = new Point(17, 116);
            label21.Name = "label21";
            label21.Size = new Size(33, 15);
            label21.TabIndex = 7;
            label21.Text = "Shop";
            // 
            // inputScheduleComment
            // 
            inputScheduleComment.BorderRadius = 8;
            inputScheduleComment.Location = new Point(127, 487);
            inputScheduleComment.Name = "inputScheduleComment";
            inputScheduleComment.Size = new Size(235, 30);
            inputScheduleComment.TabIndex = 23;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(17, 490);
            label20.Name = "label20";
            label20.Size = new Size(60, 15);
            label20.TabIndex = 22;
            label20.Text = "Comment";
            // 
            // inputMaxFull
            // 
            inputMaxFull.BorderRadius = 6;
            inputMaxFull.Location = new Point(127, 442);
            inputMaxFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxFull.Name = "inputMaxFull";
            inputMaxFull.Size = new Size(120, 30);
            inputMaxFull.TabIndex = 21;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(17, 444);
            label19.Name = "label19";
            label19.Size = new Size(96, 15);
            label19.TabIndex = 20;
            label19.Text = "Max full / month";
            // 
            // inputMaxConsecutiveFull
            // 
            inputMaxConsecutiveFull.BorderRadius = 6;
            inputMaxConsecutiveFull.Location = new Point(127, 395);
            inputMaxConsecutiveFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveFull.Name = "inputMaxConsecutiveFull";
            inputMaxConsecutiveFull.Size = new Size(120, 30);
            inputMaxConsecutiveFull.TabIndex = 19;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(17, 397);
            label18.Name = "label18";
            label18.Size = new Size(101, 15);
            label18.TabIndex = 18;
            label18.Text = "Max consecutive full";
            // 
            // inputMaxConsecutiveDays
            // 
            inputMaxConsecutiveDays.BorderRadius = 6;
            inputMaxConsecutiveDays.Location = new Point(127, 348);
            inputMaxConsecutiveDays.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveDays.Name = "inputMaxConsecutiveDays";
            inputMaxConsecutiveDays.Size = new Size(120, 30);
            inputMaxConsecutiveDays.TabIndex = 17;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(17, 350);
            label17.Name = "label17";
            label17.Size = new Size(109, 15);
            label17.TabIndex = 16;
            label17.Text = "Max consecutive day";
            // 
            // inputMaxHours
            // 
            inputMaxHours.BorderRadius = 6;
            inputMaxHours.Location = new Point(127, 301);
            inputMaxHours.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxHours.Name = "inputMaxHours";
            inputMaxHours.Size = new Size(120, 30);
            inputMaxHours.TabIndex = 15;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(17, 303);
            label16.Name = "label16";
            label16.Size = new Size(89, 15);
            label16.TabIndex = 14;
            label16.Text = "Max hours emp";
            // 
            // inputShift2
            // 
            inputShift2.BorderRadius = 8;
            inputShift2.Location = new Point(127, 254);
            inputShift2.Name = "inputShift2";
            inputShift2.Size = new Size(235, 30);
            inputShift2.TabIndex = 13;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(17, 257);
            label15.Name = "label15";
            label15.Size = new Size(44, 15);
            label15.TabIndex = 12;
            label15.Text = "Shift 2";
            // 
            // inputShift1
            // 
            inputShift1.BorderRadius = 8;
            inputShift1.Location = new Point(127, 207);
            inputShift1.Name = "inputShift1";
            inputShift1.Size = new Size(235, 30);
            inputShift1.TabIndex = 11;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(17, 210);
            label14.Name = "label14";
            label14.Size = new Size(44, 15);
            label14.TabIndex = 10;
            label14.Text = "Shift 1";
            // 
            // inputPeoplePerShift
            // 
            inputPeoplePerShift.BorderRadius = 6;
            inputPeoplePerShift.Location = new Point(127, 160);
            inputPeoplePerShift.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputPeoplePerShift.Name = "inputPeoplePerShift";
            inputPeoplePerShift.Size = new Size(120, 30);
            inputPeoplePerShift.TabIndex = 9;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(17, 162);
            label13.Name = "label13";
            label13.Size = new Size(98, 15);
            label13.TabIndex = 8;
            label13.Text = "People per shift";
            // 
            // inputMonth
            // 
            inputMonth.BorderRadius = 6;
            inputMonth.Location = new Point(289, 64);
            inputMonth.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
            inputMonth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            inputMonth.Name = "inputMonth";
            inputMonth.Size = new Size(73, 30);
            inputMonth.TabIndex = 7;
            inputMonth.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(241, 66);
            label12.Name = "label12";
            label12.Size = new Size(42, 15);
            label12.TabIndex = 6;
            label12.Text = "Month";
            // 
            // inputYear
            // 
            inputYear.BorderRadius = 6;
            inputYear.Location = new Point(127, 64);
            inputYear.Maximum = new decimal(new int[] { 4000, 0, 0, 0 });
            inputYear.Minimum = new decimal(new int[] { 1900, 0, 0, 0 });
            inputYear.Name = "inputYear";
            inputYear.Size = new Size(108, 30);
            inputYear.TabIndex = 5;
            inputYear.Value = new decimal(new int[] { 2024, 0, 0, 0 });
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(17, 66);
            label11.Name = "label11";
            label11.Size = new Size(30, 15);
            label11.TabIndex = 4;
            label11.Text = "Year";
            // 
            // inputScheduleName
            // 
            inputScheduleName.BorderRadius = 8;
            inputScheduleName.Location = new Point(127, 35);
            inputScheduleName.Name = "inputScheduleName";
            inputScheduleName.Size = new Size(235, 30);
            inputScheduleName.TabIndex = 3;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(17, 37);
            label10.Name = "label10";
            label10.Size = new Size(88, 15);
            label10.TabIndex = 2;
            label10.Text = "Schedule name";
            // 
            // numberScheduleId
            // 
            numberScheduleId.BorderRadius = 6;
            numberScheduleId.Location = new Point(127, 6);
            numberScheduleId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberScheduleId.Name = "numberScheduleId";
            numberScheduleId.Size = new Size(120, 30);
            numberScheduleId.TabIndex = 1;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(17, 8);
            label9.Name = "label9";
            label9.Size = new Size(76, 15);
            label9.TabIndex = 0;
            label9.Text = "Schedule ID";
            // 
            // tabScheduleProfile
            // 
            tabScheduleProfile.Controls.Add(scheduleSlotProfileGrid);
            tabScheduleProfile.Controls.Add(lblScheduleSummary);
            tabScheduleProfile.Location = new Point(4, 24);
            tabScheduleProfile.Name = "tabScheduleProfile";
            tabScheduleProfile.Padding = new Padding(3);
            tabScheduleProfile.Size = new Size(1016, 612);
            tabScheduleProfile.TabIndex = 4;
            tabScheduleProfile.Text = "Schedule Profile";
            tabScheduleProfile.UseVisualStyleBackColor = true;
            // 
            // scheduleSlotProfileGrid
            // 
            scheduleSlotProfileGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleSlotProfileGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            scheduleSlotProfileGrid.BackgroundColor = Color.White;
            scheduleSlotProfileGrid.BorderStyle = BorderStyle.None;
            scheduleSlotProfileGrid.ColumnHeadersHeight = 32;
            scheduleSlotProfileGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            scheduleSlotProfileGrid.Location = new Point(18, 42);
            scheduleSlotProfileGrid.Name = "scheduleSlotProfileGrid";
            scheduleSlotProfileGrid.RowHeadersVisible = false;
            scheduleSlotProfileGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            scheduleSlotProfileGrid.Size = new Size(978, 552);
            scheduleSlotProfileGrid.TabIndex = 1;
            // 
            // lblScheduleSummary
            // 
            lblScheduleSummary.AutoSize = true;
            lblScheduleSummary.Location = new Point(18, 15);
            lblScheduleSummary.Name = "lblScheduleSummary";
            lblScheduleSummary.Size = new Size(105, 15);
            lblScheduleSummary.TabIndex = 0;
            lblScheduleSummary.Text = "Schedule summary";
            // 
            // errorProviderContainer
            // 
            errorProviderContainer.ContainerControl = this;
            // 
            // errorProviderSchedule
            // 
            errorProviderSchedule.ContainerControl = this;
            // 
            // ContainerView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1024, 640);
            Controls.Add(tabControl);
            Name = "ContainerView";
            Text = "Container";
            tabControl.ResumeLayout(false);
            tabList.ResumeLayout(false);
            tabList.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)containerGrid).EndInit();
            tabEdit.ResumeLayout(false);
            tabEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numberContainerId).EndInit();
            tabProfile.ResumeLayout(false);
            tabProfile.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)scheduleGrid).EndInit();
            tabScheduleEdit.ResumeLayout(false);
            tabScheduleEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)slotGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxFull).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxConsecutiveFull).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxConsecutiveDays).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputMaxHours).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputPeoplePerShift).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputMonth).EndInit();
            ((System.ComponentModel.ISupportInitialize)inputYear).EndInit();
            ((System.ComponentModel.ISupportInitialize)numberScheduleId).EndInit();
            tabScheduleProfile.ResumeLayout(false);
            tabScheduleProfile.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)scheduleSlotProfileGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProviderContainer).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProviderSchedule).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2TabControl tabControl;
        private TabPage tabList;
        private TabPage tabEdit;
        private TabPage tabProfile;
        private TabPage tabScheduleEdit;
        private TabPage tabScheduleProfile;
        private Guna.UI2.WinForms.Guna2DataGridView containerGrid;
        private Guna.UI2.WinForms.Guna2Button btnDelete;
        private Guna.UI2.WinForms.Guna2Button btnEdit;
        private Guna.UI2.WinForms.Guna2Button btnAdd;
        private Guna.UI2.WinForms.Guna2Button btnSearch;
        private Guna.UI2.WinForms.Guna2TextBox inputSearch;
        private Guna.UI2.WinForms.Guna2Button btnCancel;
        private Guna.UI2.WinForms.Guna2Button btnSave;
        private Guna.UI2.WinForms.Guna2TextBox inputContainerNote;
        private Label label3;
        private Guna.UI2.WinForms.Guna2TextBox inputContainerName;
        private Label label2;
        private Guna.UI2.WinForms.Guna2NumericUpDown numberContainerId;
        private Label label1;
        private Label lblContainerNote;
        private Label lblContainerName;
        private Label label5;
        private Label label4;
        private Guna.UI2.WinForms.Guna2DataGridView scheduleGrid;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleSearch;
        private Guna.UI2.WinForms.Guna2Button btnScheduleSearch;
        private Guna.UI2.WinForms.Guna2Button btnScheduleAdd;
        private Guna.UI2.WinForms.Guna2Button btnScheduleDelete;
        private Guna.UI2.WinForms.Guna2Button btnScheduleEdit;
        private Guna.UI2.WinForms.Guna2Button btnOpenScheduleProfile;
        private Label label9;
        private Guna.UI2.WinForms.Guna2NumericUpDown numberScheduleId;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleName;
        private Label label10;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputYear;
        private Label label11;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMonth;
        private Label label12;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputPeoplePerShift;
        private Label label13;
        private Label label21;
        private Guna.UI2.WinForms.Guna2TextBox inputShift1;
        private Label label14;
        private Guna.UI2.WinForms.Guna2TextBox inputShift2;
        private Label label15;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxHours;
        private Label label16;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxConsecutiveDays;
        private Label label17;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxConsecutiveFull;
        private Label label18;
        private Guna.UI2.WinForms.Guna2NumericUpDown inputMaxFull;
        private Label label19;
        private Guna.UI2.WinForms.Guna2TextBox inputScheduleComment;
        private Label label20;
        private Guna.UI2.WinForms.Guna2ComboBox comboShop;
        private Guna.UI2.WinForms.Guna2ComboBox comboStatus;
        private Guna.UI2.WinForms.Guna2CheckedListBox checkedAvailabilities;
        private Guna.UI2.WinForms.Guna2Button btnGenerate;
        private Guna.UI2.WinForms.Guna2DataGridView slotGrid;
        private Guna.UI2.WinForms.Guna2Button btnScheduleSave;
        private Guna.UI2.WinForms.Guna2Button btnScheduleCancel;
        private Label lblScheduleSummary;
        private Guna.UI2.WinForms.Guna2DataGridView scheduleSlotProfileGrid;
        private ErrorProvider errorProviderContainer;
        private ErrorProvider errorProviderSchedule;
    }
}
