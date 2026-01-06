using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        // Named handlers -> можемо безпечно робити "-=" перед "+="
        private void BtnShowHideInfo_Click(object? sender, EventArgs e) => ToggleScheduleInfo();
        private void BtnShowHideNote_Click(object? sender, EventArgs e) => ToggleScheduleNote();
        private void BtnShowHideEmployee_Click(object? sender, EventArgs e) => ToggleScheduleEmployee();

        private void ToggleScheduleInfo()
        {
            _scheduleInfoExpanded = !_scheduleInfoExpanded;
            ApplyScheduleInfoState(_scheduleInfoExpanded);
        }

        private void ToggleScheduleNote()
        {
            _scheduleNoteExpanded = !_scheduleNoteExpanded;
            ApplyScheduleNoteState(_scheduleNoteExpanded);
        }

        private void ToggleScheduleEmployee()
        {
            _scheduleEmployeeExpanded = !_scheduleEmployeeExpanded;
            ApplyScheduleEmployeeState(_scheduleEmployeeExpanded);
        }

        private void ApplyScheduleInfoState(bool expanded)
        {
            panel1.SuspendLayout();
            try
            {
                guna2GroupBox5.Height = expanded ? _scheduleInfoExpandedHeight : ScheduleInfoCollapsedHeight;

                foreach (Control c in guna2GroupBox5.Controls)
                {
                    // залишаємо кнопки керування видимими завжди
                    if (ReferenceEquals(c, btnShowHideInfo) || ReferenceEquals(c, guna2Button6))
                        continue;

                    c.Visible = expanded;
                    c.TabStop = expanded;
                }

                btnShowHideInfo.Text = expanded ? "Hide" : "Show";

                RelayoutScheduleEditLeftColumn();
            }
            finally
            {
                panel1.ResumeLayout(true);
            }
        }

        private void ApplyScheduleNoteState(bool expanded)
        {
            panel1.SuspendLayout();
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

                // Note йде під Info, тому перерахунок потрібен і тут
                RelayoutScheduleEditLeftColumn();
            }
            finally
            {
                panel1.ResumeLayout(true);
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

                RelayoutScheduleEditLeftColumn();
            }
            finally
            {
                panel3.ResumeLayout(true);
            }
        }

        private void RelayoutScheduleEditLeftColumn()
        {
            // Info завжди на дизайнерському місці
            guna2GroupBox5.Top = _scheduleLeftColumnTop;

            // Note завжди одразу під Info з дизайнерським відступом
            guna2GroupBox16.Top = guna2GroupBox5.Top + guna2GroupBox5.Height + _scheduleLeftColumnGap;

            // Employee під Note
            guna2GroupBox19.Top = guna2GroupBox16.Top + guna2GroupBox16.Height + _scheduleEmployeeColumnGap;
        }

        private void EnsureScheduleEditTogglesInitialized()
        {
            if (_scheduleEditTogglesInitialized) return;
            _scheduleEditTogglesInitialized = true;

            InitScheduleEditToggles();
        }

        private void InitScheduleEditToggles()
        {
            // Пам’ятаємо “розгорнуті” висоти з дизайнера
            _scheduleInfoExpandedHeight = guna2GroupBox5.Height;
            _scheduleNoteExpandedHeight = guna2GroupBox16.Height;
            _scheduleEmployeeExpandedHeight = guna2GroupBox19.Height;

            // Пам’ятаємо базову геометрію з дизайнера
            _scheduleLeftColumnTop = guna2GroupBox5.Top;
            _scheduleLeftColumnGap = guna2GroupBox16.Top - guna2GroupBox5.Bottom;
            _scheduleEmployeeColumnGap = guna2GroupBox19.Top - guna2GroupBox16.Bottom;

            // Ідемпотентні підписки (щоб не було 2х викликів)
            btnShowHideInfo.Click -= BtnShowHideInfo_Click;
            btnShowHideInfo.Click += BtnShowHideInfo_Click;

            btnShowHideNote.Click -= BtnShowHideNote_Click;
            btnShowHideNote.Click += BtnShowHideNote_Click;

            btnShowHideEmployee.Click -= BtnShowHideEmployee_Click;
            btnShowHideEmployee.Click += BtnShowHideEmployee_Click;

            // Синхронізуємо UI зі станами
            ApplyScheduleInfoState(_scheduleInfoExpanded);
            ApplyScheduleNoteState(_scheduleNoteExpanded);
            ApplyScheduleEmployeeState(_scheduleEmployeeExpanded);
        }
    }
}
