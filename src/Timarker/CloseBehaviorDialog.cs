namespace Timarker;

public enum CloseBehavior
{
    MinimizeToTray,
    Exit
}

public sealed class CloseBehaviorDialog : Form
{
    private static readonly Color AppBack = Color.FromArgb(246, 247, 251);
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private readonly RadioButton _minimize = new() { Text = "最小化到系统托盘", AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
    private readonly RadioButton _exit = new() { Text = "退出 Timarker", AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
    private readonly CheckBox _remember = new() { Text = "记住我的选择，以后不再询问", AutoSize = true };

    public CloseBehavior SelectedBehavior => _minimize.Checked ? CloseBehavior.MinimizeToTray : CloseBehavior.Exit;
    public bool RememberChoice => _remember.Checked;

    public CloseBehaviorDialog(CloseBehavior currentBehavior = CloseBehavior.MinimizeToTray)
    {
        Text = "关闭 Timarker";
        ClientSize = new Size(480, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppBack;

        _minimize.Checked = currentBehavior is CloseBehavior.MinimizeToTray;
        _exit.Checked = currentBehavior is CloseBehavior.Exit;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(26, 22, 26, 20),
            BackColor = AppBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = Padding.Empty };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        header.Controls.Add(new Label
        {
            Text = "关闭窗口",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            ForeColor = TextMain
        });
        header.Controls.Add(new Label
        {
            Text = "请选择关闭主窗口后的操作。",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted
        }, 0, 1);
        root.Controls.Add(header);
        root.Controls.Add(Option(_minimize, "Timarker 将继续在后台运行，并按计划发送提醒。"), 0, 1);
        root.Controls.Add(Option(_exit, "结束程序，关闭后将不再发送提醒。"), 0, 2);

        var remember = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 14, 0, 0) };
        remember.Controls.Add(_remember);
        root.Controls.Add(remember, 0, 3);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var confirm = ActionButton("确定", true);
        var cancel = ActionButton("取消", false);
        confirm.Click += (_, _) => DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        actions.Controls.Add(confirm);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 4);

        AcceptButton = confirm;
        CancelButton = cancel;
        Controls.Add(root);
        L.Apply(this);
    }

    private static Control Option(RadioButton radio, string description)
    {
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            Padding = new Padding(1),
            BackColor = Border
        };
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(14, 9, 14, 7),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        radio.Dock = DockStyle.Fill;
        card.Controls.Add(radio);
        card.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, ForeColor = TextMuted, Padding = new Padding(22, 0, 0, 0) }, 0, 1);
        card.Click += (_, _) => radio.Checked = true;
        foreach (Control child in card.Controls) child.Click += (_, _) => radio.Checked = true;
        frame.Controls.Add(card);
        frame.Click += (_, _) => radio.Checked = true;
        return frame;
    }

    private static Button ActionButton(string text, bool primary) => new()
    {
        Text = text,
        Width = primary ? 96 : 82,
        Height = 34,
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Accent : Color.White,
        ForeColor = primary ? Color.White : TextMain,
        Margin = new Padding(0, 0, 8, 0)
    };
}
