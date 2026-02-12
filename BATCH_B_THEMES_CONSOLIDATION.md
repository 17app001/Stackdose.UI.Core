# ?? Batch B: Themes 收斂 (Theme Consolidation) - 完成?告

## ? 完成??

**完成??**: 2024-01-XX  
**目?**: 建立可重用的主?系?，提供??化 Token 和向后兼容机制

---

## ?? 新增文件

### 1. **Themes/Tokens/SemanticTokens.xaml**
- **用途**: 定???化?? Token 架构
- **?容**: 
  - Surface.* (表面背景)
  - Border.* (?框)
  - Text.* (文字)
  - Accent.* (??色)
  - Action.* (操作按?)
  - Status.* (??指示)
  - Overlay.* (遮罩?)
  - Component.* (?件特定色)

### 2. **Themes/Tokens/DarkTheme.xaml**
- **用途**: Dark 主?的具体??
- **特?**: 
  - 提供所有 Semantic Tokens 的 Dark ?色值
  - 低?比度，适合???操作

### 3. **Themes/Tokens/LightTheme.xaml**
- **用途**: Light 主?的具体??
- **特?**:
  - 提供所有 Semantic Tokens 的 Light ?色值
  - 高?比度，适合高亮?境

### 4. **Helpers/UI/ThemeLoader.cs**
- **用途**: 提供安全的主?切?机制
- **功能**:
  - ?程安全的?源字典操作
  - Design-time 安全（避免 Designer 崩?）
  - 原子性切?（要么全部成功，要么回?）
  - 主??存机制

---

## ?? 修改文件

### 1. **Themes/Colors.xaml**
- **?更?容**:
  - ? 保留原有所有?色定?（向后兼容）
  - ? 添加 Semantic Alias Tokens (v1)
  - ? 添加向后兼容 Aliases (v2) - 使用 StaticResource 映射

### 2. **Themes/LightColors.xaml**
- **?更?容**:
  - ? 保留原有所有?色定?（向后兼容）
  - ? 添加 Semantic Alias Tokens (v1)
  - ? 添加向后兼容 Aliases (v2) - 与 Colors.xaml 保持一致

### 3. **Themes/Theme.xaml**
- **?更?容**:
  - ? 添加主?加?策略注?
  - ? 添加 Design-Time Safety ?明
  - ? 保持原有?构（确保不破坏?有引用）

---

## ?? 改??清?

| 改??型 | 文件路? | 改??容 | ??等? |
|---------|---------|---------|----------|
| **新增** | `Themes/Tokens/SemanticTokens.xaml` | ??化 Token 定? | ?? 低 |
| **新增** | `Themes/Tokens/DarkTheme.xaml` | Dark 主??? | ?? 低 |
| **新增** | `Themes/Tokens/LightTheme.xaml` | Light 主??? | ?? 低 |
| **新增** | `Helpers/UI/ThemeLoader.cs` | 主?切??助? | ?? 低 |
| **修改** | `Themes/Colors.xaml` | 添加向后兼容 Alias | ?? 低 |
| **修改** | `Themes/LightColors.xaml` | 添加向后兼容 Alias | ?? 低 |
| **修改** | `Themes/Theme.xaml` | 添加注??明 | ?? 低 |

---

## ?? 改?原因 (Rationale)

### ?? (Problem)
1. **主?耦合度高**: 控件直接硬?? `Cyber.*`, `Plc.*`, `Button.*` 等?色 key
2. **?以?展**: 新增主?需要修改大量控件
3. **缺乏??化**: ?色命名不清楚用途（如 `Cyber.Bg.Dark` 是?面背景?是面板背景？）
4. **主?切?不安全**: 缺少原子性保?和回?机制

### 解?方案 (Solution)
1. **建立??化 Token ?**: 使用 `Surface.*`, `Text.*`, `Action.*` 等??化命名
2. **向后兼容机制**: 保留? key，使用 `StaticResource` 映射到新 Token
3. **安全切?机制**: 提供 `ThemeLoader` 确保主?切?的原子性和安全性
4. **清晰分?架构**: Token 定? → 主??? → ?件?式

### 优? (Benefits)
- ? **可重用性**: 新控件使用 Semantic Tokens，自?支持所有主?
- ? **可??性**: 主?修改只需修改 DarkTheme.xaml / LightTheme.xaml
- ? **向后兼容**: ?控件?需修改，??工作
- ? **Design-Time 安全**: XAML Designer 不?崩?

---

## ?? ???估 (Risk Assessment)

### ?? 低??
- **新增文件**: 不影??有代?
- **Alias 映射**: 使用 `StaticResource` 保?向后兼容
- **ThemeLoader**: 完全?立的?助?，不?制使用

### ?? 中??
- **主?切??机**: 如果在控件初始化前切?主?，可能?致?源未找到
  - **?解措施**: ThemeLoader 提供?加?机制
  
### ?? 高??
- **?** (本次重构?添加新功能，不修改核心??)

---

## ? ?收方式 (Acceptance Criteria)

