using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Controls.Base
{
    /// <summary>
    /// �i���α������ - ���ѳq�ΥͩR�g���޲z�M�D�D�P��
    /// </summary>
    /// <remarks>
    /// <para>���Ѫ��\��G</para>
    /// <list type="bullet">
    /// <item>�۰ʳ]�p���˴��]�קK Designer �Y��^</item>
    /// <item>�Τ@�� Loaded/Unloaded �ͩR�g���޲z</item>
    /// <item>�D�D�P���۰ʵ��U/���P</item>
    /// <item>�u�{�w���� Dispatcher �ާ@</item>
    /// <item>�귽�M�z�޲z</item>
    /// </list>
    /// 
    /// <para>�ϥΤ覡�G</para>
    /// <code>
    /// public partial class MyControl : CyberControlBase
    /// {
    ///     protected override void OnControlLoaded()
    ///     {
    ///         // �A����l���޿�
    ///     }
    ///     
    ///     protected override void OnControlUnloaded()
    ///     {
    ///         // �A���M�z�޿�
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class CyberControlBase : UserControl, IThemeAware, IDisposable
    {
        #region Private Fields

        /// <summary>�O�_�w���J</summary>
        private bool _isLoaded = false;

        /// <summary>�O�_�wdisposed</summary>
        private bool _disposed = false;

        /// <summary>�O�_�b�]�p�Ҧ��U</summary>
        private readonly bool _isInDesignMode;

        #endregion

        #region Constructor

        /// <summary>
        /// �����غc���
        /// </summary>
        protected CyberControlBase()
        {
            // �˴��]�p�Ҧ�
            _isInDesignMode = DesignerProperties.GetIsInDesignMode(this);

            // �]�p�Ҧ��U����l��
            if (_isInDesignMode)
            {
                return;
            }

            // ���U�ͩR�g���ƥ�
            this.Loaded += CyberControlBase_Loaded;
            this.Unloaded += CyberControlBase_Unloaded;
        }

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// ������J�ɪ��Τ@�B�z
        /// </summary>
        private void CyberControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInDesignMode || _isLoaded)
            {
                return;
            }

            _isLoaded = true;

            try
            {
                // 1. ���U�� ThemeManager
                if (this is IThemeAware)
                {
                    Helpers.ThemeManager.Register(this);
                }

                // 2. �I�s�l������l���޿�
                OnControlLoaded();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Control loaded");
                #endif
            }
            catch (Exception ex)
            {
                OnLoadError(ex);
            }
        }

        /// <summary>
        /// ��������ɪ��Τ@�B�z
        /// </summary>
        private void CyberControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_isInDesignMode || !_isLoaded)
            {
                return;
            }

            try
            {
                // 1. �I�s�l�����M�z�޿�
                OnControlUnloaded();

                // 2. �q ThemeManager ���P
                if (this is IThemeAware)
                {
                    Helpers.ThemeManager.Unregister(this);
                }

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Control unloaded");
                #endif
            }
            catch (Exception ex)
            {
                OnUnloadError(ex);
            }

            _isLoaded = false;
        }

        /// <summary>
        /// �l����@�G������J�ɪ���l���޿�
        /// </summary>
        protected virtual void OnControlLoaded()
        {
            // �l���мg����k��@�ۭq��l���޿�
        }

        /// <summary>
        /// �l����@�G��������ɪ��M�z�޿�
        /// </summary>
        protected virtual void OnControlUnloaded()
        {
            // �l���мg����k��@�ۭq�M�z�޿�
        }

        /// <summary>
        /// �l����@�G���J�ɵo�Ϳ��~���B�z�޿�
        /// </summary>
        protected virtual void OnLoadError(Exception ex)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Load error: {ex.Message}");
            #endif
        }

        /// <summary>
        /// �l����@�G�����ɵo�Ϳ��~���B�z�޿�
        /// </summary>
        protected virtual void OnUnloadError(Exception ex)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Unload error: {ex.Message}");
            #endif
        }

        #endregion

        #region IThemeAware Implementation

        /// <summary>
        /// �D�D�ܧ�ɪ��B�z�]�w�]�Ź�@�^
        /// </summary>
        /// <param name="e">�D�D�ܧ�ƥ�Ѽ�</param>
        /// <remarks>
        /// �l���p�ݳB�z�D�D�ܧ�A�мg����k
        /// </remarks>
        public virtual void OnThemeChanged(ThemeChangedEventArgs e)
        {
            // �l���мg����k��@�D�D�ܧ��޿�
        }

        #endregion

        #region Dispatcher Helpers

        /// <summary>
        /// �w������ UI �ާ@�]�۰ʤ����� UI ������^
        /// </summary>
        /// <param name="action">�n���檺�ާ@</param>
        protected void SafeInvoke(Action action)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }

                if (Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Dispatcher.Invoke(action);
                }
            }
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] SafeInvoke error");
                #endif
            }
        }

        /// <summary>
        /// �w������ UI �ާ@�]�D�P�B�^
        /// </summary>
        /// <param name="action">�n���檺�ާ@</param>
        protected void SafeBeginInvoke(Action action)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }

                Dispatcher.BeginInvoke(action);
            }
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] SafeBeginInvoke error");
                #endif
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// ����귽
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ����귽�]�i�мg�^
        /// </summary>
        /// <param name="disposing">�O�_���������</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // ���� Managed �귽
                try
                {
                    // �����ƥ�q�\
                    this.Loaded -= CyberControlBase_Loaded;
                    this.Unloaded -= CyberControlBase_Unloaded;

                    // �q ThemeManager ���P
                    if (this is IThemeAware)
                    {
                        Helpers.ThemeManager.Unregister(this);
                    }

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Disposed");
                    #endif
                }
                catch (Exception)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Dispose error");
                    #endif
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// �Ѻc���
        /// </summary>
        ~CyberControlBase()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// ���o����O�_�w���J
        /// </summary>
        protected bool IsControlLoaded => _isLoaded;

        /// <summary>
        /// ���o�O�_�b�]�p�Ҧ��U
        /// </summary>
        protected bool IsInDesignMode => _isInDesignMode;

        /// <summary>
        /// ���o����O�_�wdisposed
        /// </summary>
        protected bool IsDisposed => _disposed;

        #endregion
    }
}
