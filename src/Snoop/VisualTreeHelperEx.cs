// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Media;

namespace Snoop
{
	public static class VisualTreeHelperEx
	{
		public delegate HitTestFilterBehavior EnumerateTreeFilterCallback(Visual visual, object misc);
		public delegate HitTestResultBehavior EnumerateTreeResultCallback(Visual visual, object misc);

		public static bool IsFrameworkElementName(this Visual @this, object name)
		{
			var element = @this as FrameworkElement;
			return element != null && string.CompareOrdinal(element.Name, (string)name) == 0;
		}

		public static bool IsFrameworkElementTemplatedChild(this Visual @this, object templatedParent)
		{
			var element = @this as FrameworkElement;
			return element != null && ReferenceEquals(element.TemplatedParent, templatedParent);
		}

		public static void EnumerateTree(this Visual @this, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
		    if (@this == null)
			{
				throw new ArgumentNullException("this");
			}
		    DoEnumerateTree(@this, filterCallback, enumeratorCallback, misc);
		}

	    public static T GetAncestor<T>(this Visual @this, Visual root, Func<Visual, object, bool> predicate, object param)
			where T : Visual
		{
			T result = @this as T;
			while (@this != null && !ReferenceEquals(@this, root) && 
                (result == null || (predicate != null && !predicate(result, param))))
			{
				@this = (Visual)VisualTreeHelper.GetParent(@this);
				result = @this as T;
			}
			return result;
		}

		public static T GetAncestor<T>(Visual @this, Visual root, Func<Visual, object, bool> predicate) where T : Visual
		{
			return GetAncestor<T>(@this, root, predicate, null);
		}

		public static T GetAncestor<T>(this Visual @this, Visual root) where T : Visual
		{
			return GetAncestor<T>(@this, root, null, null);
		}

		public static T GetAncestor<T>(this Visual @this) where T : Visual
		{
			return GetAncestor<T>(@this, null, null, null);
		}

		private static bool DoEnumerateTree(Visual reference, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(reference); ++i)
			{
				Visual child = (Visual)VisualTreeHelper.GetChild(reference, i);

				HitTestFilterBehavior filterResult = HitTestFilterBehavior.Continue;
				if (filterCallback != null)
				{
					filterResult = filterCallback(child, misc);
				}

				bool enumerateSelf = true;
				bool enumerateChildren = true;

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
