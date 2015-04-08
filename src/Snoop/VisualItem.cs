// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Snoop
{
    /// <summary>
    /// Main class that represents a visual in the visual tree
    /// </summary>
    public class VisualItem : ResourceContainerItem
    {
        private AdornerContainer _adorner;

        public VisualItem(Visual visual, VisualTreeItem parent)
            : base(visual, parent)
        {
            Visual = visual;
        }

        public Visual Visual { get; private set; }

        public override bool HasBindingError
        {
            get
            {
                var propertyDescriptors =
                    TypeDescriptor.GetProperties(Visual, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

                var bindingExpressions =
                    from PropertyDescriptor property in propertyDescriptors
                    select DependencyPropertyDescriptor.FromProperty(property)
                    into descriptor
                    where descriptor != null
                    select BindingOperations.GetBindingExpressionBase(Visual, descriptor.DependencyProperty);

                return bindingExpressions.Any(expression =>
                            expression != null && (expression.HasError || expression.Status != BindingStatus.Active));
            }
        }

        public override Visual MainVisual
        {
            get { return Visual; }
        }

        public override Brush TreeBackgroundBrush
        {
            get { return Brushes.Transparent; }
        }

        public override Brush VisualBrush
        {
            get
            {
                var brush = new VisualBrush(Visual) { Stretch = Stretch.Uniform };
                return brush;
            }
        }

        protected override ResourceDictionary ResourceDictionary
        {
            get
            {
                FrameworkElement element = Visual as FrameworkElement;
                if (element != null)
                    return element.Resources;
                return null;
            }
        }

        protected override void OnSelectionChanged()
        {
            // Add adorners for the visual this is representing.
            var adorners = AdornerLayer.GetAdornerLayer(Visual);
            var visualElement = Visual as UIElement;

            Visual root = null;
            if (visualElement != null)
            {
                root = FindRoot(Visual);

                ((UIElement)root).RemoveHandler(UIElement.KeyUpEvent, new KeyEventHandler(OnKeyUp));
            }

            if (adorners != null && visualElement != null)
            {
                if (IsSelected && _adorner == null)
                {
                    ((UIElement)root).AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(OnKeyUp), true);

                    _adorner = new AdornerContainer(visualElement)
                    {
                        Child = new Border
                        {
                            BorderThickness = new Thickness(4),
                            BorderBrush = new SolidColorBrush(new Color { ScA = .3f, ScR = 1 }),
                            IsHitTestVisible = false
                        }
                    };
                    adorners.Add(_adorner);
                }
                else if (_adorner != null)
                {
                    adorners.Remove(_adorner);
                    _adorner.Child = null;
                    _adorner = null;
                }
            }
        }

        private int _lastKeyUpTime;
        private Key _lastUpDown;

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            const Key targetKey = Key.LeftCtrl;

            if (keyEventArgs.Key == targetKey && _lastUpDown == targetKey &&
                keyEventArgs.Timestamp - _lastKeyUpTime < 2000)
            {
                Print();
            }
            _lastUpDown = keyEventArgs.Key;
            _lastKeyUpTime = keyEventArgs.Timestamp;
        }

        private void Print()
        {
            var root = FindRoot(Visual);
            var dialog = new ScreenshotDialog { DataContext = root };
            dialog.ShowDialog();
        }

        private static Visual FindRoot(Visual visual)
        {
            while (true)
            {
                var parent = (Visual)VisualTreeHelper.GetParent(visual);
                if (parent == null)
                {
                    return visual;
                }
                visual = parent;
            }
        }

        protected override void Reload(List<VisualTreeItem> toBeRemoved)
        {
            // having the call to base.Reload here ... puts the application resources at the very top of the tree view.
            // this used to be at the bottom. putting it here makes it consistent (and easier to use) with ApplicationTreeItem
            base.Reload(toBeRemoved);

            // remove items that are no longer in tree, add new ones.
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Visual); i++)
            {
                var child = VisualTreeHelper.GetChild(Visual, i);
                if (child != null)
                {
                    bool foundItem = false;
                    foreach (var item in toBeRemoved)
                    {
                        if (ReferenceEquals(item.Target, child))
                        {
                            toBeRemoved.Remove(item);
                            item.Reload();
                            foundItem = true;
                            break;
                        }
                    }
                    if (!foundItem)
                    {
                        Children.Add(Construct(child, this));
                    }
                }
            }

            var grid = Visual as Grid;
            if (grid != null)
            {
                foreach (var row in grid.RowDefinitions)
                {
                    Children.Add(Construct(row, this));
                }
                foreach (var column in grid.ColumnDefinitions)
                {
                    Children.Add(Construct(column, this));
                }
            }
        }
    }
}
