namespace Timarker;

public sealed class PomodoroForm : Form
{
    private static readonly Color Back = Color.FromArgb(248, 250, 252);
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);

    private readonly NotifyIcon _notifyIcon;
    private readonly Label _mode = new();
    private readonly Label _time = new();
    private readonly ModernNumericUpDown _focusMinutes = MinutesBox(25);
    private readonly ModernNumericUpDown _breakMinutes = MinutesBox(5);
    private readonly Button _switchButton = Button("切到休息", 96, accent: true);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private TimeSpan _left = TimeSpan.FromMinutes(25);
    private bool _running;
    private bool _focusMode = true;

    public PomodoroForm(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
        Text = "番茄钟";
        Width = 380;
        Height = 280;
        MinimumSize = new Size(320, 240);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Back;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        Resize += (_, _) => AdjustFonts();
        _timer.Tick += (_, _) => Tick();
        ResetCurrent();
        L.Apply(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Padding = new Padding(32),
            BackColor = Back
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            Padding = new Padding(34, 28, 34, 24),
            BackColor = Color.White,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        header.Controls.Add(new Label { Text = "番茄钟", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font.FontFamily, 16F, FontStyle.Bold), ForeColor = TextMain, AutoEllipsis = true });
        header.Controls.Add(new Label { Text = "一次只做一件事。离开此页面后计时仍会继续。", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = TextMuted, AutoEllipsis = true });
        root.Controls.Add(header);

        _mode.Dock = DockStyle.Fill;
        _mode.ForeColor = Accent;
        _mode.TextAlign = ContentAlignment.MiddleCenter;
        root.Controls.Add(_mode);

        _time.Dock = DockStyle.Fill;
        _time.ForeColor = TextMain;
        _time.TextAlign = ContentAlignment.MiddleCenter;
        root.Controls.Add(_time);

        var settings = new FlowLayoutPanel { Anchor = AnchorStyles.None, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        settings.Controls.Add(TextLabel("专注"));
        settings.Controls.Add(_focusMinutes);
        settings.Controls.Add(TextLabel("分钟"));
        settings.Controls.Add(TextLabel("休息"));
        settings.Controls.Add(_breakMinutes);
        settings.Controls.Add(TextLabel("分钟"));
        root.Controls.Add(settings);

        var buttons = new FlowLayoutPanel { Anchor = AnchorStyles.None, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var start = Button("开始 / 暂停", 108, primary: true);
        start.Click += (_, _) => Toggle();
        buttons.Controls.Add(start);

        var reset = Button("重置", 76);
        reset.Click += (_, _) => ResetCurrent();
        buttons.Controls.Add(reset);

        _switchButton.Click += (_, _) => SwitchMode();
        buttons.Controls.Add(_switchButton);
        root.Controls.Add(buttons);

        root.Controls.Add(new Label
        {
            Text = "时间可随时调整，重置后生效。",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleCenter
        });

        outer.Controls.Add(root, 1, 0);
        Controls.Add(outer);
        AdjustFonts();
    }

    private void Toggle()
    {
        _running = !_running;
        if (_running)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void Tick()
    {
        _left -= TimeSpan.FromSeconds(1);
        if (_left <= TimeSpan.Zero)
        {
            _notifyIcon.BalloonTipTitle = "番茄钟";
            _notifyIcon.BalloonTipText = _focusMode ? "专注结束，休息一下。" : "休息结束，开始下一轮。";
            _notifyIcon.ShowBalloonTip(4000);
            SwitchMode();
        }

        RefreshText();
    }

    private void SwitchMode()
    {
        _focusMode = !_focusMode;
        ResetCurrent();
    }

    private void ResetCurrent()
    {
        _left = TimeSpan.FromMinutes((double)(_focusMode ? _focusMinutes.Value : _breakMinutes.Value));
        RefreshText();
    }

    private void RefreshText()
    {
        _mode.Text = _focusMode ? "专注中" : "休息中";
        _switchButton.Text = _focusMode ? "切到休息" : "切到专注";
        _time.Text = $"{(int)_left.TotalMinutes:00}:{_left.Seconds:00}";
    }

    private void AdjustFonts()
    {
        var big = Math.Max(28, Math.Min(56, ClientSize.Width / 7));
        _time.Font = new Font(Font.FontFamily, big, FontStyle.Bold);
        _mode.Font = new Font(Font.FontFamily, Math.Max(10, big / 4), FontStyle.Bold);
    }

    private static ModernNumericUpDown MinutesBox(decimal value)
    {
        return new ModernNumericUpDown
        {
            Minimum = 1,
            Maximum = 180,
            Value = value,
            Width = 64,
            TextAlign = HorizontalAlignment.Center
        };
    }

    private static Label TextLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 8, 4, 0),
            ForeColor = TextMuted
        };
    }

    private static Button Button(string text, int width, bool primary = false, bool accent = false)
    {
        var button = new ModernButton
        {
            Text = text,
            Width = width,
            Height = 38,
            AutoSize = false,
            BackColor = primary ? Accent : accent ? Color.FromArgb(239, 246, 255) : Color.White,
            ForeColor = primary ? Color.White : accent ? Accent : TextMain,
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? Accent : accent ? Color.FromArgb(147, 197, 253) : Color.FromArgb(203, 213, 225);
        return button;
    }
}
