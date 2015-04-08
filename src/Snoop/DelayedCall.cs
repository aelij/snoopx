// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
	public delegate void DelayedHandler();

	public class DelayedCall
	{
		public DelayedCall(DelayedHandler handler, DispatcherPriority priority)
		{
			_handler = handler;
			_priority = priority;
		}

		public void Enqueue()
		{
			if (!_queued)
			{
				_queued = true;

				Dispatcher dispatcher;
				if (Application.Current == null || SnoopModes.MultipleDispatcherMode)
					dispatcher = Dispatcher.CurrentDispatcher;
				else
					dispatcher = Application.Current.Dispatcher;

				dispatcher.BeginInvoke(_priority, new DispatcherOperationCallback(Process), null);
			}
		}


		private object Process(object arg)
		{
			_queued = false;

			_handler();

			return null;
		}

		private readonly DelayedHandler _handler;
		private readonly DispatcherPriority _priority;

		private bool _queued;
	}
}
