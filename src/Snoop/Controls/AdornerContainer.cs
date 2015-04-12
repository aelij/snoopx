// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Snoop.Controls
{
	/// <summary>
	/// Simple helper class to allow any UIElements to be used as an Adorner.
	/// </summary>
	public class AdornerContainer : Adorner
	{
		public AdornerContainer(UIElement adornedElement): base(adornedElement)
		{
		}

		protected override int VisualChildrenCount
		{
			get { return _child == null ? 0 : 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index == 0 && _child != null)
				return _child;
			return base.GetVisualChild(index);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_child != null)
				_child.Arrange(new Rect(finalSize));
			return finalSize;
		}

		public UIElement Child
		{
			get { return _child; }
			set
			{
				AddVisualChild(value);
				_child = value;
			}
		}
		private UIElement _child;
	}
}
