// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Controls;

namespace Snoop.MethodsTab
{
    public class SnoopParameterInformationTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element == null)
                return null;

            var snoopParameterInfo = item as SnoopParameterInformation;
            if (snoopParameterInfo == null)
                return null;

            if (snoopParameterInfo.IsEnum || snoopParameterInfo.ParameterType == typeof(bool))
            {
                return element.FindResource("EnumParameterTemplate") as DataTemplate;
            }
            if (snoopParameterInfo.ParameterType == typeof(DependencyProperty))
            {
                return element.FindResource("DependencyPropertyTemplate") as DataTemplate;
            }
            if (snoopParameterInfo.IsCustom)
            {
                return element.FindResource("UnknownParameterTemplate") as DataTemplate;
            }
            return element.FindResource("DefaultParameterTemplate") as DataTemplate;
        }
    }
}
