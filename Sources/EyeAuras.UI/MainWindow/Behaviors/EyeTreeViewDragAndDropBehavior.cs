using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.Services;
using EyeAuras.UI.MainWindow.ViewModels;
using PoeShared.UI.TreeView;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TreeView = System.Windows.Controls.TreeView;

namespace EyeAuras.UI.MainWindow.Behaviors
{
    internal sealed class EyeTreeViewDragAndDropBehavior : Behavior<System.Windows.Controls.TreeView>
    {
        public Point? StartPoint { get; set; }
        
        public bool IsDragging { get; set; }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(ObservableCollection<IAuraTabViewModel>), typeof(EyeTreeViewDragAndDropBehavior), new PropertyMetadata(default(ObservableCollection<IAuraTabViewModel>)));

        public ObservableCollection<IAuraTabViewModel> ItemsSource
        {
            get { return (ObservableCollection<IAuraTabViewModel>) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObjectOnMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp += AssociatedObjectOnMouseLeftButtonUp;
            AssociatedObject.DragOver += AssociatedObjectOnDragOver;
            AssociatedObject.Drop +=AssociatedObjectOnDrop;
            AssociatedObject.PreviewMouseMove += AssociatedObjectOnPreviewMouseMove;
            AssociatedObject.AllowDrop = true;
        }

        private void AssociatedObjectOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(AssociatedObject);

            if (StartPoint == null || IsDragging)
            {
                return;
            }
            
            var currentRect = new Size(Math.Abs(point.X - StartPoint.Value.X), Math.Abs(point.Y - StartPoint.Value.Y));

            if (!(currentRect.Width >= SystemInformation.DragSize.Height) &&
                !(currentRect.Height >= SystemInformation.DragSize.Width))
            {
                return;
            }

            var input = AssociatedObject.InputHitTest(point);
            if (!TryFindTreeItem(AssociatedObject, out var node, input as DependencyObject) || node.DataContext == null)
            {
                return;
            }

            IsDragging = true;
            DragDrop.DoDragDrop(AssociatedObject, node.DataContext, DragDropEffects.Move);
            IsDragging = false;
        }

        private void AssociatedObjectOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StartPoint = null;
        }

        private void AssociatedObjectOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartPoint = e.GetPosition(AssociatedObject);
        }

        private bool TryFindTreeItem(TreeView treeView,
            out TreeViewItem pItemNode,
            DependencyObject source)
        {
            pItemNode = null;
            var k = source;
            while (k != null)
            {
                if (k is TreeViewItem treeNode)
                {
                    if (treeNode.DataContext is ITreeViewItemViewModel)
                    {
                        pItemNode = treeNode;
                        return true;
                    }
                } 
                else if (k == treeView)
                {
                    return false;
                }

                k = VisualTreeHelper.GetParent(k);
            }

            return false;
        }

        private bool TryFindDropTarget(
            TreeView treeView,
            out TreeViewItem pItemNode,
            DragEventArgs pDragEventArgs)
        {
            pItemNode = null;

            var k = VisualTreeHelper.HitTest(treeView, pDragEventArgs.GetPosition(treeView)).VisualHit;
            return TryFindTreeItem(treeView, out pItemNode, k);
        }

        private void AssociatedObjectOnDrop(object sender, DragEventArgs e)
        {
            if (!TryFindDropTarget(AssociatedObject, out var itemNode, e))
            {
                return;
            }
            StartPoint = null;

            {
                if (itemNode.DataContext is HolderTreeViewItemViewModel targetNode && e.Data.GetData(typeof(HolderTreeViewItemViewModel)) is HolderTreeViewItemViewModel sourceNode && targetNode != sourceNode)
                {
                    sourceNode.Parent = targetNode.Parent;
                }
            }
            {
                if (itemNode.DataContext is DirectoryTreeViewItemViewModel targetNode && e.Data.GetData(typeof(HolderTreeViewItemViewModel)) is HolderTreeViewItemViewModel sourceNode)
                {
                    sourceNode.Parent = targetNode;
                }
            }
            {
                if (itemNode.DataContext is DirectoryTreeViewItemViewModel targetNode && e.Data.GetData(typeof(DirectoryTreeViewItemViewModel)) is DirectoryTreeViewItemViewModel sourceNode && targetNode != sourceNode)
                {
                    var parents = targetNode.FindParents();
                    if (!parents.Contains(sourceNode))
                    {
                        sourceNode.Parent = targetNode;
                    }
                }
            }
            {
                if (itemNode.DataContext is HolderTreeViewItemViewModel targetNode && e.Data.GetData(typeof(DirectoryTreeViewItemViewModel)) is DirectoryTreeViewItemViewModel sourceNode)
                {
                    sourceNode.Parent = targetNode.Parent;
                }
            }
        }

        private void AssociatedObjectOnDragOver(object sender, DragEventArgs e)
        {
            if (!TryFindDropTarget(AssociatedObject, out var itemNode, e))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = DragDropEffects.Move;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.AllowDrop = false;
            base.OnDetaching();
        }
    }
}