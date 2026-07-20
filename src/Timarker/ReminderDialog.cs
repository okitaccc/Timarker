using Timarker.Models;

namespace Timarker;

public enum ReminderDialogAction
{
    Ignore,
    Complete,
    Snooze,
    Postpone
}

public sealed class ReminderDialog : Form
{
    private readonly NumericUpDown _minutesBox = new()
    {
        Minimum = 1,
        Maximum = 1440,
        Increment = 5,
        Width = 86
    };

    public ReminderDialogAction SelectedAction { get; private set; } = ReminderDialogAction.Ignore;
    public int Minutes => (int)_minutesBox.Value;

    public ReminderDialog(EventItem item, int defaultMinutes)
    {
        Text = "提醒确认";
        Width = 430;
        Height = 280;
        MinimumSize = new Size(380, 260);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 247, 251);
        TopMost = true;
        _minutesBox.Value = Math.Min(_minutesBox.Maximum, Math.Max(_minutesBox.Minimum, defaultMinutes));
        BuildUi(item);
        L.Apply(this);
    }

    private void BuildUi(EventItem item)
    {
        var dueText = item.NextDueAt(DateTime.Now)?.ToString("yyyy-MM-dd HH:mm") ?? "未设置时间";
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(new Label
        {
            Text = "现在需要处理这个事项吗？",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        });

        root.Controls.Add(new Label
        {
            Text = $"{item.Title}\r\n{item.TypeText} · {dueText}\r\n{item.Notes}",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(31, 41, 55)
        });

        var minutes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        minutes.Controls.Add(new Label
        {
            Text = "稍后/延期",
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0),
            ForeColor = Color.FromArgb(107, 114, 128)
        });
        minutes.Controls.Add(_minutesBox);
        minutes.Controls.Add(new Label
        {
            Text = "分钟",
            AutoSize = true,
            Padding = new Padding(4, 8, 0, 0),
            ForeColor = Color.FromArgb(107, 114, 128)
        });
        root.Controls.Add(minutes);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        buttons.Controls.Add(Button("完成", ReminderDialogAction.Complete, true));
        buttons.Controls.Add(Button("稍后提醒", ReminderDialogAction.Snooze, false));
        buttons.Controls.Add(Button("延期事项", ReminderDialogAction.Postpone, false));
        buttons.Controls.Add(Button("忽略", ReminderDialogAction.Ignore, false));
        root.Controls.Add(buttons);

        Controls.Add(root);
    }

    private Button Button(string text, ReminderDialogAction action, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 86,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55),
            Margin = new Padding(6, 6, 0, 0)
        };
        button.Click += (_, _) =>
        {
            SelectedAction = action;
            DialogResult = DialogResult.OK;
        };
        return button;
    }
}
