// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Windows;

namespace Snoop
{
	public abstract class ResourceContainerItem : VisualTreeItem
	{
		public ResourceContainerItem(object target, VisualTreeItem parent): base(target, parent)
		{
		}

		protected abstract ResourceDictionary ResourceDictionary { get; }

		protected override void Reload(List<VisualTreeItem> toBeRemoved)
		{
			base.Reload(toBeRemoved);

			ResourceDictionary resources = ResourceDictionary;

			if (resources != null && resources.Count != 0)
			{
				bool foundItem = false;
				foreach (VisualTreeItem item in toBeRemoved)
				{
					if (item.Target == resources)
					{
						toBeRemoved.Remove(item);
						item.Reload();
						foundItem = true;
						break;
					}
				}
				if (!foundItem)
					Children.Add(Construct(resources, this));
			}
		}
	}
}
