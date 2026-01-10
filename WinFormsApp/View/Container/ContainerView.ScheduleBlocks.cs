using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private bool _scheduleBlockMetricsInit;
        private int _minScheduleGroupW, _minScheduleGroupH, _scheduleSidePad, _scheduleBottomPad;
        private int _minAvailGroupW, _minAvailGroupH, _availSidePad, _availBottomPad;

        public void InitializeScheduleBlocks()
        {
            _scheduleBlocksInitialized = true;
            _scheduleBlocks.Clear();
            _scheduleBlockOrder.Clear();
            _scheduleBlocksByGrid.Clear();
            _selectedScheduleBlockId = null;

            panel1.SuspendLayout();
            try
            {
                panel1.Controls.Clear();
                panel1.AutoScroll = true;
                panel1.HorizontalScroll.Enabled = true;
                panel1.HorizontalScroll.Visible = true;
                panel1.VerticalScroll.Enabled = true;
                panel1.VerticalScroll.Visible = true;
            }
            finally
            {
                panel1.ResumeLayout(true);
            }
        }

        public void AddScheduleBlock(Guid blockId)
        {
            if (!_scheduleBlocksInitialized)
                InitializeScheduleBlocks();

            if (_scheduleBlocks.ContainsKey(blockId))
                return;

            var block = CreateScheduleBlockFromTemplate(blockId);

            _scheduleBlocks[blockId] = block;
            _scheduleBlockOrder.Add(blockId);
            _scheduleBlocksByGrid[block.SlotGrid] = block;

            LayoutScheduleBlocks();
        }

        public void RemoveScheduleBlock(Guid blockId)
        {
            if (!_scheduleBlocks.Remove(blockId, out var block))
                return;

            _scheduleBlocksByGrid.Remove(block.SlotGrid);
            _scheduleBlockOrder.Remove(blockId);

            if (ReferenceEquals(block.HostPanel.Parent, panel1))
                panel1.Controls.Remove(block.HostPanel);

            block.HostPanel.Dispose();

            if (_selectedScheduleBlockId == blockId)
                _selectedScheduleBlockId = null;

            LayoutScheduleBlocks();
        }

        public void ClearScheduleBlocks()
        {
            foreach (var id in _scheduleBlockOrder.ToList())
                RemoveScheduleBlock(id);

            _scheduleBlocksInitialized = false;
        }

        public void SetSelectedScheduleBlock(Guid blockId)
        {
            if (!_scheduleBlocks.TryGetValue(blockId, out var selected))
                return;

            if (_selectedScheduleBlockId.HasValue &&
                _scheduleBlocks.TryGetValue(_selectedScheduleBlockId.Value, out var previous))
            {
                ApplyScheduleBlockSelection(previous, isSelected: false);
            }

            _selectedScheduleBlockId = blockId;
            ApplyScheduleBlockSelection(selected, isSelected: true);

            foreach (var block in _scheduleBlocks.Values)
            {
                var isSelected = block.Id == blockId;
                block.SlotGrid.ReadOnly = !isSelected;
                block.SlotGrid.ThemeStyle.ReadOnly = !isSelected;
            }
        }

        public void ClearSelectedScheduleBlock()
        {
            if (_selectedScheduleBlockId.HasValue &&
                _scheduleBlocks.TryGetValue(_selectedScheduleBlockId.Value, out var previous))
            {
                ApplyScheduleBlockSelection(previous, isSelected: false);
            }

            _selectedScheduleBlockId = null;
        }

        public void SetAddNewScheduleEnabled(bool enabled)
        {
            btnAddNewSchedule.Enabled = enabled;
        }

        private ScheduleBlockUi? GetSelectedScheduleBlock()
        {
            if (_selectedScheduleBlockId is null) return null;
            return _scheduleBlocks.TryGetValue(_selectedScheduleBlockId.Value, out var block) ? block : null;
        }

        private bool TryGetBlockFromGrid(object? sender, out ScheduleBlockUi block)
        {
            block = null!;
            return sender is Guna2DataGridView grid &&
                   _scheduleBlocksByGrid.TryGetValue(grid, out block);
        }

        private ScheduleBlockUi CreateScheduleBlockFromTemplate(Guid blockId)
        {
            var scheduleGroup = CloneScheduleGroupBox();
            var availabilityGroup = CloneAvailabilityGroupBox();
            var slotGridControl = CreateScheduleSlotGrid();
            var availabilityGridControl = CreateAvailabilityGrid();
            var hideShowButton = CloneHeaderButton(btnHideShowScheduleTable);
            var closeButton = CloneHeaderButton(btnCloseScheduleTable);

            // стало (як в дизайнері)
            var selectButton = CloneHeaderButton(guna2Button27);

            scheduleGroup.Controls.Add(slotGridControl);
            scheduleGroup.Controls.Add(hideShowButton);
            scheduleGroup.Controls.Add(closeButton);
            scheduleGroup.Controls.Add(selectButton);

            var titleButton = CloneTitleButton();
            scheduleGroup.Controls.Add(titleButton);

            availabilityGroup.Controls.Add(availabilityGridControl);
            availabilityGroup.Controls.Add(CloneAvailabilityTitleButton());

            ConfigureMatrixGrid(slotGridControl, readOnly: false);
            ConfigureMatrixGrid(availabilityGridControl, readOnly: true);

            var hostPanel = new Panel
            {
                BackColor = Color.Transparent,
                Size = new Size(scheduleGroup.Width, scheduleGroup.Height + availabilityGroup.Height + ScheduleBlockGap),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            scheduleGroup.Location = new Point(0, 0);
            availabilityGroup.Location = new Point(0, scheduleGroup.Bottom + ScheduleBlockGap);

            hostPanel.Controls.Add(availabilityGroup);
            hostPanel.Controls.Add(scheduleGroup);

            panel1.Controls.Add(hostPanel);

            PositionHeaderButtons(scheduleGroup, hideShowButton, closeButton, selectButton);

            closeButton.Click -= ScheduleBlockCloseButton_Click;
            closeButton.Click += ScheduleBlockCloseButton_Click;

            hideShowButton.Click -= ScheduleBlockToggleButton_Click;
            hideShowButton.Click += ScheduleBlockToggleButton_Click;

            selectButton.Click -= ScheduleBlockSelectButton_Click;
            selectButton.Click += ScheduleBlockSelectButton_Click;

            return new ScheduleBlockUi
            {
                Id = blockId,
                HostPanel = hostPanel,
                ScheduleGroupBox = scheduleGroup,
                AvailabilityGroupBox = availabilityGroup,
                SlotGrid = slotGridControl,
                AvailabilityGrid = availabilityGridControl,
                HideShowButton = hideShowButton,
                CloseButton = closeButton,
                SelectButton = selectButton,
                TitleButton = scheduleGroup.Controls.OfType<Guna2Button>().FirstOrDefault(b => b.Text == "Schedule Table"),
                ExpandedWidth = scheduleGroup.Width,
                ExpandedHeight = scheduleGroup.Height + availabilityGroup.Height + ScheduleBlockGap,
                CollapsedWidth = Math.Max(240, closeButton.Width + hideShowButton.Width + selectButton.Width + 30),
                DefaultBorderColor = scheduleGroup.BorderColor,
                DefaultCustomBorderColor = scheduleGroup.CustomBorderColor
            };
        }

        private void LayoutScheduleBlocks()
        {
            panel1.SuspendLayout();
            try
            {
                var x = ScheduleBlockStartX;
                var maxHeight = panel1.ClientSize.Height;

                foreach (var id in _scheduleBlockOrder)
                {
                    if (!_scheduleBlocks.TryGetValue(id, out var block))
                        continue;

                    // ✅ завжди беремо актуальні розміри
                    block.ExpandedWidth = block.ScheduleGroupBox.Width;

                    var expandedHeight =
                        block.ScheduleGroupBox.Height +
                        (block.AvailabilityGroupBox.Visible ? block.AvailabilityGroupBox.Height + ScheduleBlockGap : 0);

                    block.HostPanel.Location = new Point(x, ScheduleBlockStartY);
                    block.HostPanel.Width = block.IsCollapsed ? block.CollapsedWidth : block.ExpandedWidth;
                    block.HostPanel.Height = block.IsCollapsed ? block.ScheduleGroupBox.Height : expandedHeight;

                    // availability завжди під schedule
                    block.AvailabilityGroupBox.Location = new Point(0, block.ScheduleGroupBox.Bottom + ScheduleBlockGap);

                    x += block.HostPanel.Width + ScheduleBlockGap;
                    maxHeight = Math.Max(maxHeight, block.HostPanel.Bottom);
                }

                panel1.AutoScrollMinSize = new Size(Math.Max(panel1.ClientSize.Width, x), maxHeight);
            }
            finally
            {
                panel1.ResumeLayout(true);
            }
        }

        private void ApplyScheduleBlockSelection(ScheduleBlockUi block, bool isSelected)
        {
            var borderColor = isSelected ? _scheduleBlockHighlightColor : block.DefaultBorderColor;
            var customBorderColor = isSelected ? _scheduleBlockHighlightColor : block.DefaultCustomBorderColor;

            block.ScheduleGroupBox.BorderColor = borderColor;
            block.ScheduleGroupBox.CustomBorderColor = customBorderColor;

            block.AvailabilityGroupBox.BorderColor = borderColor;
            block.AvailabilityGroupBox.CustomBorderColor = customBorderColor;
        }

        private void ScheduleBlockSelectButton_Click(object? sender, EventArgs e)
        {
            if (!TryGetBlockFromHeaderButton(sender, out var block)) return;
            _ = RaiseScheduleBlockEventAsync(block.Id, ScheduleBlockSelectEvent);
        }

        private void ScheduleBlockCloseButton_Click(object? sender, EventArgs e)
        {
            if (!TryGetBlockFromHeaderButton(sender, out var block)) return;
            _ = RaiseScheduleBlockEventAsync(block.Id, ScheduleBlockCloseEvent);
        }

        private void ScheduleBlockToggleButton_Click(object? sender, EventArgs e)
        {
            if (!TryGetBlockFromHeaderButton(sender, out var block)) return;
            ToggleScheduleBlockCollapse(block);
        }

        private void ToggleScheduleBlockCollapse(ScheduleBlockUi block)
        {
            block.IsCollapsed = !block.IsCollapsed;
            block.AvailabilityGroupBox.Visible = !block.IsCollapsed;
            block.HideShowButton.Text = block.IsCollapsed ? "Show" : "Hide";

            foreach (Control control in block.ScheduleGroupBox.Controls)
            {
                if (ReferenceEquals(control, block.HideShowButton) ||
                    ReferenceEquals(control, block.CloseButton) ||
                    ReferenceEquals(control, block.SelectButton))
                    continue;

                control.Visible = !block.IsCollapsed;
                control.TabStop = !block.IsCollapsed;
            }

            if (block.IsCollapsed)
            {
                block.ScheduleGroupBox.Width = block.CollapsedWidth;
                block.HostPanel.Width = block.CollapsedWidth;
            }
            else
            {
                block.ScheduleGroupBox.Width = block.ExpandedWidth;
                block.HostPanel.Width = block.ExpandedWidth;
            }

            LayoutScheduleBlocks();
        }

        private bool TryGetBlockFromHeaderButton(object? sender, out ScheduleBlockUi block)
        {
            block = null!;
            if (sender is not Guna2Button button) return false;

            var match = _scheduleBlocks.Values.FirstOrDefault(b =>
                ReferenceEquals(b.SelectButton, button) ||
                ReferenceEquals(b.CloseButton, button) ||
                ReferenceEquals(b.HideShowButton, button));

            if (match == null)
                return false;

            block = match;
            return true;
        }

        private async Task RaiseScheduleBlockEventAsync(Guid blockId, Func<Guid, CancellationToken, Task>? ev)
        {
            if (ev == null) return;
            try { await ev(blockId, _lifetimeCts.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private Guna2GroupBox CloneScheduleGroupBox()
        {
            return CloneGroupBox(guna2GroupBox11);
        }

        private Guna2GroupBox CloneAvailabilityGroupBox()
        {
            return CloneGroupBox(guna2GroupBox15);
        }

        private Guna2GroupBox CloneGroupBox(Guna2GroupBox source)
        {
            var group = new Guna2GroupBox
            {
                Anchor = source.Anchor,
                BackColor = source.BackColor,
                BorderColor = source.BorderColor,
                BorderRadius = source.BorderRadius,
                BorderThickness = source.BorderThickness,
                CustomBorderColor = source.CustomBorderColor,
                Font = source.Font,
                ForeColor = source.ForeColor,
                Size = source.Size,
                Location = source.Location,
                ShadowDecoration =
                {
                    BorderRadius = source.ShadowDecoration.BorderRadius,
                    Depth = source.ShadowDecoration.Depth,
                    Enabled = source.ShadowDecoration.Enabled,
                    Shadow = source.ShadowDecoration.Shadow
                }
            };

            return group;
        }

        private Guna2DataGridView CreateScheduleSlotGrid()
        {
            return CloneGrid(slotGrid);
        }

        private Guna2DataGridView CreateAvailabilityGrid()
        {
            return CloneGrid(dataGridAvailabilityOnScheduleEdit);
        }

        private Guna2DataGridView CloneGrid(Guna2DataGridView source)
        {
            var grid = new Guna2DataGridView
            {
                Anchor = source.Anchor,
                Location = source.Location,
                Size = source.Size,
                Name = $"{source.Name}_{Guid.NewGuid():N}"
            };

            return grid;
        }

        private Guna2Button CloneHeaderButton(Guna2Button source)
        {
            var btn = new Guna2Button
            {
                Anchor = source.Anchor,
                Animated = source.Animated,
                AutoRoundedCorners = source.AutoRoundedCorners,
                BackColor = source.BackColor,
                BorderColor = source.BorderColor,
                BorderRadius = source.BorderRadius,
                BorderThickness = source.BorderThickness,
                Cursor = source.Cursor,
                CustomizableEdges = source.CustomizableEdges,

                FillColor = source.FillColor,
                FocusedColor = source.FocusedColor,
                Font = source.Font,
                ForeColor = source.ForeColor,

                Image = source.Image,
                ImageAlign = source.ImageAlign,
                ImageOffset = source.ImageOffset,
                ImageSize = source.ImageSize,

                Text = source.Text,
                TextAlign = source.TextAlign,
                TextOffset = source.TextOffset,

                Location = source.Location,
                Size = source.Size,
                Name = $"{source.Name}_{Guid.NewGuid():N}",
                UseTransparentBackground = source.UseTransparentBackground,
            };

            btn.DisabledState.BorderColor = source.DisabledState.BorderColor;
            btn.DisabledState.CustomBorderColor = source.DisabledState.CustomBorderColor;
            btn.DisabledState.FillColor = source.DisabledState.FillColor;
            btn.DisabledState.ForeColor = source.DisabledState.ForeColor;

            btn.ShadowDecoration.BorderRadius = source.ShadowDecoration.BorderRadius;
            btn.ShadowDecoration.Depth = source.ShadowDecoration.Depth;
            btn.ShadowDecoration.Enabled = source.ShadowDecoration.Enabled;
            btn.ShadowDecoration.Shadow = source.ShadowDecoration.Shadow;
            btn.ShadowDecoration.CustomizableEdges = source.ShadowDecoration.CustomizableEdges;

            return btn;
        }


        private Guna2Button CloneTitleButton()
        {
            var source = guna2Button12;
            return new Guna2Button
            {
                BackColor = source.BackColor,
                BorderRadius = source.BorderRadius,
                FillColor = source.FillColor,
                FocusedColor = source.FocusedColor,
                Font = source.Font,
                ForeColor = source.ForeColor,
                Image = source.Image,
                ImageSize = source.ImageSize,
                Location = source.Location,
                Size = source.Size,
                Text = source.Text,
                Tag = source.Tag
            };
        }

        private Guna2Button CloneAvailabilityTitleButton()
        {
            var source = guna2Button19;
            return new Guna2Button
            {
                BackColor = source.BackColor,
                BorderRadius = source.BorderRadius,
                FillColor = source.FillColor,
                FocusedColor = source.FocusedColor,
                Font = source.Font,
                ForeColor = source.ForeColor,
                Image = source.Image,
                ImageSize = source.ImageSize,
                Location = source.Location,
                Size = source.Size,
                Text = source.Text,
                Tag = source.Tag
            };
        }

        private static void PositionHeaderButtons(Guna2GroupBox groupBox, params Guna2Button[] buttons)
        {
            foreach (var button in buttons)
                button.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var right = groupBox.Width - 5;
            foreach (var button in buttons)
            {
                right -= button.Width;
                button.Location = new Point(right, button.Location.Y);
                right -= 5;
            }
        }

        private void EnsureScheduleBlockMetrics()
        {
            if (_scheduleBlockMetricsInit) return;
            _scheduleBlockMetricsInit = true;

            // ✅ мінімум — як у дизайнері
            _minScheduleGroupW = guna2GroupBox11.Width;
            _minScheduleGroupH = guna2GroupBox11.Height;
            _scheduleSidePad = guna2GroupBox11.Width - slotGrid.Width;
            _scheduleBottomPad = guna2GroupBox11.Height - slotGrid.Bottom;

            _minAvailGroupW = guna2GroupBox15.Width;
            _minAvailGroupH = guna2GroupBox15.Height;
            _availSidePad = guna2GroupBox15.Width - dataGridAvailabilityOnScheduleEdit.Width;
            _availBottomPad = guna2GroupBox15.Height - dataGridAvailabilityOnScheduleEdit.Bottom;
        }

        private static int CalcGridContentWidth(DataGridView grid)
        {
            int w = 0;

            // всі видимі колонки
            foreach (DataGridViewColumn c in grid.Columns)
                if (c.Visible) w += c.Width;

            if (grid.RowHeadersVisible)
                w += grid.RowHeadersWidth;

            // якщо є вертикальний скрол — він “з’їдає” частину ширини клієнт-області
            var vScroll = grid.Controls.OfType<VScrollBar>().FirstOrDefault();
            if (vScroll?.Visible == true)
                w += SystemInformation.VerticalScrollBarWidth;

            return w;
        }

        private static int CalcGridContentHeight(DataGridView grid)
        {
            int h = (grid.ColumnHeadersVisible ? grid.ColumnHeadersHeight : 0);

            // висота всіх видимих рядків
            h += grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible);

            // якщо є горизонтальний скрол — він “з’їдає” частину висоти
            var hScroll = grid.Controls.OfType<HScrollBar>().FirstOrDefault();
            if (hScroll?.Visible == true)
                h += SystemInformation.HorizontalScrollBarHeight;

            return h;
        }

        private void AutoSizeScheduleBlock(ScheduleBlockUi block)
        {
            if (block == null || IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => AutoSizeScheduleBlock(block))); return; }

            EnsureScheduleBlockMetrics();

            // --- Schedule group ---
            var scheduleGridW = CalcGridContentWidth(block.SlotGrid);
            var scheduleGridH = CalcGridContentHeight(block.SlotGrid);

            var newScheduleW = Math.Max(_minScheduleGroupW, scheduleGridW + _scheduleSidePad);
            var newScheduleH = Math.Max(_minScheduleGroupH, block.SlotGrid.Top + scheduleGridH + _scheduleBottomPad);

            block.ScheduleGroupBox.Width = newScheduleW;
            block.ScheduleGroupBox.Height = newScheduleH;

            // --- Availability group ---
            var availGridW = CalcGridContentWidth(block.AvailabilityGrid);
            var availGridH = CalcGridContentHeight(block.AvailabilityGrid);

            var newAvailW = Math.Max(_minAvailGroupW, availGridW + _availSidePad);
            var newAvailH = Math.Max(_minAvailGroupH, block.AvailabilityGrid.Top + availGridH + _availBottomPad);

            block.AvailabilityGroupBox.Width = Math.Max(newScheduleW, newAvailW); // ✅ щоб обидві карти мали однакову ширину
            block.AvailabilityGroupBox.Height = newAvailH;

            // ✅ хедер-кнопки треба перепозиціонувати після зміни ширини
            PositionHeaderButtons(block.ScheduleGroupBox, block.HideShowButton, block.CloseButton, block.SelectButton);

            LayoutScheduleBlocks();
        }


    }
}
