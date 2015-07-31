// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Media;

namespace Snoop.Utilities
{
	public static class VisualTreeHelperEx
	{
		public delegate HitTestFilterBehavior EnumerateTreeFilterCallback(Visual visual, object misc);
		public delegate HitTestResultBehavior EnumerateTreeResultCallback(Visual visual, object misc);

		public static bool IsFrameworkElementName(this Visual visual, object name)
		{
			var element = visual as FrameworkElement;
			return element != null && string.CompareOrdinal(element.Name, (string)name) == 0;
		}

		public static bool IsFrameworkElementTemplatedChild(this Visual visual, object templatedParent)
		{
			var element = visual as FrameworkElement;
			return element != null && ReferenceEquals(element.TemplatedParent, templatedParent);
		}

		public static void EnumerateTree(this Visual visual, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
		    if (visual == null)
			{
				throw new ArgumentNullException(nameof(visual));
			}
		    DoEnumerateTree(visual, filterCallback, enumeratorCallback, misc);
		}

	    public static T GetAncestor<T>(this Visual visual, Visual root, Func<Visual, object, bool> predicate, object param)
			where T : Visual
		{
			var result = visual as T;
			while (visual != null && !ReferenceEquals(visual, root) && 
                (result == null || (predicate != null && !predicate(result, param))))
			{
				visual = (Visual)VisualTreeHelper.GetParent(visual);
				result = visual as T;
			}
			return result;
		}

		public static T GetAncestor<T>(Visual visual, Visual root, Func<Visual, object, bool> predicate) where T : Visual
		{
			return GetAncestor<T>(visual, root, predicate, null);
		}

		public static T GetAncestor<T>(this Visual visual, Visual root) where T : Visual
		{
			return GetAncestor<T>(visual, root, null, null);
		}

		public static T GetAncestor<T>(this Visual visual) where T : Visual
		{
			return GetAncestor<T>(visual, null, null, null);
		}

		private static bool DoEnumerateTree(Visual reference, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(reference); ++i)
			{
				var child = (Visual)VisualTreeHelper.GetChild(reference, i);

				var filterResult = HitTestFilterBehavior.Continue;
				if (filterCallback != null)
				{
					filterResult = filterCallback(child, misc);
				}

				var enumerateSelf = true;
				var enumerateChildren = true;

				switch (filterResult)
				{
					case HitTestFilterBehavior.Continue:
						break;
					case HitTestFilterBehavior.ContinueSkipChildren:
						enumerateChildren = false;
						break;
					case HitTestFilterBehavior.ContinueSkipSelf:
						enumerateSelf = false;
						break;
					case HitTestFilterBehavior.ContinueSkipSelfAndChildren:
						enumerateChildren = false;
						enumerateSelf = false;
						break;
					default:
						return false;
				}

				if
				(
					(enumerateSelf && enumeratorCallback != null && enumeratorCallback(child, misc) == HitTestResultBehavior.Stop) ||
					(enumerateChildren && !DoEnumerateTree(child, filterCallback, enumeratorCallback, misc))
				)
				{
					return false;
				}
			}

			return true;
		}
	}
}
