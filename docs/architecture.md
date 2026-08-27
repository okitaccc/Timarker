# Timarker architecture

Timarker 是一个本地优先的 Windows WinForms 应用，核心关系为：

```text
EventItem + Time rule + Reminder rule + State/occurrence log
```

## Modules

- `Models/EventItem.cs`：事项类型、状态、周期、生日和纪念日计算。
- `Services/EventStore.cs`：JSON 原子保存、回退副本和滚动备份。
- `Services/SettingsStore.cs`：用户设置与开机启动。
- `Services/ReminderEngine.cs`：到期检测和提醒触发。
- `MainForm.cs`：主窗口、创建入口、今日与未来以及内嵌功能视图。
- `EventCardRenderer.cs`：应用内统一的事项卡片模板。
- `Localization.cs`：简体中文与英文界面词典。

## Local data

```text
%LocalAppData%\Timarker\events.json
%LocalAppData%\Timarker\events.bak
%LocalAppData%\Timarker\Backups\events-*.json
%LocalAppData%\Timarker\settings.json
```

存储文档包含 `SchemaVersion`。旧版 `%LocalAppData%\Timeline` 会在首次启动时迁移到新目录。

## Reminder loop

```text
MainForm timer
  -> ReminderEngine checks due events
  -> ReminderDialog handles complete/snooze/postpone/ignore
  -> EventStore saves state atomically
```

## Deliberate limits

- 单机、本地 JSON 存储。
- 暂无账号、云同步和跨设备通知协调。
- 插件位于 `%LocalAppData%\Timarker\Plugins`，每个插件包含 `plugin.json` 和实现 `ITimarkerPlugin` 的 .NET 程序集；插件属于受信任的本机代码，不提供进程级沙箱。
- 暂无通用工作流引擎。
- 项目/组合事件继续复用 `EventItem`，在确有复杂度前不拆分额外领域层。
