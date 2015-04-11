// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.ComponentModel;
using System.Windows;

namespace Snoop
{
	public class PertinentPropertyFilter
	{
        private readonly FrameworkElement _element;

		public PertinentPropertyFilter(object target)
		{
			_element = target as FrameworkElement;
		}

		public bool Filter(PropertyDescriptor property)
		{
		    if (_element == null)
		    {
		        return true;
		    }

			// Filter the 20 stylistic set properties that I've never seen used.
		    if (property.Name.Contains("Typography.StylisticSet"))
		    {
		        return false;
		    }

			var attachedPropertyForChildren = (AttachedPropertyBrowsableForChildrenAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForChildrenAttribute)];
			var attachedPropertyForType = (AttachedPropertyBrowsableForTypeAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForTypeAttribute)];
			var attachedPropertyForAttribute = (AttachedPropertyBrowsableWhenAttributePresentAttribute)property.Attributes[typeof(AttachedPropertyBrowsableWhenAttributePresentAttribute)];

			if (attachedPropertyForChildren != null)
			{
				var descriptor = DependencyPropertyDescriptor.FromProperty(property);
			    if (descriptor == null)
			    {
			        return false;
			    }

				var localElement = _element;
				do
				{
					localElement = localElement.Parent as FrameworkElement;
					if (localElement != null && descriptor.DependencyProperty.OwnerType.IsInstanceOfType(localElement))
						return true;
				}
				while (attachedPropertyForChildren.IncludeDescendants && localElement != null);
				return false;
			}
		    if (attachedPropertyForType != null)
		    {
		        // when using [AttachedPropertyBrowsableForType(typeof(IMyInterface))] and IMyInterface is not a DependencyObject, Snoop crashes.
		        // see http://snoopwpf.codeplex.com/workitem/6712

		        if (attachedPropertyForType.TargetType.IsSubclassOf(typeof(DependencyObject)))
		        {
		            DependencyObjectType doType = DependencyObjectType.FromSystemType(attachedPropertyForType.TargetType);
		            if (doType.IsInstanceOfType(_element))
		            {
		                return true;
		            }
		        }

		        return false;
		    }
		    if (attachedPropertyForAttribute == null)
		    {
		        return true;
		    }

		    var dependentAttribute = TypeDescriptor.GetAttributes(_element)[attachedPropertyForAttribute.AttributeType];
		    return dependentAttribute != null && !dependentAttribute.IsDefaultAttribute();
		}
	}
}
