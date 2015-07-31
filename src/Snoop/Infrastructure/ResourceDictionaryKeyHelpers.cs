// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Snoop.Infrastructure
{
	public static class ResourceDictionaryKeyHelpers
	{
		public static string GetKeyOfResourceItem(DependencyObject dependencyObject, DependencyProperty dp)
		{
			if (dependencyObject == null || dp == null)
			{
				return string.Empty;
			}

			var resourceItem = dependencyObject.GetValue(dp);
			if (resourceItem != null)
			{
				// Walk up the visual tree, looking for the resourceItem in each frameworkElement's resource dictionary.
				while (dependencyObject != null)
				{
					var frameworkElement = dependencyObject as FrameworkElement;
                    if (frameworkElement != null)
                    {
                        var resourceKey = GetKeyInResourceDictionary(frameworkElement.Resources, resourceItem);
                        if (resourceKey != null)
                        {
                            return resourceKey;
                        }
                    }
                    else
                    {
                        break;
                    }

					dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
				}

				// check the application resources
				if (Application.Current != null)
				{
					var resourceKey = GetKeyInResourceDictionary(Application.Current.Resources, resourceItem);
					if (resourceKey != null)
						return resourceKey;
				}
			}
			return string.Empty;
		}

		public static string GetKeyInResourceDictionary(ResourceDictionary dictionary, object resourceItem)
		{
			foreach (var key in dictionary.Keys)
			{
				if (dictionary[key] == resourceItem)
				{
					return key.ToString();
				}
			}

		    return dictionary.MergedDictionaries.Select(dic => GetKeyInResourceDictionary(dic, resourceItem))
		            .FirstOrDefault(name => !string.IsNullOrEmpty(name));
		}
	}
}
