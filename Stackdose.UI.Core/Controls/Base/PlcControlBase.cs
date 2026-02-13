using System;
using System.Windows;
using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls.Base
{
    /// <summary>
    /// PLC ����������� - ���ѲΤ@�� PLC �s���޲z
    /// </summary>
    /// <remarks>
    /// <para>���Ѫ��\��G</para>
    /// <list type="bullet">
    /// <item>�۰ʸj�w�����Ϋ��w�� PlcStatus</item>
    /// <item>�۰ʭq�\/�����q�\ PLC �ƥ�</item>
    /// <item>�Τ@���s�u���A�B�z</item>
    /// <item>������w���� PLC �ާ@</item>
    /// </list>
    /// </remarks>
    public abstract class PlcControlBase : CyberControlBase
    {
        #region Private Fields

        /// <summary>�w�q�\�� PlcStatus ���</summary>
        private PlcStatus? _subscribedStatus;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// PLC Manager ��ҡ]�i��^
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
        /// �j�w�ؼ� PLC
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

            // ���ոj�w�� PLC
            TryBindToPlc();

            // �I�s�l����l��
            OnPlcControlLoaded();
        }

        protected override void OnControlUnloaded()
        {
            // ���� PLC �q�\
            UnsubscribeFromPlc();

            // �I�s�l���M�z
            OnPlcControlUnloaded();

            base.OnControlUnloaded();
        }

        /// <summary>
        /// �l����@�GPLC ������J�ɪ���l���޿�
        /// </summary>
        protected virtual void OnPlcControlLoaded()
        {
            // �l���мg����k��@�ۭq��l���޿�
        }

        /// <summary>
        /// �l����@�GPLC ��������ɪ��M�z�޿�
        /// </summary>
        protected virtual void OnPlcControlUnloaded()
        {
            // �l���мg����k��@�ۭq�M�z�޿�
        }

        #endregion

        #region PLC Connection Management

        /// <summary>
        /// ���ոj�w�� PLC
        /// </summary>
        private void TryBindToPlc()
        {
            try
            {
                // 1. �ˬd�O�_���������w�� TargetStatus
                if (TargetStatus != null)
                {
                    BindToPlcStatus(TargetStatus);
                    return;
                }

                // 2. ���ձq PlcContext ���o
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
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] TryBindToPlc error");
                #endif
            }
        }

        /// <summary>
        /// �j�w����w�� PlcStatus
        /// </summary>
        private void BindToPlcStatus(PlcStatus? status)
        {
            if (status == null)
            {
                return;
            }

            // �������ª��q�\
            UnsubscribeFromPlc();

            _subscribedStatus = status;

            // �q�\�ƥ�
            _subscribedStatus.ConnectionEstablished += OnPlcConnectionEstablished;
            _subscribedStatus.ScanUpdated += OnPlcScanUpdated;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Subscribed to PlcStatus, IsConnected={status.CurrentManager?.IsConnected}");
            #endif

            // �p�G�w�s�u�A�ߧY�q���l��
            if (_subscribedStatus.CurrentManager != null && _subscribedStatus.CurrentManager.IsConnected)
            {
                OnPlcConnectionEstablished(_subscribedStatus.CurrentManager);
            }
        }

        /// <summary>
        /// ���� PLC �q�\
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
        /// PLC �s�u�إ߮ɪ��B�z
        /// </summary>
        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            SafeInvoke(() =>
            {
                try
                {
                    OnPlcConnected(manager);
                }
                catch (Exception)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] OnPlcConnected error");
                    #endif
                }
            });
        }

        /// <summary>
        /// PLC ���y��s�ɪ��B�z
        /// </summary>
        private void OnPlcScanUpdated(IPlcManager manager)
        {
            SafeInvoke(() =>
            {
                try
                {
                    OnPlcDataUpdated(manager);
                }
                catch (Exception)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] OnPlcDataUpdated error");
                    #endif
                }
            });
        }

        /// <summary>
        /// TargetStatus �ܧ�ɪ��^�I
        /// </summary>
        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcControlBase control && e.NewValue is PlcStatus newStatus)
            {
                control.BindToPlcStatus(newStatus);
            }
        }

        #endregion

        #region Virtual Methods for�l��Override

        /// <summary>
        /// �l����@�GPLC �s�u�إ߮ɪ��B�z�޿�
        /// </summary>
        /// <param name="manager">PlcManager ���</param>
        protected virtual void OnPlcConnected(IPlcManager manager)
        {
            // �l���мg����k��@�s�u���\�᪺�޿�
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] PLC connected");
            #endif
        }

        /// <summary>
        /// �l����@�GPLC �ƾڧ�s�ɪ��B�z�޿�
        /// </summary>
        /// <param name="manager">PlcManager ���</param>
        protected virtual void OnPlcDataUpdated(IPlcManager manager)
        {
            // �l���мg����k��@�ƾڧ�s�޿�
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// ���o���e�� PlcManager
        /// </summary>
        protected IPlcManager? GetPlcManager()
        {
            return PlcManager ?? _subscribedStatus?.CurrentManager ?? PlcContext.GlobalStatus?.CurrentManager;
        }

        /// <summary>
        /// �ˬd PLC �O�_�w�s�u
        /// </summary>
        protected bool IsPlcConnected()
        {
            var manager = GetPlcManager();
            return manager != null && manager.IsConnected;
        }

        #endregion

        #region Properties

        /// <summary>
        /// ���o�w�q�\�� PlcStatus
        /// </summary>
        protected PlcStatus? SubscribedStatus => _subscribedStatus;

        #endregion
    }
}
