using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

internal sealed class ProjectView : UserControl
{
    private static readonly Color AppBack = Color.FromArgb(246, 247, 251);
    private static readonly Color CardBack = Color.White;
    private static readonly Color TextMain = Color.FromArgb(15, 23, 42);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private static readonly Color Success = Color.FromArgb(22, 163, 74);

    private readonly List<EventItem> _events;
    private readonly Action<EventItem> _edit;
    private readonly Action<EventItem> _complete;
    private readonly Action<EventItem> _delete;
    private readonly Action _save;
    private readonly ListBox _projects = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 78,
        IntegralHeight = false
    };
    private readonly ListBox _steps = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 92,
        IntegralHeight = false
    };
    private readonly Label _title = Heading("选择一个项目", 16F);
    private readonly Label _subtitle = Muted("项目步骤按顺序执行，每一步都可以单独设置提醒。");
    private readonly Label _percent = Heading("0%", 26F);
    private readonly Label _progressText = Muted("还没有步骤");
    private readonly Label _nextAction = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = TextMain,
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
    };
    private readonly Label _deadline = Muted("未设置总体截止日期");
    private readonly ProgressLine _progress = new() { Dock = DockStyle.Fill, Height = 10 };
    private readonly Button _addStep = Button("新增步骤", true);
    private readonly Button _editProject = Button("编辑项目");
    private readonly Button _shiftProject = Button("顺延计划");
    private readonly Button _deleteProject = Button("删除项目", danger: true);
    private readonly Button _moveUp = Button("上移");
    private readonly Button _moveDown = Button("下移");
    private readonly Button _completeStep = Button("完成步骤", true);
    public ProjectView(List<EventItem> events, Action<EventItem> edit, Action<EventItem> complete, Action<EventItem> delete, Action save)
    {
        _events = events;
        _edit = edit;
        _complete = complete;
        _delete = delete;
        _save = save;
        Dock = DockStyle.Fill;
        BackColor = AppBack;
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
        RefreshView();
        L.Apply(this);
    }

    public void RefreshView()
    {
        var selectedId = SelectedProject()?.Id;
        _projects.Items.Clear();
        foreach (var project in Projects()) _projects.Items.Add(project);
        if (selectedId is not null)
        {
            for (var i = 0; i < _projects.Items.Count; i++)
            {
                if (_projects.Items[i] is EventItem project && project.Id == selectedId)
                {
                    _projects.SelectedIndex = i;
                    break;
                }
            }
        }
        if (_projects.SelectedIndex < 0 && _projects.Items.Count > 0) _projects.SelectedIndex = 0;
        RefreshSelectedProject();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = AppBack
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        root.Controls.Add(BuildProjectList(), 0, 0);
        root.Controls.Add(BuildSteps(), 1, 0);
        root.Controls.Add(BuildSummary(), 2, 0);
        Controls.Add(root);

        _projects.DrawItem += DrawProject;
        _projects.SelectedIndexChanged += (_, _) => RefreshSelectedProject();
        _steps.DrawItem += DrawStep;
        _steps.SelectedIndexChanged += (_, _) => UpdateStepButtons();
        _steps.DoubleClick += (_, _) => EditSelectedStep();
        _addStep.Click += (_, _) => AddStep();
        _editProject.Click += (_, _) => EditProject();
        _shiftProject.Click += (_, _) => ShiftProject();
        _deleteProject.Click += (_, _) => DeleteProject();
        _moveUp.Click += (_, _) => MoveStep(-1);
        _moveDown.Click += (_, _) => MoveStep(1);
        _completeStep.Click += (_, _) => CompleteSelectedStep();
        AttachStepMenu();
    }

    private Control BuildProjectList()
    {
        var panel = Card();
        panel.RowCount = 3;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.Controls.Add(HeaderBlock("项目", "把一件大事拆成可执行步骤。"), 0, 0);
        panel.Controls.Add(_projects, 0, 1);
        var create = Button("新建项目", true);
        create.Dock = DockStyle.Fill;
        create.Click += (_, _) => CreateProject();
        panel.Controls.Add(create, 0, 2);
        return panel;
    }

    private Control BuildSteps()
    {
        var panel = Card(new Padding(20, 16, 20, 16), new Padding(0, 0, 14, 0));
        panel.RowCount = 4;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        header.Controls.Add(_title);
        header.Controls.Add(_subtitle);
        panel.Controls.Add(header, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        actions.Controls.Add(_addStep);
        actions.Controls.Add(_moveUp);
        actions.Controls.Add(_moveDown);
        panel.Controls.Add(actions, 0, 1);
        panel.Controls.Add(_steps, 0, 2);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
        footer.Controls.Add(_completeStep);
        panel.Controls.Add(footer, 0, 3);
        return panel;
    }

    private Control BuildSummary()
    {
        var panel = Card(new Padding(20), Padding.Empty);
        panel.RowCount = 9;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.Controls.Add(Heading("项目摘要", 12F), 0, 0);
        panel.Controls.Add(_percent, 0, 1);
        panel.Controls.Add(_progress, 0, 2);
        panel.Controls.Add(_progressText, 0, 3);
        panel.Controls.Add(_deadline, 0, 4);
        panel.Controls.Add(BuildNextActionCard(), 0, 6);
        _deleteProject.Dock = DockStyle.Fill;
        _deleteProject.Margin = Padding.Empty;
        panel.Controls.Add(_deleteProject, 0, 8);
        return panel;
    }

    private Control BuildNextActionCard()
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(14, 10, 14, 12),
            Margin = Padding.Empty
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, card.ClientRectangle, 12, Border, 1F);
        ModernUi.Round(card, 12);
        card.Controls.Add(Muted("下一步行动"), 0, 0);
        card.Controls.Add(_nextAction, 0, 1);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _editProject.Dock = DockStyle.Fill;
        _editProject.Margin = new Padding(0, 3, 4, 0);
        _shiftProject.Dock = DockStyle.Fill;
        _shiftProject.Margin = new Padding(4, 3, 0, 0);
        actions.Controls.Add(_editProject, 0, 0);
        actions.Controls.Add(_shiftProject, 1, 0);
        card.Controls.Add(actions, 0, 2);
        return card;
    }

    private void CreateProject()
    {
        var project = new EventItem { IsProject = true, Type = EventType.Maybe, Priority = EventPriority.None, Tags = "项目" };
        using var dialog = new ProjectDialog(project, "新建项目");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _events.Add(project);
        _save();
        RefreshView();
        SelectProject(project.Id);
    }

    private void EditProject()
    {
        var project = SelectedProject();
        if (project is null) return;
        using var dialog = new ProjectDialog(project, "编辑项目");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _save();
        RefreshView();
    }

    private void AddStep()
    {
        var project = SelectedProject();
        if (project is null) return;
        var now = DateTime.Now.AddHours(1);
        var step = new EventItem
        {
            ProjectId = project.Id,
            ProjectOrder = Steps(project).Count + 1,
            StartAt = now,
            Type = EventType.StartAt,
            Status = EventStatus.Pending,
            ReminderRepeatMinutes = 10
        };
        using var dialog = new EventEditForm(step, true);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        step.ProjectId = project.Id;
        _events.Add(step);
        _save();
        RefreshSelectedProject();
        SelectStep(step.Id);
    }

    private void EditSelectedStep()
    {
        if (_steps.SelectedItem is not EventItem step) return;
        _edit(step);
        RefreshSelectedProject();
    }

    private void CompleteSelectedStep()
    {
        if (_steps.SelectedItem is not EventItem step || IsHandled(step)) return;
        _complete(step);
        RefreshSelectedProject();
    }

    private void MoveStep(int direction)
    {
        var project = SelectedProject();
        if (project is null || _steps.SelectedItem is not EventItem selected) return;
        var steps = Steps(project);
        var index = steps.IndexOf(selected);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= steps.Count) return;
        (steps[index], steps[target]) = (steps[target], steps[index]);
        NormalizeOrder(steps);
        _save();
        RefreshSelectedProject();
        SelectStep(selected.Id);
    }

    private void ShiftProject()
    {
        var project = SelectedProject();
        if (project is null) return;
        var days = PromptShiftDays();
        if (days is null) return;
        var minutes = days.Value * 1440;
        foreach (var step in Steps(project).Where(x => !IsHandled(x))) step.ShiftSchedule(minutes);
        project.DeadlineAt = project.DeadlineAt?.AddDays(days.Value);
        project.UpdatedAt = DateTime.Now;
        _save();
        RefreshSelectedProject();
    }

    private void DeleteProject()
    {
        var project = SelectedProject();
        if (project is null) return;
        var keep = new TaskDialogCommandLinkButton(L.T("仅删除项目"), L.T("步骤保留为普通事项"));
        var remove = new TaskDialogCommandLinkButton(L.T("删除项目和步骤"), L.T("同时删除项目内的全部步骤"));
        var page = new TaskDialogPage
        {
            Caption = L.T("删除项目"),
            Heading = L.IsEnglish ? $"Delete “{project.Title}”?" : $"确定删除“{project.Title}”吗？",
            Text = L.T("请选择项目步骤的处理方式。"),
            Icon = TaskDialogIcon.Warning,
            AllowCancel = true
        };
        page.Buttons.Add(keep);
        page.Buttons.Add(remove);
        var result = TaskDialog.ShowDialog(FindForm()!, page);
        if (result != keep && result != remove) return;
        var steps = Steps(project);
        if (result == remove)
        {
            foreach (var step in steps) _events.Remove(step);
        }
        else
        {
            foreach (var step in steps)
            {
                step.ProjectId = null;
                step.ProjectOrder = 0;
            }
        }
        _events.Remove(project);
        _save();
        RefreshView();
    }

    private void RefreshSelectedProject()
    {
        var project = SelectedProject();
        _steps.Items.Clear();
        var hasProject = project is not null;
        foreach (var control in new Control[] { _addStep, _editProject, _shiftProject, _deleteProject, _moveUp, _moveDown, _completeStep })
        {
            control.Enabled = hasProject;
        }
        if (project is null)
        {
            _title.Text = "选择一个项目";
            _subtitle.Text = "项目步骤按顺序执行，每一步都可以单独设置提醒。";
            _percent.Text = "0%";
            _progress.Value = 0;
            _progressText.Text = "还没有步骤";
            _deadline.Text = "未设置总体截止日期";
            _nextAction.Text = "—";
            _nextAction.ForeColor = TextMuted;
            return;
        }

        var steps = Steps(project);
        NormalizeOrder(steps);
        foreach (var step in steps) _steps.Items.Add(step);
        var completed = steps.Count(IsHandled);
        var progress = steps.Count == 0 ? 0 : completed * 100 / steps.Count;
        var next = steps.FirstOrDefault(x => !IsHandled(x));
        var allCompleted = steps.Count > 0 && completed == steps.Count;
        project.Status = allCompleted ? EventStatus.Done : EventStatus.Pending;
        _title.Text = project.Title;
        _subtitle.Text = string.IsNullOrWhiteSpace(project.Notes) ? "按顺序完成下面的步骤。" : project.Notes;
        _percent.Text = $"{progress}%";
        _progress.Value = progress;
        _progressText.Text = steps.Count == 0 ? "还没有步骤" : $"已完成 {completed} / {steps.Count} 步";
        _deadline.Text = project.DeadlineAt is null ? "未设置总体截止日期" : $"总体截止  {project.DeadlineAt:yyyy-MM-dd}";
        _nextAction.Text = next?.Title ?? (steps.Count == 0 ? "先添加第一个步骤" : "全部步骤已完成");
        _nextAction.ForeColor = allCompleted ? Success : TextMain;
        _steps.Invalidate();
        _projects.Invalidate();
        UpdateStepButtons();
    }

    private void UpdateStepButtons()
    {
        var selected = _steps.SelectedItem as EventItem;
        var project = SelectedProject();
        var steps = project is null ? [] : Steps(project);
        var index = selected is null ? -1 : steps.IndexOf(selected);
        _moveUp.Enabled = index > 0;
        _moveDown.Enabled = index >= 0 && index < steps.Count - 1;
        _completeStep.Enabled = selected is not null && !IsHandled(selected);
    }

    private void AttachStepMenu()
    {
        _steps.MouseDown += (_, e) =>
        {
            if (e.Button is MouseButtons.Right) _steps.SelectedIndex = _steps.IndexFromPoint(e.Location);
        };
        var menu = new ModernContextMenuStrip();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            if (_steps.SelectedItem is not EventItem step)
            {
                e.Cancel = true;
                return;
            }
            menu.Items.Add("编辑", null, (_, _) => EditSelectedStep());
            if (!IsHandled(step)) menu.Items.Add("完成步骤", null, (_, _) => CompleteSelectedStep());
            menu.Items.Add("上移", null, (_, _) => MoveStep(-1));
            menu.Items.Add("下移", null, (_, _) => MoveStep(1));
            menu.Items.Add("移出项目", null, (_, _) =>
            {
                step.ProjectId = null;
                step.ProjectOrder = 0;
                _save();
                RefreshSelectedProject();
            });
            menu.Items.Add(new ToolStripSeparator());
            var delete = menu.Items.Add("删除", null, (_, _) => { _delete(step); RefreshSelectedProject(); });
            delete.ForeColor = Color.FromArgb(220, 38, 38);
            L.Apply(menu);
        };
        _steps.ContextMenuStrip = menu;
    }

    private List<EventItem> Projects() => _events.Where(x => x.IsProject).OrderBy(x => x.Status is EventStatus.Done).ThenBy(x => x.DeadlineAt).ThenBy(x => x.Title).ToList();

    private List<EventItem> Steps(EventItem project) => _events.Where(x => x.ProjectId == project.Id && !x.IsProject && !x.IsGroup)
        .OrderBy(x => x.ProjectOrder).ThenBy(x => x.CreatedAt).ToList();

    private static bool IsHandled(EventItem item) => item.Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled;

    private static void NormalizeOrder(IReadOnlyList<EventItem> steps)
    {
        for (var i = 0; i < steps.Count; i++) steps[i].ProjectOrder = i + 1;
    }

    private EventItem? SelectedProject() => _projects.SelectedItem as EventItem;

    private void SelectProject(Guid id)
    {
        for (var i = 0; i < _projects.Items.Count; i++)
        {
            if (_projects.Items[i] is EventItem project && project.Id == id) _projects.SelectedIndex = i;
        }
    }

    private void SelectStep(Guid id)
    {
        for (var i = 0; i < _steps.Items.Count; i++)
        {
            if (_steps.Items[i] is EventItem step && step.Id == id) _steps.SelectedIndex = i;
        }
    }

    private void DrawProject(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _projects.Items[e.Index] is not EventItem project) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = Rectangle.Inflate(e.Bounds, -3, -5);
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var path = RoundRect(bounds, 10);
        using var back = new SolidBrush(selected ? Color.FromArgb(239, 246, 255) : CardBack);
        using var border = new Pen(selected ? Accent : Border);
        e.Graphics.FillPath(back, path);
        e.Graphics.DrawPath(border, path);
        var steps = Steps(project);
        var completed = steps.Count(IsHandled);
        using var titleFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, project.Title, titleFont,
            new Rectangle(bounds.Left + 14, bounds.Top + 10, bounds.Width - 28, 24), TextMain, TextFormatFlags.EndEllipsis);
        var meta = steps.Count == 0 ? "尚未添加步骤" : $"{completed}/{steps.Count} 步 · {(steps.Count == 0 ? 0 : completed * 100 / steps.Count)}%";
        TextRenderer.DrawText(e.Graphics, meta, Font, new Rectangle(bounds.Left + 14, bounds.Top + 38, bounds.Width - 28, 22),
            TextMuted, TextFormatFlags.EndEllipsis);
    }

    private void DrawStep(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _steps.Items[e.Index] is not EventItem step) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var canvas = new SolidBrush(CardBack);
        e.Graphics.FillRectangle(canvas, e.Bounds);
        var handled = IsHandled(step);
        var centerX = e.Bounds.Left + 24;
        if (e.Index > 0)
        {
            using var line = new Pen(handled ? Success : Border, 2);
            e.Graphics.DrawLine(line, centerX, e.Bounds.Top, centerX, e.Bounds.Top + 24);
        }
        if (e.Index < _steps.Items.Count - 1)
        {
            using var line = new Pen(handled ? Success : Border, 2);
            e.Graphics.DrawLine(line, centerX, e.Bounds.Top + 48, centerX, e.Bounds.Bottom);
        }
        using var circle = new SolidBrush(handled ? Success : Accent);
        e.Graphics.FillEllipse(circle, centerX - 16, e.Bounds.Top + 20, 32, 32);
        using var numberFont = new Font(Font.FontFamily, 9F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, (e.Index + 1).ToString(), numberFont,
            new Rectangle(centerX - 16, e.Bounds.Top + 20, 32, 32), Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var selected = (e.State & DrawItemState.Selected) != 0;
        var card = new Rectangle(e.Bounds.Left + 50, e.Bounds.Top + 7, e.Bounds.Width - 56, e.Bounds.Height - 14);
        using var path = RoundRect(card, 10);
        using var back = new SolidBrush(selected ? Color.FromArgb(239, 246, 255) : Color.FromArgb(248, 250, 252));
        using var border = new Pen(selected ? Accent : Border);
        e.Graphics.FillPath(back, path);
        e.Graphics.DrawPath(border, path);
        using var stepFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, step.Title, stepFont,
            new Rectangle(card.Left + 14, card.Top + 10, card.Width - 28, 24), handled ? TextMuted : TextMain, TextFormatFlags.EndEllipsis);
        var due = step.NextDueAt(DateTime.Now)?.ToString("MM-dd HH:mm") ?? "未设置时间";
        var state = handled ? step.StatusText : e.Index == Steps(SelectedProject()!).FindIndex(x => !IsHandled(x)) ? "下一步" : "等待前一步";
        TextRenderer.DrawText(e.Graphics, $"{due}  ·  {state}", Font,
            new Rectangle(card.Left + 14, card.Top + 40, card.Width - 28, 22), handled ? TextMuted : Accent, TextFormatFlags.EndEllipsis);
    }

    private int? PromptShiftDays()
    {
        using var dialog = new Form
        {
            Text = "顺延计划",
            Width = 380,
            Height = 210,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            Font = new Font("Microsoft YaHei UI", 9F),
            BackColor = AppBack
        };
        var value = new ModernNumericUpDown { Minimum = 1, Maximum = 365, Value = 1, Width = 100 };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(22) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.Controls.Add(HeaderBlock("顺延未完成步骤", "总体截止日期也会同步顺延。"), 0, 0);
        var input = new FlowLayoutPanel { Dock = DockStyle.Fill };
        input.Controls.Add(value);
        input.Controls.Add(new Label { Text = "天", AutoSize = true, Padding = new Padding(0, 7, 0, 0), ForeColor = TextMuted });
        root.Controls.Add(input, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var ok = Button("确认顺延", true);
        ok.DialogResult = DialogResult.OK;
        var cancel = Button("取消");
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);
        dialog.Controls.Add(root);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        L.Apply(dialog);
        return dialog.ShowDialog(FindForm()) == DialogResult.OK ? (int)value.Value : null;
    }

    private static TableLayoutPanel Card(Padding? padding = null, Padding? margin = null) => new()
    {
        Dock = DockStyle.Fill,
        BackColor = CardBack,
        Padding = padding ?? new Padding(14),
        Margin = margin ?? new Padding(0, 0, 14, 0)
    };

    private static TableLayoutPanel HeaderBlock(string title, string subtitle)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        panel.Controls.Add(Heading(title, 13F));
        panel.Controls.Add(Muted(subtitle));
        return panel;
    }

    private static Label Heading(string text, float size) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        ForeColor = TextMain,
        Font = new Font("Microsoft YaHei UI", size, FontStyle.Bold)
    };

    private static Label Muted(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        ForeColor = TextMuted
    };

    private static Button Button(string text, bool primary = false, bool danger = false)
    {
        var button = new ModernButton
        {
            Text = text,
            AutoSize = false,
            Width = primary ? 104 : 82,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : CardBack,
            ForeColor = primary ? Color.White : danger ? Color.FromArgb(220, 38, 38) : TextMain,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? Accent : danger ? Color.FromArgb(254, 202, 202) : Border;
        return button;
    }

    private static GraphicsPath RoundRect(Rectangle bounds, int radius)
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

    private sealed class ProgressLine : Control
    {
        private int _value;
        public int Value
        {
            get => _value;
            set { _value = Math.Clamp(value, 0, 100); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new Rectangle(0, 3, Math.Max(1, Width - 1), Math.Max(6, Height - 6));
            using var trackPath = RoundRect(track, track.Height / 2);
            using var trackBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
            e.Graphics.FillPath(trackBrush, trackPath);
            if (Value <= 0) return;
            var fill = new Rectangle(track.Left, track.Top, Math.Max(track.Height, track.Width * Value / 100), track.Height);
            using var fillPath = RoundRect(fill, fill.Height / 2);
            using var fillBrush = new SolidBrush(Value == 100 ? Success : Accent);
            e.Graphics.FillPath(fillBrush, fillPath);
        }
    }

    private sealed class ProjectDialog : Form
    {
        private readonly EventItem _project;
        private readonly ModernTextBox _name = new() { Dock = DockStyle.Fill, PlaceholderText = "例如：完成毕业设计" };
        private readonly ModernTextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, Height = 72, PlaceholderText = "项目目标或完成标准" };
        private readonly Button _deadlineButton = Button("选择总体截止日期");
        private DateTime? _deadline;

        public ProjectDialog(EventItem project, string title)
        {
            _project = project;
            _deadline = project.DeadlineAt;
            Text = title;
            Width = 560;
            Height = 430;
            MinimumSize = MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = AppBack;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(24) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(HeaderBlock(title, "项目负责聚合步骤，本身不会触发提醒。"), 0, 0);
            root.Controls.Add(Field("项目名称", _name), 0, 1);
            root.Controls.Add(Field("项目说明", _notes), 0, 2);
            root.Controls.Add(Field("总体截止日期", DeadlineField()), 0, 3);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, WrapContents = false };
            var save = Button("保存项目", true);
            var cancel = Button("取消");
            save.Click += (_, _) => Save();
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 4);
            Controls.Add(root);
            AcceptButton = save;
            CancelButton = cancel;
            _deadlineButton.Click += (_, _) => ChooseDeadline();
            _name.Text = project.Title;
            _notes.Text = project.Notes;
            RefreshDeadline();
            L.Apply(this);
        }

        private void ChooseDeadline()
        {
            var end = _deadline ?? DateTime.Today.AddMonths(1);
            using var picker = new DateRangePickerDialog(DateTime.Today, end, false, true, TimeSpan.Zero, end.TimeOfDay);
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            _deadline = picker.HasEnd ? picker.EndDate.Add(picker.EndTime) : null;
            RefreshDeadline();
        }

        private Control DeadlineField()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            _deadlineButton.Dock = DockStyle.Fill;
            var clear = Button("清除日期");
            clear.Dock = DockStyle.Fill;
            clear.Click += (_, _) => { _deadline = null; RefreshDeadline(); };
            panel.Controls.Add(_deadlineButton, 0, 0);
            panel.Controls.Add(clear, 1, 0);
            return panel;
        }

        private void RefreshDeadline() => _deadlineButton.Text = _deadline is null ? "未设置总体截止日期" : $"{_deadline:yyyy-MM-dd HH:mm}";

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show("请输入项目名称。", "缺少项目名称", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _project.Title = _name.Text.Trim();
            _project.Notes = _notes.Text.Trim();
            _project.DeadlineAt = _deadline;
            _project.IsProject = true;
            _project.Type = EventType.Maybe;
            _project.UpdatedAt = DateTime.Now;
            DialogResult = DialogResult.OK;
        }

        private static Control Field(string label, Control control)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(Muted(label), 0, 0);
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, 0, 1);
            return panel;
        }
    }
}
