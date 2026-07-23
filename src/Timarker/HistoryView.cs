using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

public sealed class HistoryView : UserControl
{
    private static readonly Color AppBack = Color.FromArgb(246, 247, 251);
    private static readonly Color PanelBack = Color.White;
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private readonly List<ActivityRecord> _records;
    private readonly List<EventItem> _events;
    private readonly Action _save;
    private readonly RecordHeatmap _heatmap;
    private readonly ListBox _timeline = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawVariable, BackColor = AppBack };
    private readonly TableLayoutPanel _summary = new() { Dock = DockStyle.Fill, BackColor = PanelBack, Margin = new Padding(0, 0, 14, 14) };
    private readonly TextBox _title = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, PlaceholderText = "例如：完成了周报初稿" };
    private readonly TextBox _detail = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "补充成果、收获或下一步，可不填" };
    private readonly Button _date = DateButton();
    private DateTime _recordedAt = DateTime.Now;
    private readonly Label _empty = new() { Text = "完成事项后，时间足迹会自动出现在这里。", AutoSize = true, ForeColor = TextMuted, BackColor = AppBack };

    public HistoryView(List<ActivityRecord> records, List<EventItem> events, Action save)
    {
        _records = records;
        _events = events;
        _save = save;
        _heatmap = new RecordHeatmap(records, events);
        Dock = DockStyle.Fill;
        BackColor = AppBack;
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
        RefreshRecordDate();
        L.Apply(this);
        RefreshView();
    }

    private void BuildUi()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(26), BackColor = AppBack };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var words = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(2, 0, 0, 0) };
        words.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        words.Controls.Add(new Label { Text = "记录", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 18F, FontStyle.Bold), ForeColor = TextMain });
        words.Controls.Add(new Label { Text = "回看完成的事，也看见自己走了多远。", Dock = DockStyle.Fill, ForeColor = TextMuted }, 0, 1);
        shell.Controls.Add(words, 0, 0);
        shell.SetColumnSpan(words, 2);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Margin = new Padding(0, 0, 14, 0) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ModernUi.Round(_summary, 12);
        _summary.CellPaint += (_, e) =>
        {
            if (e.Row != 0 || e.Column >= 3) return;
            using var pen = new Pen(Border);
            var x = e.CellBounds.Right - 1;
            e.Graphics.DrawLine(pen, x, e.CellBounds.Top + 16, x, e.CellBounds.Bottom - 16);
        };
        left.Controls.Add(_summary);
        _heatmap.Margin = new Padding(0, 0, 14, 14);
        left.Controls.Add(_heatmap, 0, 1);
        var timelinePanel = new Panel { Dock = DockStyle.Fill, BackColor = AppBack, Padding = Padding.Empty };
        _timeline.MeasureItem += (_, e) => e.ItemHeight = 104;
        _timeline.DrawItem += DrawRecord;
        timelinePanel.Controls.Add(_timeline);
        _empty.Location = new Point(92, 26);
        timelinePanel.Controls.Add(_empty);
        _empty.BringToFront();
        left.Controls.Add(timelinePanel, 0, 2);
        shell.Controls.Add(left, 0, 1);
        shell.Controls.Add(BuildEditor(), 1, 1);
        Controls.Add(shell);
    }

    private Control BuildEditor()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = PanelBack, Padding = new Padding(24) };
        ModernUi.Round(card, 12);
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 9 };
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        heading.Controls.Add(new Label { Text = "补一条记录", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 15F, FontStyle.Bold), ForeColor = TextMain });
        heading.Controls.Add(new Label { Text = "适合补记工作成果或学习收获。", Dock = DockStyle.Fill, ForeColor = TextMuted }, 0, 1);
        form.Controls.Add(heading);
        AddLabel(form, "发生了什么", 1); form.Controls.Add(Field(_title), 0, 2);
        AddLabel(form, "补充", 3); form.Controls.Add(Field(_detail, 10), 0, 4);
        AddLabel(form, "记录时间", 5); form.Controls.Add(_date, 0, 6);
        form.Controls.Add(new Label { Text = "完成事项、项目或庆祝重要日子时，会自动留下记录。", Dock = DockStyle.Fill, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleLeft }, 0, 7);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
        var save = ActionButton("保存记录", true); save.Click += (_, _) => AddRecord();
        var clear = ActionButton("清空", false); clear.Click += (_, _) => ClearEditor();
        actions.Controls.Add(save); actions.Controls.Add(clear); form.Controls.Add(actions, 0, 8);
        _date.Click += (_, _) => ChooseRecordDate();
        card.Controls.Add(form); return card;
    }

    public void RefreshView()
    {
        BuildSummary();
        _heatmap.Invalidate();
        _timeline.Items.Clear();
        foreach (var record in _records.OrderByDescending(x => x.OccurredAt)) _timeline.Items.Add(record);
        _empty.Visible = _records.Count == 0;
    }

    private void BuildSummary()
    {
        _summary.Controls.Clear();
        _summary.ColumnStyles.Clear();
        _summary.ColumnCount = 4;
        _summary.RowCount = 1;
        _summary.RowStyles.Clear();
        _summary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) _summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        var values = new[]
        {
            (L.T("已完成"), _records.Count(x => x.Kind is ActivityRecordKind.EventCompleted).ToString()),
            (L.T("重要时刻"), _records.Count(x => x.Kind is ActivityRecordKind.BirthdayCelebrated or ActivityRecordKind.AnniversaryCelebrated).ToString()),
            (L.T("完成项目"), _records.Count(x => x.Kind is ActivityRecordKind.ProjectCompleted).ToString()),
            (L.T("连续记录"), L.IsEnglish ? $"{CurrentStreak()} days" : $"{CurrentStreak()} 天")
        };
        for (var i = 0; i < values.Length; i++)
        {
            _summary.Controls.Add(Metric(values[i].Item1, values[i].Item2), i, 0);
        }
    }

    private Control Metric(string label, string value)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(16, 10, 12, 8) };
        panel.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 15F, FontStyle.Bold), ForeColor = TextMain });
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, ForeColor = TextMuted }, 0, 1); return panel;
    }

    private int CurrentStreak()
    {
        var days = _records.Select(x => x.OccurredAt.Date).Distinct().ToHashSet();
        var day = DateTime.Today;
        if (!days.Contains(day)) day = day.AddDays(-1);
        var count = 0;
        while (days.Contains(day)) { count++; day = day.AddDays(-1); }
        return count;
    }

    private void DrawRecord(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _timeline.Items[e.Index] is not ActivityRecord record) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var background = new SolidBrush(AppBack)) e.Graphics.FillRectangle(background, e.Bounds);
        var dateBounds = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 10, 72, 66);
        var cardBounds = new Rectangle(e.Bounds.Left + 88, e.Bounds.Top + 5, Math.Max(40, e.Bounds.Width - 96), 90);
        using var cardPath = Rounded(cardBounds, 10);
        using var cardBrush = new SolidBrush(PanelBack);
        using var cardPen = new Pen(Border);
        e.Graphics.FillPath(cardBrush, cardPath);
        e.Graphics.DrawPath(cardPen, cardPath);
        using var rail = new SolidBrush(KindColor(record.Kind));
        e.Graphics.FillRectangle(rail, cardBounds.Left, cardBounds.Top + 14, 4, cardBounds.Height - 28);
        using var dayFont = new Font(Font.FontFamily, 14F, FontStyle.Bold);
        using var monthFont = new Font(Font.FontFamily, 8F);
        TextRenderer.DrawText(e.Graphics, record.OccurredAt.ToString("dd"), dayFont, new Rectangle(dateBounds.Left, dateBounds.Top, dateBounds.Width, 30), TextMain, TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(e.Graphics, record.OccurredAt.ToString("yyyy.MM"), monthFont, new Rectangle(dateBounds.Left, dateBounds.Top + 31, dateBounds.Width, 22), TextMuted, TextFormatFlags.HorizontalCenter);
        using var titleFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, record.Title, titleFont, new Rectangle(cardBounds.Left + 20, cardBounds.Top + 16, cardBounds.Width - 34, 24), TextMain, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, string.IsNullOrWhiteSpace(record.Detail) ? KindText(record.Kind) : record.Detail, Font, new Rectangle(cardBounds.Left + 20, cardBounds.Top + 47, cardBounds.Width - 34, 24), TextMuted, TextFormatFlags.EndEllipsis);
    }

    private void AddRecord()
    {
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            MessageBox.Show("请写下发生了什么。", "记录是空的", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _records.Add(new ActivityRecord { Kind = ActivityRecordKind.Note, Title = _title.Text.Trim(), Detail = _detail.Text.Trim(), OccurredAt = _recordedAt });
        _save();
        ClearEditor();
        RefreshView();
    }

    private void ChooseRecordDate()
    {
        using var picker = new DateRangePickerDialog(_recordedAt, _recordedAt, true, false, _recordedAt.TimeOfDay, _recordedAt.TimeOfDay);
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        _recordedAt = picker.HasStart
            ? picker.StartDate.Add(picker.StartTime)
            : picker.EndDate.Add(picker.EndTime);
        RefreshRecordDate();
    }

    private void RefreshRecordDate() => _date.Text = $"{_recordedAt:yyyy-MM-dd}  ·  {_recordedAt:HH:mm}";

    private void ClearEditor() { _title.Clear(); _detail.Clear(); _recordedAt = DateTime.Now; RefreshRecordDate(); _title.Focus(); }
    private static void AddLabel(TableLayoutPanel form, string text, int row) => form.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, ForeColor = TextMuted, TextAlign = ContentAlignment.BottomLeft }, 0, row);
    private static Control Field(Control control, int verticalPadding = 7)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, verticalPadding, 10, verticalPadding) };
        panel.Paint += (_, e) => { using var pen = new Pen(Border); e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1); };
        panel.Controls.Add(control); return panel;
    }
    private static Button ActionButton(string text, bool primary)
    {
        var button = new ModernButton
        {
            Text = text,
            Width = primary ? 100 : 76,
            Height = 38,
            BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : TextMain,
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        return button;
    }
    private static Button DateButton()
    {
        var button = new ModernButton
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = TextMain,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            Cursor = Cursors.Hand,
            Margin = Padding.Empty
        };
        button.FlatAppearance.BorderColor = Border;
        return button;
    }
    private static Color KindColor(ActivityRecordKind kind) => kind switch
    {
        ActivityRecordKind.BirthdayCelebrated => Color.FromArgb(236, 72, 153),
        ActivityRecordKind.AnniversaryCelebrated => Color.FromArgb(139, 92, 246),
        ActivityRecordKind.ProjectCompleted => Color.FromArgb(16, 185, 129),
        _ => Accent
    };
    private static string KindText(ActivityRecordKind kind) => kind switch
    {
        ActivityRecordKind.BirthdayCelebrated => L.T("庆祝生日"),
        ActivityRecordKind.AnniversaryCelebrated => L.T("纪念重要日子"),
        ActivityRecordKind.ProjectCompleted => L.T("完成项目"),
        ActivityRecordKind.EventCompleted => L.T("完成事项"),
        _ => L.T("手动记录")
    };
    private static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RecordHeatmap : Control
{
    private static readonly Color Surface = Color.White;
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color[] Levels =
    [
        Color.FromArgb(241, 245, 249),
        Color.FromArgb(219, 234, 254),
        Color.FromArgb(147, 197, 253),
        Color.FromArgb(59, 130, 246),
        Color.FromArgb(29, 78, 216)
    ];
    private readonly List<ActivityRecord> _records;
    private readonly List<EventItem> _events;

    public RecordHeatmap(List<ActivityRecord> records, List<EventItem> events)
    {
        _records = records;
        _events = events;
        Dock = DockStyle.Fill;
        BackColor = Surface;
        DoubleBuffered = true;
        AccessibleName = "每日记录";
        ModernUi.Round(this, 12);
        System.Diagnostics.Debug.Assert(HeatLevel(0) == 0 && HeatLevel(1) == 1 && HeatLevel(5) == 4);
        var checkDay = DateTime.Today;
        var completedCheck = new EventItem { StartAt = checkDay, Status = EventStatus.Done };
        System.Diagnostics.Debug.Assert(IsDueOn(completedCheck, checkDay) && IsDoneOn(completedCheck, checkDay));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var borderPath = ModernUi.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 12))
        using (var pen = new Pen(Border))
        {
            e.Graphics.DrawPath(pen, borderPath);
        }

        using var titleFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        using var smallFont = new Font(Font.FontFamily, 8F);
        TextRenderer.DrawText(e.Graphics, L.T("每日记录"), titleFont, new Point(18, 13), TextMain);
        TextRenderer.DrawText(e.Graphics, L.T("过去一年"), Font, new Rectangle(Width - 104, 13, 86, 22), TextMuted, TextFormatFlags.Right);

        const int cell = 9;
        const int gap = 3;
        const int step = cell + gap;
        const int gridX = 46;
        const int gridY = 46;
        var weeks = Math.Clamp((Width - gridX - 48) / step, 8, 53);
        var weekEnd = DateTime.Today.AddDays(6 - (int)DateTime.Today.DayOfWeek);
        var start = weekEnd.AddDays(-(weeks * 7 - 1));
        var clearedDays = ClearedDays(start, DateTime.Today);
        var counts = _records
            .GroupBy(x => x.OccurredAt.Date)
            .ToDictionary(x => x.Key, x => x.Count());

        TextRenderer.DrawText(e.Graphics, L.T("一"), smallFont, new Point(20, gridY + step - 3), TextMuted);
        TextRenderer.DrawText(e.Graphics, L.T("三"), smallFont, new Point(20, gridY + step * 3 - 3), TextMuted);
        TextRenderer.DrawText(e.Graphics, L.T("五"), smallFont, new Point(20, gridY + step * 5 - 3), TextMuted);

        var previousMonth = start.Month;
        for (var week = 0; week < weeks; week++)
        {
            var sunday = start.AddDays(week * 7);
            if (sunday.Month != previousMonth && week < weeks - 2)
            {
                var monthText = L.IsEnglish ? sunday.ToString("MMM") : $"{sunday.Month}月";
                TextRenderer.DrawText(e.Graphics, monthText, smallFont, new Point(gridX + week * step, gridY - 17), TextMuted);
                previousMonth = sunday.Month;
            }
            for (var day = 0; day < 7; day++)
            {
                var date = sunday.AddDays(day);
                var count = counts.GetValueOrDefault(date);
                var color = date > DateTime.Today ? Color.FromArgb(248, 250, 252) : Levels[HeatLevel(count)];
                using var brush = new SolidBrush(color);
                var cellBounds = new Rectangle(gridX + week * step, gridY + day * step, cell, cell);
                e.Graphics.FillRectangle(brush, cellBounds);
                if (clearedDays.Contains(date.Date))
                {
                    using var clearedPen = new Pen(Color.FromArgb(22, 163, 74));
                    e.Graphics.DrawRectangle(clearedPen, cellBounds.X, cellBounds.Y, cellBounds.Width - 1, cellBounds.Height - 1);
                }
            }
        }

        var legendX = Width - 29;
        TextRenderer.DrawText(e.Graphics, L.T("少"), smallFont, new Point(legendX - 1, gridY - 18), TextMuted);
        for (var i = 0; i < Levels.Length; i++)
        {
            using var brush = new SolidBrush(Levels[i]);
            e.Graphics.FillRectangle(brush, legendX, gridY + i * 13, cell, cell);
        }
        TextRenderer.DrawText(e.Graphics, L.T("多"), smallFont, new Point(legendX - 1, gridY + Levels.Length * 13 + 1), TextMuted);

        using var clearedLegendPen = new Pen(Color.FromArgb(22, 163, 74), 1.4F);
        e.Graphics.DrawRectangle(clearedLegendPen, 19, Height - 24, cell + 2, cell + 2);
        TextRenderer.DrawText(e.Graphics, L.T("当日清空"), smallFont, new Point(36, Height - 27), TextMuted);
    }

    private HashSet<DateTime> ClearedDays(DateTime start, DateTime end)
    {
        var candidates = new HashSet<DateTime>();
        foreach (var item in _events.Where(x => !x.IsGroup && !x.IsProject && x.Status is not EventStatus.Cancelled))
        {
            foreach (var occurrence in item.Occurrences.Where(x => x.Status is EventStatus.Done))
            {
                if (occurrence.EffectiveAt.Date >= start.Date && occurrence.EffectiveAt.Date <= end.Date) candidates.Add(occurrence.EffectiveAt.Date);
            }
            if (!item.IsRecurringSeries && item.Status is EventStatus.Done && ScheduledDate(item) is DateTime scheduled
                && scheduled.Date >= start.Date && scheduled.Date <= end.Date)
            {
                candidates.Add(scheduled.Date);
            }
        }

        return candidates.Where(day =>
        {
            var due = _events
                .Where(x => !x.IsGroup && !x.IsProject && x.Status is not EventStatus.Cancelled)
                .Where(x => IsDueOn(x, day))
                .ToList();
            return due.Count > 0 && due.All(x => IsDoneOn(x, day));
        }).ToHashSet();
    }

    private static bool IsDueOn(EventItem item, DateTime day)
    {
        if (item.IsRecurringSeries || item.Type is EventType.Birthday or EventType.Anniversary)
        {
            return item.Occurrences.Any(x => x.EffectiveAt.Date == day.Date) || item.OccursOn(day);
        }
        return ScheduledDate(item)?.Date == day.Date;
    }

    private static bool IsDoneOn(EventItem item, DateTime day)
    {
        if (item.IsRecurringSeries || item.Type is EventType.Birthday or EventType.Anniversary)
        {
            return item.Occurrences.Any(x => x.EffectiveAt.Date == day.Date && x.Status is EventStatus.Done);
        }
        return item.Status is EventStatus.Done;
    }

    private static DateTime? ScheduledDate(EventItem item) => item.DeadlineAt ?? item.EndAt ?? item.StartAt;

    private static int HeatLevel(int count) => count switch
    {
        <= 0 => 0,
        1 => 1,
        2 => 2,
        <= 4 => 3,
        _ => 4
    };
}
