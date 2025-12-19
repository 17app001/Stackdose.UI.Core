# PlcLabel Light 模式配色指南

## ?? Light 模式推薦配色

### 1?? 淺灰底 + 橙色文字（警告）
```xml
<Custom:PlcLabel Label="溫度" 
                Address="D100"
                ShowFrame="True"
                FrameBackground="DarkBlue"
                LabelForeground="Warning"
                ValueForeground="Warning"/>
```
**Light 模式效果：**
- 底框：#F5F5F5（淺灰）
- 文字：#FF9800（橙色）
- 適合：溫度、壓力等需要注意的參數

---

### 2?? 白色底 + 藍色文字（資訊）
```xml
<Custom:PlcLabel Label="流量" 
                Address="D200"
                ShowFrame="True"
                FrameBackground="White"
                LabelForeground="Info"
                ValueForeground="Info"/>
```
**Light 模式效果：**
- 底框：#FFFFFF（白色）
- 文字：#2196F3（藍色）
- 適合：一般數據顯示

---

### 3?? 成功色底 + 白色文字（狀態）
```xml
<Custom:PlcLabel Label="正常" 
                Address="M100"
                ShowFrame="True"
                FrameBackground="Success"
                LabelForeground="White"
                ValueForeground="White"/>
```
**效果：** 綠色底色在 Light/Dark 模式下都清晰

---

## ?? Light vs Dark 配色對照

| 元素 | Dark 模式 | Light 模式 |
|------|-----------|-----------|
| DarkBlue 底框 | #1E1E2E（深藍） | #F5F5F5（淺灰）|
| Warning 文字 | #FF9800（橙色）| #FF9800（橙色）|
| Info 文字 | #00BCD4（青色）| #2196F3（藍色）|
| Success 底 | #4CAF50（綠色）| #4CAF50（綠色）|

---

## ?? 使用建議

**ShowFrame="False" 時：**
- 適合疊加在其他元件上
- 文字需要高對比色

**ShowFrame="True" 時：**
- 使用 `FrameBackground="DarkBlue"` 自動適應主題
- Light 模式：淺灰底 + 深色文字
- Dark 模式：深藍底 + 亮色文字
