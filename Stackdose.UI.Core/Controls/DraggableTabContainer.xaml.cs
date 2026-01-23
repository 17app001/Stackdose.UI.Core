using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Stackdose.UI.Core.Controls
{
    [ContentProperty(nameof(Tabs))]
    public partial class DraggableTabContainer : UserControl
    {
        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();

        private Point _dragStart;

        public DraggableTabContainer()
        {
            InitializeComponent();
            
            // Bind Tabs collection to TabControl
            PART_TabControl.ItemsSource = Tabs;
            
            // Monitor collection changes
            Tabs.CollectionChanged += Tabs_CollectionChanged;
            
            this.Loaded += DraggableTabContainer_Loaded;
        }

        private void Tabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Ensure first tab is selected if none selected
            if (Tabs.Any() && PART_TabControl.SelectedIndex < 0)
            {
                PART_TabControl.SelectedIndex = 0;
            }
        }

        private void DraggableTabContainer_Loaded(object sender, RoutedEventArgs e)
        {
            // Skip PLC context setup in design mode
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            
            // Set default attached PlcContext status inheritance if parent has one
            try
            {
                var parentStatus = Stackdose.UI.Core.Helpers.PlcContext.GetStatus(this);
                if (parentStatus != null)
                {
                    foreach (var t in Tabs)
                    {
                        if (t.Content is FrameworkElement fe) 
                            fe.SetValue(Stackdose.UI.Core.Helpers.PlcContext.StatusProperty, parentStatus);
                    }
                }
            }
            catch { }
        }

        private void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var tabItem = GetTabItemUnderMouse();
                if (tabItem?.DataContext is TabViewModel vm)
                {
                    var payload = new DragPayload { Tab = vm, Source = this };
                    var dataObj = new DataObject();
                    dataObj.SetData(typeof(DragPayload), payload);
                    DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Move);
                }
            }
        }

        private TabItem? GetTabItemUnderMouse()
        {
            var pt = Mouse.GetPosition(PART_TabControl);
            var hit = PART_TabControl.InputHitTest(pt) as DependencyObject;
            while (hit != null && !(hit is TabItem)) 
                hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
            return hit as TabItem;
        }

        private void TabControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(DragPayload)) is DragPayload dp && dp.Tab != null)
            {
                if (dp.Source == this)
                {
                    // Reorder within same container
                    var targetTab = GetTabItemUnderMouse();
                    if (targetTab?.DataContext is TabViewModel targetVm)
                    {
                        int fromIndex = Tabs.IndexOf(dp.Tab);
                        int toIndex = Tabs.IndexOf(targetVm);
                        if (fromIndex >= 0 && toIndex >= 0 && fromIndex != toIndex)
                        {
                            Tabs.Move(fromIndex, toIndex);
                        }
                    }
                }
                else if (dp.Source != null)
                {
                    // Move from another container
                    dp.Source.RemoveTab(dp.Tab);
                    AddExternalTab(dp.Tab);
                }
            }
        }

        public bool RemoveTab(TabViewModel tab)
        {
            if (tab != null && Tabs.Contains(tab))
            {
                Tabs.Remove(tab);
                return true;
            }
            return false;
        }

        public void AddExternalTab(TabViewModel tab)
        {
            if (tab != null)
            {
                Tabs.Add(tab);
                PART_TabControl.SelectedItem = tab;
            }
        }
    }
}
