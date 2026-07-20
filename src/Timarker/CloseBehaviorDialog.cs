namespace Timarker;

public enum CloseBehavior
{
    MinimizeToTray,
    Exit
}

public sealed class CloseBehaviorDialog : Form
{
    public CloseBehavior SelectedBehavior { get; private set; } = CloseBehavior.MinimizeToTray;

    public CloseBehaviorDialog()
    {
        Text = "关闭事刻";
        Width = 420;
        Height = 220;
        MinimumSize = new Size(380, 200);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 247, 251);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(new Label
        {
            Text = "关闭窗口后，你希望事刻怎么运行？",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        });
        root.Controls.Add(new Label
        {
            Text = "最小化到系统托盘后，提醒会继续在后台工作；退出程序后，将不会再触发提醒。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(71, 85, 105)
        });
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        buttons.Controls.Add(Button("最小化到托盘", CloseBehavior.MinimizeToTray, true));
        buttons.Controls.Add(Button("退出程序", CloseBehavior.Exit, false));
        root.Controls.Add(buttons);
        Controls.Add(root);
        L.Apply(this);
        L.Apply(this);
        L.Apply(this);
    }

    private Button Button(string text, CloseBehavior behavior, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 112,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55),
            Margin = new Padding(8, 6, 0, 0)
        };
        button.Click += (_, _) =>
        {
            SelectedBehavior = behavior;
            DialogResult = DialogResult.OK;
        };
        return button;
    }
}
