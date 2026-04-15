# Platform 跨 Repo 契約文件

> 記錄 UI.Core 對 Stackdose.Platform 的依賴契約。
> **Platform 修改前請先確認以下介面是否受影響，避免 UI.Core 編譯失敗。**

---

## 概覽

`Stackdose.Platform` 是另一個獨立 Git Repo（`../Stackdose.Platform/`），UI.Core 透過 `ProjectReference` 直接引用其原始碼。
兩邊**沒有版號隔離**，Platform 改動會立即影響 UI.Core 編譯。

---

## 高危介面清單（改了 UI.Core 一定爆）

### IPlcManager（`Stackdose.Abstractions/Hardware/IPlcManager.cs`）

```csharp
public interface IPlcManager : IDisposable
{
    bool IsConnected { get; }
    IPlcClient PlcClient { get; }
    IPlcMonitor? Monitor { get; }

    Task<bool> InitializeAsync(string ip, int port, int intervalMs = 150, int retry = 1);
    Task DisconnectAsync();
    Task<bool> WriteAsync(string deviceInput);
    Task<int> ReadAsync(string deviceInput);
    short? ReadWord(string device);
    int? ReadDWord(string device);
    bool? ReadBit(string device);
    Task<bool> WriteValuesWithSumAsync(string device, int startAddress, short[] values, int checkSumAddress = 9, bool checkSum = false);
    Task<bool> WriteRecipeWithSumAsync(string device, int startAddress, short[] values, bool checkSum = false);
    event Action<string>? ScanElapsedChanged;
    Task<Dictionary<string, short?>> ReadBatchAsync(IEnumerable<string> addresses);
}
```

**UI.Core 使用點：** `PlcContext`、`ProcessCommandService`、`SecuredButton`、所有 Plc* 控制項
**危險操作：** 修改 `WriteAsync` / `ReadAsync` 簽名、移除 `Monitor` 屬性

---

### IPlcMonitor（`Stackdose.Abstractions/Hardware/IPlcMonitor.cs`）

```csharp
public interface IPlcMonitor : IDisposable
{
    bool IsRunning { get; }
    event Action<Dictionary<string, short>>? WordBatchChanged;
    event Action<Dictionary<string, bool>>? BitBatchChanged;
    event Action<string, bool>? BitChanged;
    event Action<string, short>? WordChanged;
    Action<string>? OnUpdateElapsed { get; set; }

    void Register(string address, int count = 1);
    void Start();
    void Stop();
    short? GetWord(string addr);
    bool? GetBit(string addr);
    bool? GetBit(string addr, int bit);
    int? GetDWord(string addr);
    void RegisterContinuousAddresses(IEnumerable<string> devices, int tolerance = 3);
}
```

**UI.Core 使用點：** `PlcLabel`、`PlcStatus`、`PlcStatusIndicator`（訂閱 `WordChanged`/`BitChanged`）
**危險操作：** 移除 `WordChanged`/`BitChanged` 事件、修改事件簽名

---

### IPlcClient（`Stackdose.Abstractions/Hardware/IPlcClient.cs`）

```csharp
public interface IPlcClient : IDisposable
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(string ip, int port);
    Task DisconnectAsync();
    Task<short[]> ReadWordsAsync(string device, int start, int length);
    Task<short> ReadWordAsync(string device, int address);
    Task<int> ReadDWordAsync(string device, int address);
    Task<bool> ReadBitAsync(string device, int address, int bitPos = 0);
    Task WriteWordsAsync(string device, int start, short[] values);
    Task WriteWordAsync(string device, int address, short value);
    Task WriteDWordAsync(string device, int address, int value);
    Task<bool> WriteBitAsync(string device, int address, int bitPos, int value);
    Task<bool> WriteBitAsync(string device, int address, int value);
    Task<bool> WriteDeviceValueAsync(string inputText);
    Task<bool> WriteValuesWithSumAsync(...);
    Task<bool> WriteRecipeWithSumAsync(...);
}
```

**危險操作：** 修改任何 Read/Write 方法簽名

---

## 中危介面清單（改了部分功能受影響）

### IPrintHead（`Stackdose.Abstractions/Print/IPrintHead.cs`）
- **UI.Core 使用點：** `PrintHeadController`、`PrintHeadPanel`
- **關鍵成員：** `Connect()`, `LoadImage()`, `StartPrint()`, `ConnectionState`, `ConnectionStateChanged`

### IPrintHeadManager（`Stackdose.Abstractions/Print/IPrintHeadManager.cs`）
- **UI.Core 使用點：** `PlcContext`（若有 PrintHead 配置）

### IMachineController（`Stackdose.Abstractions/Machine/IMachineController.cs`）
- **UI.Core 使用點：** 部分 Demo 與 DeviceFramework
- **關鍵：** `IPlcManager PlcManager` 屬性、`MachineState` enum

### ILogService（`Stackdose.Abstractions/Logging/ILogService.cs`）
- **UI.Core 使用點：** `ComplianceContext` 底層
- **介面很小**（`Log` + `LogException`），異動風險低

---

## 跨 Repo 同步規則

### Platform 修改後，UI.Core 需要做的事
| Platform 變動類型 | UI.Core 動作 |
|---|---|
| 新增介面方法 | 確認 UI.Core 實作類別是否需要補實作 |
| 修改現有方法簽名 | 搜尋 UI.Core 所有呼叫點並更新 |
| 新增 enum 值 | 確認 UI.Core switch 語句是否需要新增 case |
| 新增整個介面 | UI.Core 通常不需動，除非主動要用 |

### Devlog 標記方式
跨 Repo 的異動在 devlog 中標記為 `[Platform ↔ UI.Core]`，例如：
```
[Platform ↔ UI.Core] Platform 更新 IPlcManager.ReadBatchAsync 回傳型別，UI.Core PlcContext 同步調整
```

---

## Platform 最後已知狀態

| 項目 | 內容 |
|---|---|
| 最後 commit | 2026-03-23 `662e15a 完成第一輪優化` |
| 穩定版本 | Phase 1-3 優化完成（PrintHead、Logging、架構重整） |
| 主要異動 | 2026-03-20 大規模優化，`IPlcManager.ReadBatchAsync` 加入 |
