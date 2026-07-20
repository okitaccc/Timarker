using Timarker.Models;

namespace Timarker;

public sealed class FloatingCountdownForm : Form
{
    private static readonly Color Back = Color.FromArgb(17, 24, 39);
    private static readonly Color Muted = Color.FromArgb(156, 163, 175);
    private static readonly Color Accent = Color.FromArgb(96, 165, 250);

    private readonly Func<IReadOnlyList<EventItem>> _getEvents;
    private readonly Action<EventItem> _complete;
    private readonly Label _title = new();
    private readonly Label _time = new();
    private readonly Label _meta = new();
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private Point _dragStart;

    public FloatingCountdownForm(Func<IReadOnlyList<EventItem>> getEvents, Action<EventItem> complete)
    {
        _getEvents = getEvents;
        _complete = complete;

        Text = "Timarker 悬浮倒计时";
        Width = 360;
        Height = 260;
        TopMost = true;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Back;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(Screen.PrimaryScreen?.WorkingArea.Right - Width - 24 ?? 100, 80);
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        MouseDown += StartDrag;
        MouseMove += DragWindow;
        _timer.Tick += (_, _) => RefreshContent();
        _timer.Start();
        RefreshContent();
        L.Apply(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        foreach (var label in new[] { _title, _time, _meta })
        {
            label.Dock = DockStyle.Fill;
            label.MouseDown += StartDrag;
            label.MouseMove += DragWindow;
        }
        _title.ForeColor = Color.White;
        _title.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        _time.ForeColor = Accent;
        _time.Font = new Font(Font.FontFamily, 20F, FontStyle.Bold);
        _meta.ForeColor = Muted;

        root.Controls.Add(_title);
        root.Controls.Add(_time);
        root.Controls.Add(_meta);

        _list.BackColor = Back;
        _list.ForeColor = Color.White;
        root.Controls.Add(_list);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var close = SmallButton("关闭");
        close.Click += (_, _) => Close();
        buttons.Controls.Add(close);

        var done = SmallButton("完成");
        done.Click += (_, _) =>
        {
            var item = _getEvents().FirstOrDefault();
            if (item is not null)
            {
                _complete(item);
                RefreshContent();
            }
        };
        buttons.Controls.Add(done);

        root.Controls.Add(buttons);
        Controls.Add(root);
    }

    private void RefreshContent()
    {
        var items = _getEvents();
        _list.Items.Clear();
        if (items.Count == 0)
        {
            _title.Text = "暂无待处理事项";
            _time.Text = "--:--:--";
            _meta.Text = "添加带时间的提醒后会显示在这里";
            return;
        }

        var first = items[0];
        var due = first.NextDueAt(DateTime.Now);
        var left = due is null ? TimeSpan.Zero : due.Value - DateTime.Now;
        var overdue = left < TimeSpan.Zero;
        left = left.Duration();

        _title.Text = first.Title;
        _time.Text = $"{(overdue ? "已过 " : "")}{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        _meta.Text = $"{first.TypeText} · {due:MM-dd HH:mm} · {first.StatusText}";

        foreach (var item in items.Skip(1))
        {
            _list.Items.Add($"{item.NextDueAt(DateTime.Now):MM-dd HH:mm}  {item.Title}");
        }
    }

    private static Button SmallButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 28,
            AutoSize = true,
            BackColor = Color.FromArgb(31, 41, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 0, 0, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(55, 65, 81);
        return button;
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragStart = e.Location;
        }
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Left += e.X - _dragStart.X;
            Top += e.Y - _dragStart.Y;
        }
    }
}
