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
    private readonly ModernNumericUpDown _minutesBox = new()
    {
        Minimum = 1,
        Maximum = 1440,
        Increment = 5,
        Width = 104,
        Height = 32
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
        Font = UiTokens.Font();
        BackColor = UiTokens.AppBackground;
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(new Label
        {
            Text = "现在需要处理这个事项吗？",
            Dock = DockStyle.Fill,
            Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold),
            ForeColor = UiTokens.Text
        });

        root.Controls.Add(new Label
        {
            Text = $"{item.Title}\r\n{item.TypeText} · {dueText}\r\n{item.Notes}",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = UiTokens.Text
        });

        var minutes = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty
        };
        minutes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        minutes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        minutes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        minutes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        minutes.Controls.Add(new Label
        {
            Text = "稍后/延期",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTokens.TextMuted
        }, 0, 0);
        _minutesBox.Anchor = AnchorStyles.Left;
        _minutesBox.Margin = Padding.Empty;
        minutes.Controls.Add(_minutesBox, 1, 0);
        minutes.Controls.Add(new Label
        {
            Text = "分钟",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            ForeColor = UiTokens.TextMuted
        }, 2, 0);
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
        var button = new ModernButton
        {
            Text = text,
            Width = 86,
            Height = UiTokens.ControlHeight,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? UiTokens.Primary : UiTokens.Surface,
            ForeColor = primary ? Color.White : UiTokens.Text,
            Margin = new Padding(6, 6, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? UiTokens.Primary : UiTokens.Border;
        button.Click += (_, _) =>
        {
            SelectedAction = action;
            DialogResult = DialogResult.OK;
        };
        return button;
    }
}
