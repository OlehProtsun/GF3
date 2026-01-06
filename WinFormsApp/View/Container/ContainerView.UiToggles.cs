using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private const int ScheduleSectionGap = 10; // відступ між картами (підбери під себе)
        private Panel? _gapInfoEmployee;
        private Panel? _gapEmployeeNote;
        private bool _updatingPanel3Padding;
        private const int PanelSidePadding = 10; // як у тебе зараз
        private const int PanelTopBottomPadding = 10;

        private static Panel EnsureGapPanel(ref Panel? p)
        {
            if (p != null) return p;

            p = new Panel
            {
                Dock = DockStyle.Top,
                Height = ScheduleSectionGap,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            return p;
        }

        // Named handlers -> можемо безпечно робити "-=" перед "+="
        private void BtnShowHideInfo_Click(object? sender, EventArgs e) => ToggleScheduleInfo();
        private void BtnShowHideNote_Click(object? sender, EventArgs e) => ToggleScheduleNote();
        private void BtnShowHideEmployee_Click(object? sender, EventArgs e) => ToggleScheduleEmployee();

        private void ToggleScheduleInfo()
        {
            EnsureScheduleEditTogglesInitialized();
            _scheduleInfoExpanded = !_scheduleInfoExpanded;
            ApplyScheduleInfoState(_scheduleInfoExpanded);
        }

        private void ToggleScheduleNote()
        {
            EnsureScheduleEditTogglesInitialized();
            _scheduleNoteExpanded = !_scheduleNoteExpanded;
            ApplyScheduleNoteState(_scheduleNoteExpanded);
        }

        private void ToggleScheduleEmployee()
        {
            EnsureScheduleEditTogglesInitialized();
            _scheduleEmployeeExpanded = !_scheduleEmployeeExpanded;
            ApplyScheduleEmployeeState(_scheduleEmployeeExpanded);
        }

        /// <summary>
        /// Найпростіша, стабільна верстка:
        /// - Info завжди зверху
        /// - Employee під нею
        /// - Note під Employee
        /// - позиції не рухаємо вручну (ніяких Top = ...)
        /// </summary>
        private void SetupScheduleLeftColumnDockLayout()
        {
            panel3.SuspendLayout();
            try
            {
                // ✅ Резервуємо місце під вертикальний скролбар, щоб ширина НЕ мінялась
                // (контент завжди буде з однаковою шириною)
                panel3.Padding = new Padding(PanelSidePadding, PanelTopBottomPadding, PanelSidePadding, PanelTopBottomPadding);
                // Dock "магніт"
                guna2GroupBox5.Dock = DockStyle.Top;   // Info
                guna2GroupBox19.Dock = DockStyle.Top;  // Employee
                guna2GroupBox16.Dock = DockStyle.Top;  // Note

                // spacer-панелі для відступів
                var gap1 = EnsureGapPanel(ref _gapInfoEmployee);  // між Info та Employee
                var gap2 = EnsureGapPanel(ref _gapEmployeeNote);  // між Employee та Note

                // ✅ Найнадійніше — зібрати порядок заново
                panel3.Controls.Clear();

                // Dock=Top рахується знизу догори (reverse order), тому додаємо так:
                panel3.Controls.Add(guna2GroupBox16); // Note (нижче)
                panel3.Controls.Add(gap2);
                panel3.Controls.Add(guna2GroupBox19); // Employee (середина)
                panel3.Controls.Add(gap1);
                panel3.Controls.Add(guna2GroupBox5);  // Info (завжди зверху)

                panel3.PerformLayout();
            }
            finally
            {
                panel3.ResumeLayout(true);
            }
        }

        private void ApplyScheduleInfoState(bool expanded)
        {
            // ВАЖЛИВО: Suspend/Resume саме panel3 (бо groupbox-и в panel3)
            panel3.SuspendLayout();
            try
            {
                guna2GroupBox5.Height = expanded ? _scheduleInfoExpandedHeight : ScheduleInfoCollapsedHeight;

                foreach (Control c in guna2GroupBox5.Controls)
                {
                    if (ReferenceEquals(c, btnShowHideInfo) || ReferenceEquals(c, guna2Button6))
                        continue;

                    c.Visible = expanded;
                    c.TabStop = expanded;
                }

                btnShowHideInfo.Text = expanded ? "Hide" : "Show";
            }
            finally
            {
                panel3.ResumeLayout(true);
            }
        }

        private void ApplyScheduleNoteState(bool expanded)
        {
            panel3.SuspendLayout();
            try
            {
                guna2GroupBox16.Height = expanded ? _scheduleNoteExpandedHeight : ScheduleInfoCollapsedHeight;

                foreach (Control c in guna2GroupBox16.Controls)
                {
                    if (ReferenceEquals(c, btnShowHideNote) || ReferenceEquals(c, guna2Button29))
                        continue;

                    c.Visible = expanded;
                    c.TabStop = expanded;
                }

                btnShowHideNote.Text = expanded ? "Hide" : "Show";
            }
            finally
            {
                panel3.ResumeLayout(true);
            }
        }

        private void ApplyScheduleEmployeeState(bool expanded)
        {
            panel3.SuspendLayout();
            try
            {
                guna2GroupBox19.Height = expanded ? _scheduleEmployeeExpandedHeight : ScheduleInfoCollapsedHeight;

                foreach (Control c in guna2GroupBox19.Controls)
                {
                    if (ReferenceEquals(c, btnShowHideEmployee) || ReferenceEquals(c, guna2Button28))
                        continue;

                    c.Visible = expanded;
                    c.TabStop = expanded;
                }

                btnShowHideEmployee.Text = expanded ? "Hide" : "Show";
            }
            finally
            {
                panel3.ResumeLayout(true);
            }
        }

        private void EnsureScheduleEditTogglesInitialized()
        {
            if (_scheduleEditTogglesInitialized) return;
            _scheduleEditTogglesInitialized = true;

            InitScheduleEditToggles();
        }

        private void InitScheduleEditToggles()
        {
            // Запам’ятовуємо “розгорнуті” висоти з дизайнера
            _scheduleInfoExpandedHeight = guna2GroupBox5.Height;
            _scheduleNoteExpandedHeight = guna2GroupBox16.Height;
            _scheduleEmployeeExpandedHeight = guna2GroupBox19.Height;

            // Вмикаємо "магнітну" верстку один раз
            SetupScheduleLeftColumnDockLayout();

            panel3.Layout -= Panel3_Layout;
            panel3.Layout += Panel3_Layout;

            panel3.SizeChanged -= Panel3_SizeChanged;
            panel3.SizeChanged += Panel3_SizeChanged;

            // одразу синхронізуємо
            UpdatePanel3RightPadding();

            // НІЯКОГО AutoScrollMinSize — воно якраз часто створює "дірку" зверху
            // panel3.AutoScrollMinSize = ...

            // Ідемпотентні підписки
            btnShowHideInfo.Click -= BtnShowHideInfo_Click;
            btnShowHideInfo.Click += BtnShowHideInfo_Click;

            btnShowHideNote.Click -= BtnShowHideNote_Click;
            btnShowHideNote.Click += BtnShowHideNote_Click;

            btnShowHideEmployee.Click -= BtnShowHideEmployee_Click;
            btnShowHideEmployee.Click += BtnShowHideEmployee_Click;

            // Синхронізуємо UI зі станами
            ApplyScheduleInfoState(_scheduleInfoExpanded);
            ApplyScheduleEmployeeState(_scheduleEmployeeExpanded);
            ApplyScheduleNoteState(_scheduleNoteExpanded);
        }

        private void UpdatePanel3RightPadding()
        {
            if (_updatingPanel3Padding) return;
            _updatingPanel3Padding = true;

            try
            {
                // ВАЖЛИВО: Visible стає коректним після layout
                bool vScrollVisible = panel3.VerticalScroll.Visible;
                int right = vScrollVisible ? PanelSidePadding : PanelSidePadding + SystemInformation.VerticalScrollBarWidth;

                var p = panel3.Padding;
                if (p.Left != PanelSidePadding || p.Top != PanelTopBottomPadding || p.Bottom != PanelTopBottomPadding || p.Right != right)
                {
                    panel3.Padding = new Padding(PanelSidePadding, PanelTopBottomPadding, right, PanelTopBottomPadding);
                }
            }
            finally
            {
                _updatingPanel3Padding = false;
            }
        }

        private void Panel3_Layout(object? sender, LayoutEventArgs e)
        {
            // після перерахунку AutoScroll стає відомо, чи є скролбар
            if (panel3.IsHandleCreated)
                panel3.BeginInvoke(new Action(UpdatePanel3RightPadding));
        }

        private void Panel3_SizeChanged(object? sender, EventArgs e)
        {
            UpdatePanel3RightPadding();
        }

    }
}
