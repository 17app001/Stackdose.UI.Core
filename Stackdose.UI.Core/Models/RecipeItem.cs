namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// Recipe 項目資料模型
    /// 代表單一 Recipe 參數項目
    /// </summary>
    public class RecipeItem
    {
        /// <summary>
        /// 參數名稱 (例如: "Temperature", "Pressure")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// PLC 位址 (例如: "D100", "M200")
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 參數值 (字串格式,可轉換為數值或布林)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 資料類型 (例如: "Int16", "Int32", "Float", "Bool", "String")
        /// </summary>
        public string DataType { get; set; } = "Int16";

        /// <summary>
        /// 單位 (例如: "°C", "bar", "%")
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 描述說明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 最小值 (用於驗證)
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// 最大值 (用於驗證)
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// 是否啟用 (可用於暫時停用某些參數)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 驗證值是否在有效範圍內
        /// </summary>
        public bool IsValueInRange(double value)
        {
            if (MinValue.HasValue && value < MinValue.Value) return false;
            if (MaxValue.HasValue && value > MaxValue.Value) return false;
            return true;
        }

        public override string ToString()
        {
            return $"{Name} ({Address}) = {Value} {Unit}";
        }
    }
}
