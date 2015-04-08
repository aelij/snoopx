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
		public DataTemplate StandardTemplate
		{
			get { return _standardTemplate; }
			set { _standardTemplate = value; }
		}
		private DataTemplate _standardTemplate;

		public DataTemplate EnumTemplate
		{
			get { return _enumTemplate; }
			set { _enumTemplate = value; }
		}
		private DataTemplate _enumTemplate;

		public DataTemplate BoolTemplate
		{
			get { return _boolTemplate; }
			set { _boolTemplate = value; }
		}
		private DataTemplate _boolTemplate;

		public DataTemplate BrushTemplate
		{
			get { return _brushTemplate; }
			set { _brushTemplate = value; }
		}
		private DataTemplate _brushTemplate;


		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			PropertyInformation property = (PropertyInformation)item;

			if (property.PropertyType.IsEnum)
				return EnumTemplate;
		    if (property.PropertyType.Equals(typeof(bool)))
		        return BoolTemplate;
		    if ( property.PropertyType.IsGenericType 
		         && Nullable.GetUnderlyingType( property.PropertyType ) == typeof(bool) )
		        return BoolTemplate;
		    if (typeof(Brush).IsAssignableFrom(property.PropertyType))
		        return _brushTemplate;

		    return StandardTemplate;
		}
	}
}