### 1. ????
```powershell
# 在?目根目??行
dotnet build Stackdose.UI.Core/Stackdose.UI.Core.csproj
```
**?期?果**: ?????

### 2. XAML Designer ??
```
1. 打?任意含有 ThemeResource 的 XAML 文件 (如 CyberFrame.xaml)
2. ?查 XAML Designer 是否正常渲染
3. 切?主?（如果有切?按?）
4. 确?控件?色正确更新
```
**?期?果**: Designer 不崩?，控件正常?示

### 3. ?行???
```csharp
// 在 App.xaml.cs 或 MainWindow.xaml.cs 中??

// ?? 1: ?加?主?
ThemeLoader.PreloadThemes();

// ?? 2: 切?到 Light 主?
bool success = ThemeLoader.SwitchTheme(ThemeType.Light);
Assert.IsTrue(success);

// ?? 3: ???前主?
Assert.AreEqual(ThemeType.Light, ThemeLoader.GetCurrentThemeType());

// ?? 4: 切?回 Dark 主?
success = ThemeLoader.SwitchTheme(ThemeType.Dark);
Assert.IsTrue(success);
```
**?期?果**: 所有?言通?，UI 正确更新

### 4. 向后兼容性??
```xaml
<!-- ???的 key 是否仍然有效 -->
<Border Background="{DynamicResource Cyber.Bg.Panel}">
    <TextBlock Foreground="{DynamicResource Plc.Text.Value}" Text="TEST"/>
</Border>
```
**?期?果**: 控件正常?示，?色正确

### 5. 新 Token ??
```xaml
<!-- ??新的 Semantic Tokens -->
<Border Background="{DynamicResource Surface.Bg.Panel}">
    <TextBlock Foreground="{DynamicResource Text.Primary}" Text="TEST"/>
</Border>
```
**?期?果**: 控件正常?示，?色正确

---

## ?? ?移指南 (Migration Guide)

### ?于新控件
**推荐使用 Semantic Tokens**:

```xaml
<!-- ? 不推荐 (?方式) -->
<Border Background="{DynamicResource Cyber.Bg.Panel}">
    <TextBlock Foreground="{DynamicResource Plc.Text.Value}"/>
</Border>

<!-- ? 推荐 (新方式) -->
<Border Background="{DynamicResource Surface.Bg.Panel}">
    <TextBlock Foreground="{DynamicResource Text.Primary}"/>
</Border>
```

### ?于?控件
**?需修改**，?有代???工作：

```xaml
<!-- ? ??有效 (向后兼容) -->
<Border Background="{DynamicResource Cyber.Bg.Panel}">
    <TextBlock Foreground="{DynamicResource Plc.Text.Value}"/>
</Border>
```

### Semantic Token 映射表

| ? Key | 新 Token | 用途 |
|--------|----------|------|
| `Cyber.Bg.Dark` | `Surface.Bg.Page` | ?面背景 |
| `Cyber.Bg.Panel` | `Surface.Bg.Panel` | 面板背景 |
| `Cyber.Bg.Card` | `Surface.Bg.Card` | 卡片背景 |
| `Plc.Bg.Main` | `Surface.Bg.Control` | 控件背景 |
| `Cyber.Border` | `Surface.Border.Default` | 默??框 |
| `Cyber.Border.Strong` | `Surface.Border.Strong` | ???框 |
| `Cyber.Text.Main` | `Text.Primary` | 主要文字 |
| `Cyber.Text.Muted` | `Text.Secondary` | 次要文字 |
| `Plc.Text.Default` | `Text.Tertiary` | 第三?文字 |
| `Cyber.NeonBlue` | `Accent.Primary` | 主要??色 |
| `Button.Bg.Primary` | `Action.Primary.Bg` | 主要按?背景 |
| `Button.Bg.Success` | `Action.Success.Bg` | 成功按?背景 |

---

## ?? 下一步 (Next Steps)

### Batch C: Controls Base 抽象化
- 目?: 提取通用控件基?
- ?期收益: ?少重复代?，?一控件行?

### Batch D: Services 接口化
- 目?: ? Services ?接口化
- ?期收益: 提高可??性，支持依?注入

---

## ?? ?注 (Notes)

### ???策??
1. **?什么使用 StaticResource 而非 DynamicResource 做 Alias?**
   - `StaticResource` 在???解析，性能更好
   - Alias 本身不需要?行?切?（切??生在被引用的?源?）

2. **?什么保留 Colors.xaml 和 LightColors.xaml?**
   - 向后兼容：?有控件直接引用?些文件
   - ??式?移：允?新?并存

3. **?什么不在 Theme.xaml 中直接引用 Tokens/?**
   - Design-Time 安全：避免 Designer 因找不到?源而崩?
   - ?行?切?：通? ThemeLoader ??加?

---

## ? Batch B 完成确?

- [x] 所有新增文件已?建
- [x] 所有修改文件已更新
- [x] 向后兼容性已??
- [x] ?????
- [x] XAML Designer 正常工作
- [x] 文?已完成

**??**: ? **已完成**  
**批准者**: [Your Name]  
**日期**: 2024-01-XX
