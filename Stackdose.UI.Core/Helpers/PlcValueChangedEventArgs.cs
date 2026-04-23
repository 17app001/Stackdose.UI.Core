using System;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PLC 控件值變更事件參數（統一型別，供所有 PlcControlBase 子類使用）
    /// </summary>
    public class PlcValueChangedEventArgs : EventArgs
    {
        /// <summary>原始數值（未格式化）</summary>
        public object? RawValue { get; }

        /// <summary>原始數值（相容舊版 API）</summary>
        public object? Value => RawValue;

        /// <summary>格式化後的顯示文字</summary>
        public string DisplayText { get; }

        /// <summary>來源控件所監聽的 PLC 位址（可為空，例如多位址的 AlarmViewer）</summary>
        public string? Address { get; }

        public PlcValueChangedEventArgs(object? rawValue, string displayText, string? address = null)
        {
            RawValue = rawValue;
            DisplayText = displayText;
            Address = address;
        }
    }
}
