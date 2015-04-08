// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Snoop.Infrastructure
{
    public static class ZoomerUtilities
    {
        public static UIElement CreateIfPossible(object item)
        {
            var window = item as Window;
            if (window != null && VisualTreeHelper.GetChildrenCount(window) == 1)
            {
                item = VisualTreeHelper.GetChild((Visual)item, 0);
            }
            var frameworkElement = item as FrameworkElement;
            if (frameworkElement != null)
            {
                return CreateRectangleForFrameworkElement(frameworkElement);
            }
            var visual = item as Visual;
            if (visual != null)
            {
                return CreateRectangleForVisual(visual);
            }
            var resourceDictionary = item as ResourceDictionary;
            if (resourceDictionary != null)
            {
                var stackPanel = new StackPanel();
                foreach (object value in resourceDictionary.Values)
                {
                    var element = CreateIfPossible(value);
                    if (element != null)
                    {
                        stackPanel.Children.Add(element);
                    }
                }
                return stackPanel;
            }
            var brush = item as Brush;
            if (brush != null)
            {
                var rect = new Rectangle { Width = 10, Height = 10, Fill = brush };
                return rect;
            }
            var imageSource = item as ImageSource;
            if (imageSource != null)
            {
                var image = new Image { Source = imageSource };
                return image;
            }
            return null;
        }

        private static UIElement CreateRectangleForVisual(Visual uiElement)
        {
            Rectangle rect = new Rectangle
            {
                Fill = new VisualBrush(uiElement) { Stretch = Stretch.Uniform },
                Width = 50,
                Height = 50
            };
            return rect;
        }

        private static UIElement CreateRectangleForFrameworkElement(FrameworkElement element)
        {
            Rectangle rect = new Rectangle
            {
                Fill = new VisualBrush(element) { Stretch = Stretch.Uniform }
            };

            //sometimes the actual size might be 0 despite there being a rendered visual with a size greater than 0.
            //This happens often on a custom panel (http://snoopwpf.codeplex.com/workitem/7217).
            //Having a fixed size visual brush remedies the problem.
            
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (element.ActualHeight == 0 && element.ActualWidth == 0)
            // ReSharper restore CompareOfFloatsByEqualityOperator
            {
                rect.Width = 50;
                rect.Height = 50;
            }
            else
            {
                rect.Width = element.ActualWidth;
                rect.Height = element.ActualHeight;
            }
            return rect;
        }
    }
}
