// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snoop
{
	public class ValueEditorTemplateSelector : DataTemplateSelector
	{
		public DataTemplate StandardTemplate { get; set; }

	    public DataTemplate EnumTemplate { get; set; }

	    public DataTemplate BoolTemplate { get; set; }

	    public DataTemplate BrushTemplate { get; set; }


	    public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var property = (PropertyInformation)item;

			if (property.PropertyType.IsEnum)
				return EnumTemplate;
		    if (property.PropertyType == typeof(bool))
		        return BoolTemplate;
		    if ( property.PropertyType.IsGenericType 
		         && Nullable.GetUnderlyingType( property.PropertyType ) == typeof(bool) )
		        return BoolTemplate;
		    if (typeof(Brush).IsAssignableFrom(property.PropertyType))
		        return BrushTemplate;

		    return StandardTemplate;
		}
	}
}
