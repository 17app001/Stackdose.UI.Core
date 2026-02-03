using Stackdose.UI.Core.Controls;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 全局製程狀態管理器 - 保持製程狀態在頁面切換時不丟失
    /// </summary>
    public static class ProcessContext
    {
        private static ProcessState _currentState = ProcessState.Idle;
        private static DateTime _processStartTime = DateTime.Now;
        private static int _batchNumber = 0;
        private static string _batchId = string.Empty;  // ?? 新增：完整批號字串
        private static int _completedCount = 0;
        private static int _defectCount = 0;
        private static bool _isDeviceInitialized = false;
        
        /// <summary>
        /// 狀態改變事件
        /// </summary>
        public static event Action<ProcessState>? StateChanged;
        
        /// <summary>
        /// 批次號碼改變事件
        /// </summary>
        public static event Action<int>? BatchNumberChanged;
        
        /// <summary>
        /// ?? 批次ID改變事件（完整字串）
        /// </summary>
        public static event Action<string>? BatchIdChanged;
        
        /// <summary>
        /// 計數改變事件
        /// </summary>
        public static event Action<int, int>? CountChanged;
        
        /// <summary>
        /// 設備初始化狀態改變事件
        /// </summary>
        public static event Action<bool>? DeviceInitializedChanged;
        
        /// <summary>
        /// 當前製程狀態
        /// </summary>
        public static ProcessState CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    StateChanged?.Invoke(_currentState);
                    
                    if (_currentState == ProcessState.Running)
                    {
                        _processStartTime = DateTime.Now;
                    }
                }
            }
        }
        
        /// <summary>
        /// 設備是否已初始化（全局狀態，App 重啟後為 false）
        /// </summary>
        public static bool IsDeviceInitialized
        {
            get => _isDeviceInitialized;
            set
            {
                if (_isDeviceInitialized != value)
                {
                    _isDeviceInitialized = value;
                    DeviceInitializedChanged?.Invoke(_isDeviceInitialized);
                }
            }
        }
        
        /// <summary>
        /// 製程開始時間
        /// </summary>
        public static DateTime ProcessStartTime => _processStartTime;
        
        /// <summary>
        /// 當前批次號碼（整數）
        /// </summary>
        public static int BatchNumber
        {
            get => _batchNumber;
            set
            {
                if (_batchNumber != value)
                {
                    _batchNumber = value;
                    BatchNumberChanged?.Invoke(_batchNumber);
                }
            }
        }
        
        /// <summary>
        /// ?? 當前批次ID（完整字串，例如 "BATCH-20260203-001"）
        /// </summary>
        public static string BatchId
        {
            get => _batchId;
            set
            {
                if (_batchId != value)
                {
                    _batchId = value ?? string.Empty;
                    BatchIdChanged?.Invoke(_batchId);
                }
            }
        }
        
        /// <summary>
        /// 完成數量
        /// </summary>
        public static int CompletedCount
        {
            get => _completedCount;
            set
            {
                if (_completedCount != value)
                {
                    _completedCount = value;
                    CountChanged?.Invoke(_completedCount, _defectCount);
                }
            }
        }
        
        /// <summary>
        /// 不良數量
        /// </summary>
        public static int DefectCount
        {
            get => _defectCount;
            set
            {
                if (_defectCount != value)
                {
                    _defectCount = value;
                    CountChanged?.Invoke(_completedCount, _defectCount);
                }
            }
        }
        
        /// <summary>
        /// 良率計算
        /// </summary>
        public static double YieldRate
        {
            get
            {
                int total = _completedCount + _defectCount;
                if (total == 0) return 100.0;
                return ((double)_completedCount / total) * 100.0;
            }
        }
        
        /// <summary>
        /// 運行時間
        /// </summary>
        public static TimeSpan RunningTime
        {
            get
            {
                if (_currentState == ProcessState.Running)
                {
                    return DateTime.Now - _processStartTime;
                }
                return TimeSpan.Zero;
            }
        }
        
        /// <summary>
        /// 重置所有狀態（包含設備初始化狀態）
        /// </summary>
        public static void Reset()
        {
            _currentState = ProcessState.Idle;
            _batchNumber = 0;
            _batchId = string.Empty;
            _completedCount = 0;
            _defectCount = 0;
            _processStartTime = DateTime.Now;
            _isDeviceInitialized = false;
            
            StateChanged?.Invoke(_currentState);
            BatchNumberChanged?.Invoke(_batchNumber);
            BatchIdChanged?.Invoke(_batchId);
            CountChanged?.Invoke(_completedCount, _defectCount);
            DeviceInitializedChanged?.Invoke(_isDeviceInitialized);
        }
    }
}
