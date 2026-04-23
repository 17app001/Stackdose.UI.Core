namespace Stackdose.UI.Core.Models;

/// <summary>
/// 控制項定義最小介面：BehaviorEngine 僅依賴此介面，不依賴 DesignerItemDefinition 具體類別。
/// 讓 ShellShared 與 MachinePageDesigner 之間不產生循環依賴。
/// </summary>
public interface IControlWithBehaviors
{
    string Id { get; }
    List<BehaviorEvent> Events { get; }
}
