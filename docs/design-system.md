# Timarker 设计系统

所有 WinForms 页面统一通过 `UiTokens` 和 `ModernUi` 使用视觉规范，禁止在页面内重新定义通用蓝色、灰色、圆角或控件高度。

## 设计原则

1. 颜色表达语义，不作为无意义装饰。
2. 页面采用“标题—说明—内容—操作”层级。
3. 普通控件统一 36px 基准高度，并随字体设置放大。
4. 卡片使用 14px 圆角，输入和按钮使用 8～10px 圆角。
5. 浅色、深色和字体缩放必须使用同一份组件代码。

## Token

- 字体：8.5 / 9 / 10 / 12 / 18 / 28
- 间距：4 / 8 / 12 / 16 / 20 / 24
- 圆角：8 / 10 / 14
- 高度：32（紧凑）/ 36（标准）/ 40（强调）
- 语义色：Primary / Success / Warning / Danger / Info / Violet
- 表面色：AppBackground / Surface / SurfaceSubtle / Field / Hover / Selected / Border

## 使用规则

- 页面标题使用 `UiTokens.TextPageTitle`。
- 区块标题使用 `UiTokens.TextSection` 或 `TextEmphasis` 加粗。
- 辅助说明只使用 `TextMuted`。
- 主操作使用 `Primary`，破坏性操作使用 `Danger`。
- 输入框、按钮和下拉框不得直接使用系统默认边框。
