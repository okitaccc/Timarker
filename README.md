# 事刻 Timarker

> 把重要的事，放进时间里。

Timarker 是一款面向 Windows 的本地优先时间与事项提醒应用。它把普通事项、周期事项、生日、纪念日和提醒规则放在同一套界面中，并通过日历、收藏夹、悬浮倒计时和番茄钟帮助你查看与处理它们。

当前项目处于早期测试阶段，适合个人使用、功能体验和反馈，不建议用于无法容忍提醒遗漏的关键场景。

## 功能

- 创建普通事项、周期事项、生日与纪念日
- 设置开始时间、截止时间、提前提醒和再次提醒
- 支持阳历、农历、闰月生日和农历日期显示
- 在“今日与未来”中按状态筛选和搜索
- 使用月历查看事项，并快速跳转年月
- 使用词条组织事项，按词条创建收藏夹
- 在日历、列表和收藏夹中编辑或删除事项
- 使用悬浮倒计时关注临近事项
- 使用内置番茄钟进行专注计时
- 支持简体中文与 English
- 本地原子保存、自动备份与损坏恢复

## 数据与隐私

Timarker 当前不需要账号，也不会主动上传事项数据。数据保存在：

```text
%LocalAppData%\Timarker
```

其中包括：

```text
events.json        事项数据
events.bak         最近一次可用副本
Backups\           自动滚动备份，最多保留 10 份
settings.json      应用设置
```

从旧版 Timeline 升级时，应用会在首次启动时将原数据目录迁移到 Timarker。卸载应用默认保留这些数据，方便重新安装后继续使用。

## 安装

1. 前往仓库的 **Releases** 页面。
2. 下载最新的 `Timarker-Setup-<版本号>.exe`。
3. 运行安装程序并按提示完成安装。

当前安装包尚未进行商业代码签名，Windows 可能显示 SmartScreen 提示。请只从本仓库的 Releases 页面获取安装包。

卸载路径：

```text
Windows 设置 → 应用 → 已安装的应用 → 事刻 Timarker → 卸载
```

## 从源码运行

环境要求：

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
cd src\Timarker
dotnet run
```

## 构建安装包

除 .NET 8 SDK 外，还需要安装 [Inno Setup](https://jrsoftware.org/isdl.php)。然后在项目根目录运行：

```powershell
.\build-installer.ps1 -Version 0.1.0
```

生成结果：

```text
artifacts\installer\Timarker-Setup-0.1.0.exe
```

## 项目结构

```text
Timarker/
├─ src/Timarker/             WinForms 应用
│  ├─ Models/                事项与设置模型
│  ├─ Services/              保存、备份与提醒服务
│  └─ *.cs                   主界面和各功能视图
├─ installer/                Inno Setup 安装脚本
├─ docs/                     架构说明
└─ build-installer.ps1       发布与安装包构建脚本
```

## 反馈

如果遇到提醒重复、日期计算错误、数据恢复失败或界面显示问题，请在 GitHub Issues 中提交：

- Timarker 版本号
- Windows 版本
- 复现步骤
- 预期结果与实际结果
- 必要时附上截图；请先遮盖私人事项内容

请不要公开上传 `%LocalAppData%\Timarker` 中的完整数据文件，其中可能包含私人事项。

## 当前计划

- 完善周期事项与项目/组合事件
- 增加应用内更新检查
- 完善数据版本迁移
- 持续补齐英文界面与可访问性

