using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

public sealed class CalendarViewForm : Form
{
    private static readonly Color AppBack = Color.FromArgb(246, 247, 251);
    private static readonly Color CardBack = Color.White;
    private static readonly Color TextMain = Color.FromArgb(15, 23, 42);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);

    private readonly IReadOnlyList<EventItem> _events;
    private readonly Action<EventItem, DateTime> _editItem;
    private readonly Action<DateTime> _createItem;
    private readonly Action<EventItem> _completeItem;
    private readonly Action<EventItem> _togglePauseItem;
    private readonly Action<EventItem> _deleteItem;
    private readonly Action<EventItem, EventItem> _addToFolder;
    private readonly Action<EventItem> _createFolder;
    private readonly Action<string> _createFolderFromTag;
    private readonly MonthGrid _monthGrid;
    private readonly ListBox _items = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = EventCardRenderer.ItemHeight
    };
    private readonly Button _monthTitle = new() { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold), ForeColor = TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
    private readonly Label _monthSummary = new() { Dock = DockStyle.Fill, ForeColor = TextMuted };
    private readonly Label _dayTitle = new() { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = TextMain };
    private readonly Label _daySummary = new() { Dock = DockStyle.Fill, ForeColor = TextMuted };
    private readonly CheckBox _showLunar = new() { Text = "农历", Appearance = Appearance.Button, AutoSize = false, Width = 86, Height = 32, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
    private DateTime _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDay = DateTime.Today;

    public CalendarViewForm(IReadOnlyList<EventItem> events, Action<EventItem, DateTime> editItem, Action<DateTime> createItem,
        Action<EventItem> completeItem, Action<EventItem> togglePauseItem, Action<EventItem> deleteItem, Action<EventItem, EventItem> addToFolder,
        Action<EventItem> createFolder, Action<string> createFolderFromTag)
    {
        _events = events;
        _editItem = editItem;
        _createItem = createItem;
        _completeItem = completeItem;
        _togglePauseItem = togglePauseItem;
        _deleteItem = deleteItem;
        _addToFolder = addToFolder;
        _createFolder = createFolder;
        _createFolderFromTag = createFolderFromTag;
        _monthGrid = new MonthGrid(EventsOnDay, SelectDay);

        Text = "日历视图";
        Size = new Size(1040, 680);
        MinimumSize = new Size(840, 560);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppBack;

        BuildUi();
        RefreshCalendar();
        RefreshDay();
        L.Apply(this);
        SizeChanged += (_, _) => RefreshAfterResize();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20),
            BackColor = AppBack
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        root.Controls.Add(CalendarPanel(), 0, 0);
        root.Controls.Add(DayPanel(), 1, 0);
        Controls.Add(root);
    }

    public void RefreshView()
    {
        RefreshCalendar();
        RefreshDay();
    }

    private Control CalendarPanel()
    {
        var panel = Card();
        panel.RowCount = 4;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        panel.Controls.Add(CalendarHeader(), 0, 0);
        panel.Controls.Add(WeekHeader(), 0, 1);
        panel.Controls.Add(_monthGrid, 0, 2);

        _monthSummary.Text = "";
        _monthSummary.Padding = new Padding(2, 10, 0, 0);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        footer.Controls.Add(_monthSummary, 0, 0);
        _showLunar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        footer.Controls.Add(_showLunar, 1, 0);
        _showLunar.CheckedChanged += (_, _) =>
        {
            StyleLunarToggle(_showLunar);
            _monthGrid.SetShowLunar(_showLunar.Checked);
        };
        StyleLunarToggle(_showLunar);
        panel.Controls.Add(footer, 0, 3);
        return panel;
    }

    private static void StyleLunarToggle(CheckBox toggle)
    {
        toggle.Text = toggle.Checked ? "农历  开" : "农历  关";
        toggle.BackColor = toggle.Checked ? Color.FromArgb(239, 246, 255) : Color.White;
        toggle.ForeColor = toggle.Checked ? Accent : TextMuted;
        toggle.FlatAppearance.BorderColor = toggle.Checked ? Color.FromArgb(147, 197, 253) : Border;
    }

    private string MonthSummaryText()
    {
        var monthItems = _events
            .Where(e => e.NextDueAt(DateTime.Now) is DateTime due && due.Year == _visibleMonth.Year && due.Month == _visibleMonth.Month)
            .ToList();
        var overdue = monthItems.Count(e => e.Status is EventStatus.Overdue);
        var birthdays = monthItems.Count(e => e.Type is EventType.Birthday);
        return monthItems.Count == 0
            ? "本月还没有事项，点击日期后可新建"
            : $"本月 {monthItems.Count} 个事项 · {overdue} 个逾期 · {birthdays} 个生日\r\n颜色提示：红=逾期 / 橙=高优先级 / 蓝=普通 / 灰=低";
    }

    private Control CalendarHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));

        var prev = IconButton("<");
        var today = SecondaryButton("今天");
        var next = IconButton(">");
        prev.Click += (_, _) => MoveMonth(-1);
        today.Click += (_, _) =>
        {
            _visibleMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            SelectDay(DateTime.Today);
        };
        next.Click += (_, _) => MoveMonth(1);
        _monthTitle.FlatAppearance.BorderSize = 0;
        _monthTitle.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
        _monthTitle.Click += (_, _) => ChooseVisibleMonth();

        header.Controls.Add(_monthTitle, 0, 0);
        header.Controls.Add(prev, 1, 0);
        header.Controls.Add(today, 2, 0);
        header.Controls.Add(next, 3, 0);
        return header;
    }

    private void ChooseVisibleMonth()
    {
        using var picker = new DateRangePickerDialog.MonthYearDialog(_visibleMonth);
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        var day = Math.Min(_selectedDay.Day, DateTime.DaysInMonth(picker.SelectedMonth.Year, picker.SelectedMonth.Month));
        SelectDay(new DateTime(picker.SelectedMonth.Year, picker.SelectedMonth.Month, day));
    }

    private static Control WeekHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1
        };
        for (var i = 0; i < 7; i++)
        {
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7));
        }

        foreach (var day in new[] { "一", "二", "三", "四", "五", "六", "日" })
        {
            header.Controls.Add(new Label
            {
                Text = day,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = TextMuted,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            });
        }
        return header;
    }

    private Control DayPanel()
    {
        var panel = Card();
        panel.Margin = new Padding(16, 0, 0, 0);
        panel.RowCount = 4;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var addButton = PrimaryButton("在这一天新建事务");
        addButton.Click += (_, _) =>
        {
            _createItem(_selectedDay);
            RefreshCalendar();
            RefreshDay();
        };

        _items.BackColor = CardBack;
        _items.ForeColor = TextMain;
        _items.DrawItem += DrawCalendarItem;
        _items.DoubleClick += (_, _) =>
        {
            if (_items.SelectedItem is EventItem item)
            {
                _editItem(item, _selectedDay);
                RefreshCalendar();
                RefreshDay();
            }
        };
        AttachEventContextMenu();

        panel.Controls.Add(_dayTitle, 0, 0);
        panel.Controls.Add(_daySummary, 0, 1);
        panel.Controls.Add(_items, 0, 2);
        panel.Controls.Add(addButton, 0, 3);
        return panel;
    }

    private void AttachEventContextMenu()
    {
        _items.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) _items.SelectedIndex = _items.IndexFromPoint(e.Location);
        };
        var menu = new ModernContextMenuStrip();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            if (_items.SelectedItem is not EventItem item)
            {
                e.Cancel = true;
                return;
            }

            menu.Items.Add("编辑", null, (_, _) => RunAndRefresh(() => _editItem(item, _selectedDay)));
            if (item.IsRecurringSeries)
            {
                if (!item.IsRecurrencePaused)
                {
                    menu.Items.Add("完成本次", null, (_, _) => RunAndRefresh(() => _completeItem(item)));
                }
                menu.Items.Add(item.IsRecurrencePaused ? "恢复重复" : "暂停重复", null, (_, _) => RunAndRefresh(() => _togglePauseItem(item)));
            }
            else if (item.Status is not EventStatus.Done)
            {
                menu.Items.Add("完成", null, (_, _) => RunAndRefresh(() => _completeItem(item)));
            }

            var addToFolder = new ToolStripMenuItem("加入收藏夹");
            foreach (var folder in _events.Where(x => x.IsGroup).OrderBy(x => x.Title))
            {
                addToFolder.DropDownItems.Add(folder.Title, null, (_, _) => RunAndRefresh(() => _addToFolder(item, folder)));
            }
            if (addToFolder.DropDownItems.Count == 0) addToFolder.DropDownItems.Add("暂无收藏夹").Enabled = false;
            menu.Items.Add(addToFolder);
            menu.Items.Add("新建收藏夹并加入", null, (_, _) => RunAndRefresh(() => _createFolder(item)));

            var groupByTag = new ToolStripMenuItem("根据词条组合");
            foreach (var tag in SplitTags($"{item.Tags}, {item.Categories}").Distinct(StringComparer.OrdinalIgnoreCase))
            {
                groupByTag.DropDownItems.Add(tag, null, (_, _) => RunAndRefresh(() => _createFolderFromTag(tag)));
            }
            if (groupByTag.DropDownItems.Count == 0) groupByTag.DropDownItems.Add("该事件没有词条").Enabled = false;
            menu.Items.Add(groupByTag);

            menu.Items.Add(new ToolStripSeparator());
            var delete = menu.Items.Add("删除", null, (_, _) => RunAndRefresh(() => _deleteItem(item)));
            delete.ForeColor = Color.FromArgb(220, 38, 38);
        };
        _items.ContextMenuStrip = menu;
    }

    private void RunAndRefresh(Action action)
    {
        action();
        RefreshCalendar();
        RefreshDay();
    }

    private static TableLayoutPanel Card()
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(18),
            BackColor = CardBack
        };
    }

    private void MoveMonth(int delta)
    {
        _visibleMonth = _visibleMonth.AddMonths(delta);
        if (_selectedDay.Year != _visibleMonth.Year || _selectedDay.Month != _visibleMonth.Month)
        {
            _selectedDay = _visibleMonth;
        }
        RefreshCalendar();
        RefreshDay();
    }

    private void SelectDay(DateTime day)
    {
        _selectedDay = day.Date;
        _visibleMonth = new DateTime(day.Year, day.Month, 1);
        RefreshCalendar();
        RefreshDay();
    }

    private void RefreshAfterResize()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        BeginInvoke((MethodInvoker)(() =>
        {
            PerformLayout();
            _monthGrid.Invalidate();
            _items.Invalidate();
        }));
    }

    private void RefreshCalendar()
    {
        _monthTitle.Text = L.IsEnglish ? $"{_visibleMonth:MMMM yyyy}  ⌄" : $"{_visibleMonth:yyyy 年 M 月}  ⌄";
        if (L.IsEnglish) _monthTitle.Text = $"{_visibleMonth:MMMM yyyy}  ⌄";
        _monthSummary.Text = MonthSummaryText();
        _monthGrid.SetMonth(_visibleMonth, _selectedDay);
    }

    private void RefreshDay()
    {
        var items = EventsOnDay(_selectedDay);
        _dayTitle.Text = $"{_selectedDay:yyyy-MM-dd}";
        _daySummary.Text = items.Count == 0 ? L.T("这一天还没有事项") : L.IsEnglish ? $"{items.Count} events · Double-click to edit" : $"{items.Count} 个事项，双击可编辑";
        if (L.IsEnglish) _daySummary.Text = items.Count == 0 ? "No events on this day" : $"{items.Count} events · Double-click to edit";
        _items.Items.Clear();
        foreach (var item in items)
        {
            _items.Items.Add(item);
        }

        if (_items.Items.Count == 0)
        {
            _items.Items.Add(L.T("这一天还没有事项"));
        }
    }

    private IReadOnlyList<EventItem> EventsOnDay(DateTime day)
    {
        var now = DateTime.Now;
        return _events
            .Where(e => e.IsRecurringSeries ? e.OccursOn(day) : e.NextDueAt(now)?.Date == day.Date)
            .OrderBy(e => e.NextDueAt(now))
            .ToList();
    }

    private void DrawCalendarItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var canvas = new SolidBrush(CardBack);
        e.Graphics.FillRectangle(canvas, e.Bounds);

        if (_items.Items[e.Index] is not EventItem item)
        {
            using var emptyBrush = new SolidBrush(TextMuted);
            e.Graphics.DrawString(_items.Items[e.Index].ToString(), Font, emptyBrush, e.Bounds.Left + 12, e.Bounds.Top + 16);
            return;
        }

        EventCardRenderer.Draw(e.Graphics, e.Bounds, item, Font, (e.State & DrawItemState.Selected) != 0, CardBack);
    }

    private static Button PrimaryButton(string text)
    {
        var button = SecondaryButton(text);
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 36,
            BackColor = Color.White,
            ForeColor = TextMain,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 8, 4, 4)
        };
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    private static Button IconButton(string text)
    {
        var button = SecondaryButton(text);
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        return button;
    }

    private static void DrawText(Graphics graphics, string text, Font font, Brush brush, Rectangle bounds)
    {
        using var format = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static IEnumerable<string> SplitTags(string text)
    {
        return text
            .Split([',', '，', ';', '；', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0);
    }

    private static Color StatusColor(EventStatus status)
    {
        return status switch
        {
            EventStatus.Done => Color.FromArgb(22, 163, 74),
            EventStatus.Skipped => Color.FromArgb(100, 116, 139),
            EventStatus.Postponed => Color.FromArgb(217, 119, 6),
            EventStatus.Overdue => Color.FromArgb(220, 38, 38),
            EventStatus.Cancelled => Color.FromArgb(100, 116, 139),
            EventStatus.InProgress => Accent,
            _ => Color.FromArgb(79, 70, 229)
        };
    }

    private sealed class MonthGrid : Control
    {
        private readonly Func<DateTime, IReadOnlyList<EventItem>> _eventsOnDay;
        private readonly Action<DateTime> _selectDay;
        private DateTime _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _selectedDay = DateTime.Today;
        private DateTime? _hoverDay;
        private bool _showLunar;

        public MonthGrid(Func<DateTime, IReadOnlyList<EventItem>> eventsOnDay, Action<DateTime> selectDay)
        {
            _eventsOnDay = eventsOnDay;
            _selectDay = selectDay;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            BackColor = CardBack;
        }

        public void SetMonth(DateTime visibleMonth, DateTime selectedDay)
        {
            _visibleMonth = new DateTime(visibleMonth.Year, visibleMonth.Month, 1);
            _selectedDay = selectedDay.Date;
            Invalidate();
        }

        public void SetShowLunar(bool value) { _showLunar = value; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var firstCell = _visibleMonth.AddDays(-(((int)_visibleMonth.DayOfWeek + 6) % 7));
            var cellWidth = Math.Max(1, ClientSize.Width / 7);
            var cellHeight = Math.Max(1, ClientSize.Height / 6);

            using var mutedBrush = new SolidBrush(TextMuted);
            using var textBrush = new SolidBrush(TextMain);
            using var whiteBrush = new SolidBrush(Color.White);
            using var selectedBrush = new SolidBrush(Accent);
            using var todayPen = new Pen(Color.FromArgb(147, 197, 253), 1.5F);
            using var hoverBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
            using var eventBrush = new SolidBrush(Accent);

            for (var i = 0; i < 42; i++)
            {
                var day = firstCell.AddDays(i);
                var col = i % 7;
                var row = i / 7;
                var rect = new Rectangle(col * cellWidth + 3, row * cellHeight + 3, cellWidth - 6, cellHeight - 6);
                var inMonth = day.Month == _visibleMonth.Month;
                var selected = day.Date == _selectedDay;
                var today = day.Date == DateTime.Today;
                var hover = _hoverDay == day.Date;

                if (hover && !selected)
                {
                    using var hoverPath = RoundRect(rect, 12);
                    e.Graphics.FillPath(hoverBrush, hoverPath);
                }

                if (selected)
                {
                    using var selectedPath = RoundRect(rect, 12);
                    e.Graphics.FillPath(selectedBrush, selectedPath);
                }
                else if (today)
                {
                    using var todayPath = RoundRect(rect, 12);
                    e.Graphics.DrawPath(todayPen, todayPath);
                }

                var events = _eventsOnDay(day);
                var count = events.Count;
                var dayAccent = count > 0 ? DayAccent(events) : TextMain;
                var dayTextColor = selected
                    ? Color.White
                    : !inMonth
                        ? Color.FromArgb(203, 213, 225)
                        : count > 0
                            ? dayAccent
                            : TextMain;
                var dayFont = count > 0 && inMonth ? new Font(Font, FontStyle.Bold) : Font;
                var dayTextRect = new Rectangle(rect.Left + 8, rect.Top + 8, rect.Width - 16, 22);
                TextRenderer.DrawText(
                    e.Graphics,
                    day.Day.ToString(),
                    dayFont,
                    dayTextRect,
                    dayTextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                if (_showLunar && inMonth)
                {
                    TextRenderer.DrawText(e.Graphics, LunarDate.Text(day), new Font(Font.FontFamily, 7.5F), new Rectangle(rect.Left + 8, rect.Top + 27, rect.Width - 16, 17), selected ? Color.White : TextMuted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
                if (!ReferenceEquals(dayFont, Font))
                {
                    dayFont.Dispose();
                }

                if (count <= 0)
                {
                    continue;
                }

                if (cellHeight > 54)
                {
                    var badge = count > 9 ? "9+" : count.ToString();
                    var badgeRect = new Rectangle(rect.Right - 30, rect.Top + 10, 22, 18);
                    using var badgePath = RoundRect(badgeRect, 9);
                    using var badgeBrush = new SolidBrush(selected ? Color.FromArgb(255, 255, 255) : SoftColor(dayAccent));
                    using var badgeFont = new Font(Font.FontFamily, 7.5F, FontStyle.Bold);
                    e.Graphics.FillPath(badgeBrush, badgePath);
                    TextRenderer.DrawText(e.Graphics, badge, badgeFont, badgeRect, dayAccent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                if (cellHeight > 76 && rect.Width > 58)
                {
                    var labelTop = rect.Top + (_showLunar ? 47 : 34);
                    using var itemFont = new Font(Font.FontFamily, 7.5F);
                    foreach (var item in events.Take(2))
                    {
                        var labelRect = new Rectangle(rect.Left + 8, labelTop, rect.Width - 16, 17);
                        using var labelPath = RoundRect(labelRect, 7);
                        using var labelBrush = new SolidBrush(selected ? Color.FromArgb(255, 255, 255) : SoftColor(dayAccent));
                        e.Graphics.FillPath(labelBrush, labelPath);
                        TextRenderer.DrawText(
                            e.Graphics,
                            item.Title,
                            itemFont,
                            new Rectangle(labelRect.Left + 6, labelRect.Top, labelRect.Width - 10, labelRect.Height),
                            selected ? dayAccent : dayAccent,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        labelTop += 19;
                    }
                }

                var dotCount = Math.Min(3, count);
                var startX = rect.Left + 9;
                var dotY = rect.Bottom - 14;
                for (var dot = 0; dot < dotCount; dot++)
                {
                    using var dotBrush = new SolidBrush(selected ? Color.White : dayAccent);
                    e.Graphics.FillEllipse(dotBrush, startX + dot * 8, dotY, 5, 5);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var day = HitTest(e.Location);
            if (_hoverDay == day)
            {
                return;
            }

            _hoverDay = day;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverDay = null;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var day = HitTest(e.Location);
            if (day is not null)
            {
                _selectDay(day.Value);
            }
        }

        private DateTime? HitTest(Point point)
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return null;
            }

            var col = Math.Clamp(point.X / Math.Max(1, ClientSize.Width / 7), 0, 6);
            var row = Math.Clamp(point.Y / Math.Max(1, ClientSize.Height / 6), 0, 5);
            var firstCell = _visibleMonth.AddDays(-(((int)_visibleMonth.DayOfWeek + 6) % 7));
            return firstCell.AddDays(row * 7 + col).Date;
        }

        private static Color DayAccent(IReadOnlyList<EventItem> events)
        {
            if (events.Any(e => e.Status is EventStatus.Overdue))
            {
                return Color.FromArgb(220, 38, 38);
            }

            var highest = events
                .Select(e => e.Priority)
                .DefaultIfEmpty(EventPriority.None)
                .Max();
            return highest switch
            {
                EventPriority.High => Color.FromArgb(234, 88, 12),
                EventPriority.Normal => Accent,
                EventPriority.Low => Color.FromArgb(14, 116, 144),
                _ => Color.FromArgb(100, 116, 139)
            };
        }

        private static Color SoftColor(Color color)
        {
            return Color.FromArgb(
                245 - (245 - color.R) / 9,
                248 - (248 - color.G) / 9,
                255 - (255 - color.B) / 9);
        }
    }
}
