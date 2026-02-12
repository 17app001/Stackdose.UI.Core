using System;
using System.Windows;
using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls.Base
{
    /// <summary>
    /// PLC 相關控件基類 - 提供統一的 PLC 連接管理
    /// </summary>
    /// <remarks>
    /// <para>提供的功能：</para>
    /// <list type="bullet">
    /// <item>自動綁定到全域或指定的 PlcStatus</item>
    /// <item>自動訂閱/取消訂閱 PLC 事件</item>
    /// <item>統一的連線狀態處理</item>
    /// <item>執行緒安全的 PLC 操作</item>
    /// </list>
    /// </remarks>
    public abstract class PlcControlBase : CyberControlBase
    {
        #region Private Fields

        /// <summary>已訂閱的 PlcStatus 實例</summary>
        private PlcStatus? _subscribedStatus;

        /// <summary>是否已註冊到 PLC Context</summary>
        private bool _isRegisteredToContext = false;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// PLC Manager 實例（可選）
        /// </summary>
        public static readonly DependencyProperty PlcManagerProperty =
            DependencyProperty.Register(
                nameof(PlcManager),
                typeof(IPlcManager),
                typeof(PlcControlBase),
                new PropertyMetadata(null));

        public IPlcManager? PlcManager
        {
            get => (IPlcManager?)GetValue(PlcManagerProperty);
            set => SetValue(PlcManagerProperty, value);
        }

        /// <summary>
        /// 綁定目標 PLC
        /// </summary>
        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register(
                nameof(TargetStatus),
                typeof(PlcStatus),
                typeof(PlcControlBase),
                new PropertyMetadata(null, OnTargetStatusChanged));

        public PlcStatus? TargetStatus
        {
            get => (PlcStatus?)GetValue(TargetStatusProperty);
            set => SetValue(TargetStatusProperty, value);
        }

        #endregion

        #region Lifecycle Override

        protected override void OnControlLoaded()
        {
            base.OnControlLoaded();

            // 嘗試綁定到 PLC
            TryBindToPlc();

            // 呼叫子類初始化
            OnPlcControlLoaded();
        }

        protected override void OnControlUnloaded()
        {
            // 取消 PLC 訂閱
            UnsubscribeFromPlc();

            // 呼叫子類清理
            OnPlcControlUnloaded();

            base.OnControlUnloaded();
        }

        /// <summary>
        /// 子類實作：PLC 控件載入時的初始化邏輯
        /// </summary>
        protected virtual void OnPlcControlLoaded()
        {
            // 子類覆寫此方法實作自訂初始化邏輯
        }

        /// <summary>
        /// 子類實作：PLC 控件卸載時的清理邏輯
        /// </summary>
        protected virtual void OnPlcControlUnloaded()
        {
            // 子類覆寫此方法實作自訂清理邏輯
        }

        #endregion

        #region PLC Connection Management

        /// <summary>
        /// 嘗試綁定到 PLC
        /// </summary>
        private void TryBindToPlc()
        {
            try
            {
                // 1. 檢查是否有直接指定的 TargetStatus
                if (TargetStatus != null)
                {
                    BindToPlcStatus(TargetStatus);
                    return;
                }

                // 2. 嘗試從 PlcContext 取得
                var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
                if (contextStatus != null)
                {
                    BindToPlcStatus(contextStatus);

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Bound to PlcStatus from Context");
                    #endif
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] No PlcStatus available");
                    #endif
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] TryBindToPlc error: {ex.Message}");
                #endif
            }
        }

        /// <summary>
        /// 綁定到指定的 PlcStatus
        /// </summary>
        private void BindToPlcStatus(PlcStatus? status)
        {
            if (status == null)
            {
                return;
            }

            // 先取消舊的訂閱
            UnsubscribeFromPlc();

            _subscribedStatus = status;

            // 訂閱事件
            _subscribedStatus.ConnectionEstablished += OnPlcConnectionEstablished;
            _subscribedStatus.ScanUpdated += OnPlcScanUpdated;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Subscribed to PlcStatus, IsConnected={status.CurrentManager?.IsConnected}");
            #endif

            // 如果已連線，立即通知子類
            if (_subscribedStatus.CurrentManager != null && _subscribedStatus.CurrentManager.IsConnected)
            {
                OnPlcConnectionEstablished(_subscribedStatus.CurrentManager);
            }
        }

        /// <summary>
        /// 取消 PLC 訂閱
        /// </summary>
        private void UnsubscribeFromPlc()
        {
            if (_subscribedStatus != null)
            {
                _subscribedStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
                _subscribedStatus.ScanUpdated -= OnPlcScanUpdated;
                _subscribedStatus = null;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Unsubscribed from PlcStatus");
                #endif
            }
        }

        /// <summary>
        /// PLC 連線建立時的處理
        /// </summary>
        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            SafeInvoke(() =>
            {
                try
                {
                    OnPlcConnected(manager);
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] OnPlcConnected error: {ex.Message}");
                    #endif
                }
            });
        }

        /// <summary>
        /// PLC 掃描更新時的處理
        /// </summary>
        private void OnPlcScanUpdated(IPlcManager manager)
        {
            SafeInvoke(() =>
            {
                try
                {
                    OnPlcDataUpdated(manager);
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] OnPlcDataUpdated error: {ex.Message}");
                    #endif
                }
            });
        }

        /// <summary>
        /// TargetStatus 變更時的回呼
        /// </summary>
        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcControlBase control && e.NewValue is PlcStatus newStatus)
            {
                control.BindToPlcStatus(newStatus);
            }
        }

        #endregion

        #region Virtual Methods for子類Override

        /// <summary>
        /// 子類實作：PLC 連線建立時的處理邏輯
        /// </summary>
        /// <param name="manager">PlcManager 實例</param>
        protected virtual void OnPlcConnected(IPlcManager manager)
        {
            // 子類覆寫此方法實作連線成功後的邏輯
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] PLC connected");
            #endif
        }

        /// <summary>
        /// 子類實作：PLC 數據更新時的處理邏輯
        /// </summary>
        /// <param name="manager">PlcManager 實例</param>
        protected virtual void OnPlcDataUpdated(IPlcManager manager)
        {
            // 子類覆寫此方法實作數據更新邏輯
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 取得當前的 PlcManager
        /// </summary>
        protected IPlcManager? GetPlcManager()
        {
            return PlcManager ?? _subscribedStatus?.CurrentManager ?? PlcContext.GlobalStatus?.CurrentManager;
        }

        /// <summary>
        /// 檢查 PLC 是否已連線
        /// </summary>
        protected bool IsPlcConnected()
        {
            var manager = GetPlcManager();
            return manager != null && manager.IsConnected;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 取得已訂閱的 PlcStatus
        /// </summary>
        protected PlcStatus? SubscribedStatus => _subscribedStatus;

        #endregion
    }
}
