using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 感測器配置模型 (Sensor Configuration Model)
    /// 用途：定義感測器的監控規則和狀態
    /// </summary>
    public class SensorConfig : INotifyPropertyChanged
    {
        #region 配置屬性 (從 JSON 載入)

        /// <summary>
        /// 分組名稱 (例如：粉槽狀態、溫度監控)
        /// </summary>
        public string Group { get; set; } = "";

        /// <summary>
        /// PLC 裝置位址 (例如：D90, M100)
        /// </summary>
        public string Device { get; set; } = "";

        /// <summary>
        /// Bit 索引 (支援多個，以逗號分隔，例如：2,3)
        /// 空字串表示監控整個 Word
        /// </summary>
        public string Bit { get; set; } = "";

        /// <summary>
        /// 期望值 (支援多個，以逗號分隔，例如：0,0)
        /// 支援比較運算：>75, <50, ==100, !=0
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// 邏輯運算模式
        /// - AND: 所有 Bit 都符合才觸發
        /// - OR: 任一 Bit 符合就觸發
        /// - COMPARE: 數值比較 (用於 Word/DWord)
        /// </summary>
        public string Mode { get; set; } = "AND";

        /// <summary>
        /// 操作描述 (顯示在 UI 上的文字，例如：粉槽_B無粉)
        /// </summary>
        public string OperationDescription { get; set; } = "";

        #endregion

        #region 執行時狀態 (由系統自動更新)

        private bool _isActive;
        /// <summary>
        /// 當前是否處於觸發狀態 (符合條件 = true)
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    LastUpdate = DateTime.Now;
                    
                    // 記錄狀態變化時間
                    if (value)
                    {
                        AlarmTriggeredTime = DateTime.Now;
                    }
                    else
                    {
                        AlarmClearedTime = DateTime.Now;
                    }
                    
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        private DateTime _lastUpdate;
        /// <summary>
        /// 最後更新時間
        /// </summary>
        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 警報觸發時間
        /// </summary>
        public DateTime? AlarmTriggeredTime { get; set; }

        /// <summary>
        /// 警報消失時間
        /// </summary>
        public DateTime? AlarmClearedTime { get; set; }

        /// <summary>
        /// 當前讀取的原始數值 (用於除錯)
        /// </summary>
        public string CurrentValue { get; set; } = "";

        #endregion

        #region UI 綁定屬性

        /// <summary>
        /// 狀態文字 (用於顯示在 UI 上)
        /// </summary>
        public string StatusText => IsActive ? "? 異常" : "? 正常";

        /// <summary>
        /// 狀態顏色 (用於 UI 視覺化)
        /// </summary>
        public string StatusColor => IsActive ? "#FF5252" : "#4CAF50";

        #endregion

        #region INotifyPropertyChanged 實作

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
