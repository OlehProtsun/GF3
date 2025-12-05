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
            tabControl = new TabControl();
            tabList = new TabPage();
            btnDelete = new Button();
            btnEdit = new Button();
            btnAdd = new Button();
            btnSearch = new Button();
            inputSearch = new TextBox();
            containerGrid = new DataGridView();
            tabEdit = new TabPage();
            btnCancel = new Button();
            btnSave = new Button();
            inputContainerNote = new TextBox();
            label3 = new Label();
            inputContainerName = new TextBox();
            label2 = new Label();
            numberContainerId = new NumericUpDown();
            label1 = new Label();
            tabProfile = new TabPage();
            btnOpenScheduleProfile = new Button();
            btnScheduleDelete = new Button();
            btnScheduleEdit = new Button();
            btnScheduleAdd = new Button();
            btnScheduleSearch = new Button();
            inputScheduleSearch = new TextBox();
            scheduleGrid = new DataGridView();
            label5 = new Label();
            label4 = new Label();
            lblContainerNote = new Label();
            lblContainerName = new Label();
            tabScheduleEdit = new TabPage();
            btnScheduleCancel = new Button();
            btnScheduleSave = new Button();
            slotGrid = new DataGridView();
            btnGenerate = new Button();
            checkedAvailabilities = new CheckedListBox();
            comboStatus = new ComboBox();
            label21 = new Label();
            comboShop = new ComboBox();
            inputScheduleComment = new TextBox();
            label20 = new Label();
            inputMaxFull = new NumericUpDown();
            label19 = new Label();
            inputMaxConsecutiveFull = new NumericUpDown();
            label18 = new Label();
            inputMaxConsecutiveDays = new NumericUpDown();
            label17 = new Label();
            inputMaxHours = new NumericUpDown();
            label16 = new Label();
            inputShift2 = new TextBox();
            label15 = new Label();
            inputShift1 = new TextBox();
            label14 = new Label();
            inputPeoplePerShift = new NumericUpDown();
            label13 = new Label();
            inputMonth = new NumericUpDown();
            label12 = new Label();
            inputYear = new NumericUpDown();
            label11 = new Label();
            inputScheduleName = new TextBox();
            label10 = new Label();
            numberScheduleId = new NumericUpDown();
            label9 = new Label();
            tabScheduleProfile = new TabPage();
            scheduleSlotProfileGrid = new DataGridView();
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
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1024, 640);
            tabControl.TabIndex = 0;
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
            btnDelete.Location = new Point(374, 16);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(75, 23);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(293, 16);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(75, 23);
            btnEdit.TabIndex = 4;
            btnEdit.Text = "Edit";
            btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(212, 16);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(75, 23);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(131, 16);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // inputSearch
            // 
            inputSearch.Location = new Point(8, 16);
            inputSearch.Name = "inputSearch";
            inputSearch.Size = new Size(117, 23);
            inputSearch.TabIndex = 1;
            // 
            // containerGrid
            // 
            containerGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            containerGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            containerGrid.Location = new Point(8, 56);
            containerGrid.MultiSelect = false;
            containerGrid.Name = "containerGrid";
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
            btnCancel.Location = new Point(104, 200);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(23, 200);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // inputContainerNote
            // 
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
            inputContainerName.Location = new Point(23, 78);
            inputContainerName.Name = "inputContainerName";
            inputContainerName.Size = new Size(312, 23);
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
            numberContainerId.Location = new Point(23, 34);
            numberContainerId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberContainerId.Name = "numberContainerId";
            numberContainerId.Size = new Size(120, 23);
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
            btnOpenScheduleProfile.Location = new Point(775, 40);
            btnOpenScheduleProfile.Name = "btnOpenScheduleProfile";
            btnOpenScheduleProfile.Size = new Size(104, 23);
            btnOpenScheduleProfile.TabIndex = 10;
            btnOpenScheduleProfile.Text = "Open profile";
            btnOpenScheduleProfile.UseVisualStyleBackColor = true;
            // 
            // btnScheduleDelete
            // 
            btnScheduleDelete.Location = new Point(694, 40);
            btnScheduleDelete.Name = "btnScheduleDelete";
            btnScheduleDelete.Size = new Size(75, 23);
            btnScheduleDelete.TabIndex = 9;
            btnScheduleDelete.Text = "Delete";
            btnScheduleDelete.UseVisualStyleBackColor = true;
            // 
            // btnScheduleEdit
            // 
            btnScheduleEdit.Location = new Point(613, 40);
            btnScheduleEdit.Name = "btnScheduleEdit";
            btnScheduleEdit.Size = new Size(75, 23);
            btnScheduleEdit.TabIndex = 8;
            btnScheduleEdit.Text = "Edit";
            btnScheduleEdit.UseVisualStyleBackColor = true;
            // 
            // btnScheduleAdd
            // 
            btnScheduleAdd.Location = new Point(532, 40);
            btnScheduleAdd.Name = "btnScheduleAdd";
            btnScheduleAdd.Size = new Size(75, 23);
            btnScheduleAdd.TabIndex = 7;
            btnScheduleAdd.Text = "Add";
            btnScheduleAdd.UseVisualStyleBackColor = true;
            // 
            // btnScheduleSearch
            // 
            btnScheduleSearch.Location = new Point(451, 40);
            btnScheduleSearch.Name = "btnScheduleSearch";
            btnScheduleSearch.Size = new Size(75, 23);
            btnScheduleSearch.TabIndex = 6;
            btnScheduleSearch.Text = "Search";
            btnScheduleSearch.UseVisualStyleBackColor = true;
            // 
            // inputScheduleSearch
            // 
            inputScheduleSearch.Location = new Point(328, 40);
            inputScheduleSearch.Name = "inputScheduleSearch";
            inputScheduleSearch.Size = new Size(117, 23);
            inputScheduleSearch.TabIndex = 5;
            // 
            // scheduleGrid
            // 
            scheduleGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            scheduleGrid.Location = new Point(19, 80);
            scheduleGrid.MultiSelect = false;
            scheduleGrid.Name = "scheduleGrid";
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
            btnScheduleCancel.Location = new Point(114, 566);
            btnScheduleCancel.Name = "btnScheduleCancel";
            btnScheduleCancel.Size = new Size(75, 23);
            btnScheduleCancel.TabIndex = 29;
            btnScheduleCancel.Text = "Cancel";
            btnScheduleCancel.UseVisualStyleBackColor = true;
            // 
            // btnScheduleSave
            // 
            btnScheduleSave.Location = new Point(33, 566);
            btnScheduleSave.Name = "btnScheduleSave";
            btnScheduleSave.Size = new Size(75, 23);
            btnScheduleSave.TabIndex = 28;
            btnScheduleSave.Text = "Save";
            btnScheduleSave.UseVisualStyleBackColor = true;
            // 
            // slotGrid
            // 
            slotGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            slotGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            slotGrid.Location = new Point(368, 16);
            slotGrid.Name = "slotGrid";
            slotGrid.Size = new Size(630, 536);
            slotGrid.TabIndex = 27;
            // 
            // btnGenerate
            // 
            btnGenerate.Location = new Point(287, 509);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(75, 23);
            btnGenerate.TabIndex = 26;
            btnGenerate.Text = "Generate";
            btnGenerate.UseVisualStyleBackColor = true;
            // 
            // checkedAvailabilities
            // 
            checkedAvailabilities.FormattingEnabled = true;
            checkedAvailabilities.Location = new Point(17, 356);
            checkedAvailabilities.Name = "checkedAvailabilities";
            checkedAvailabilities.Size = new Size(345, 148);
            checkedAvailabilities.TabIndex = 25;
            // 
            // comboStatus
            // 
            comboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboStatus.FormattingEnabled = true;
            comboStatus.Location = new Point(127, 327);
            comboStatus.Name = "comboStatus";
            comboStatus.Size = new Size(235, 23);
            comboStatus.TabIndex = 24;
            // 
            // comboShop
            //
            comboShop.DropDownStyle = ComboBoxStyle.DropDownList;
            comboShop.FormattingEnabled = true;
            comboShop.Location = new Point(127, 113);
            comboShop.Name = "comboShop";
            comboShop.Size = new Size(235, 23);
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
            inputScheduleComment.Location = new Point(127, 487);
            inputScheduleComment.Name = "inputScheduleComment";
            inputScheduleComment.Size = new Size(235, 23);
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
            inputMaxFull.Location = new Point(127, 442);
            inputMaxFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxFull.Name = "inputMaxFull";
            inputMaxFull.Size = new Size(120, 23);
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
            inputMaxConsecutiveFull.Location = new Point(127, 395);
            inputMaxConsecutiveFull.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveFull.Name = "inputMaxConsecutiveFull";
            inputMaxConsecutiveFull.Size = new Size(120, 23);
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
            inputMaxConsecutiveDays.Location = new Point(127, 348);
            inputMaxConsecutiveDays.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxConsecutiveDays.Name = "inputMaxConsecutiveDays";
            inputMaxConsecutiveDays.Size = new Size(120, 23);
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
            inputMaxHours.Location = new Point(127, 301);
            inputMaxHours.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputMaxHours.Name = "inputMaxHours";
            inputMaxHours.Size = new Size(120, 23);
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
            inputShift2.Location = new Point(127, 254);
            inputShift2.Name = "inputShift2";
            inputShift2.Size = new Size(235, 23);
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
            inputShift1.Location = new Point(127, 207);
            inputShift1.Name = "inputShift1";
            inputShift1.Size = new Size(235, 23);
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
            inputPeoplePerShift.Location = new Point(127, 160);
            inputPeoplePerShift.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            inputPeoplePerShift.Name = "inputPeoplePerShift";
            inputPeoplePerShift.Size = new Size(120, 23);
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
            inputMonth.Location = new Point(289, 64);
            inputMonth.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
            inputMonth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            inputMonth.Name = "inputMonth";
            inputMonth.Size = new Size(73, 23);
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
            inputYear.Location = new Point(127, 64);
            inputYear.Maximum = new decimal(new int[] { 4000, 0, 0, 0 });
            inputYear.Minimum = new decimal(new int[] { 1900, 0, 0, 0 });
            inputYear.Name = "inputYear";
            inputYear.Size = new Size(108, 23);
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
            inputScheduleName.Location = new Point(127, 35);
            inputScheduleName.Name = "inputScheduleName";
            inputScheduleName.Size = new Size(235, 23);
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
            numberScheduleId.Location = new Point(127, 6);
            numberScheduleId.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numberScheduleId.Name = "numberScheduleId";
            numberScheduleId.Size = new Size(120, 23);
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
            scheduleSlotProfileGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            scheduleSlotProfileGrid.Location = new Point(18, 42);
            scheduleSlotProfileGrid.Name = "scheduleSlotProfileGrid";
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

        private TabControl tabControl;
        private TabPage tabList;
        private TabPage tabEdit;
        private TabPage tabProfile;
        private TabPage tabScheduleEdit;
        private TabPage tabScheduleProfile;
        private DataGridView containerGrid;
        private Button btnDelete;
        private Button btnEdit;
        private Button btnAdd;
        private Button btnSearch;
        private TextBox inputSearch;
        private Button btnCancel;
        private Button btnSave;
        private TextBox inputContainerNote;
        private Label label3;
        private TextBox inputContainerName;
        private Label label2;
        private NumericUpDown numberContainerId;
        private Label label1;
        private Label lblContainerNote;
        private Label lblContainerName;
        private Label label5;
        private Label label4;
        private DataGridView scheduleGrid;
        private TextBox inputScheduleSearch;
        private Button btnScheduleSearch;
        private Button btnScheduleAdd;
        private Button btnScheduleDelete;
        private Button btnScheduleEdit;
        private Button btnOpenScheduleProfile;
        private Label label9;
        private NumericUpDown numberScheduleId;
        private TextBox inputScheduleName;
        private Label label10;
        private NumericUpDown inputYear;
        private Label label11;
        private NumericUpDown inputMonth;
        private Label label12;
        private NumericUpDown inputPeoplePerShift;
        private Label label13;
        private Label label21;
        private TextBox inputShift1;
        private Label label14;
        private TextBox inputShift2;
        private Label label15;
        private NumericUpDown inputMaxHours;
        private Label label16;
        private NumericUpDown inputMaxConsecutiveDays;
        private Label label17;
        private NumericUpDown inputMaxConsecutiveFull;
        private Label label18;
        private NumericUpDown inputMaxFull;
        private Label label19;
        private TextBox inputScheduleComment;
        private Label label20;
        private ComboBox comboShop;
        private ComboBox comboStatus;
        private CheckedListBox checkedAvailabilities;
        private Button btnGenerate;
        private DataGridView slotGrid;
        private Button btnScheduleSave;
        private Button btnScheduleCancel;
        private Label lblScheduleSummary;
        private DataGridView scheduleSlotProfileGrid;
        private ErrorProvider errorProviderContainer;
        private ErrorProvider errorProviderSchedule;
    }
}
