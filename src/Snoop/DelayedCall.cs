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
        private readonly DelayedHandler _handler;
        private readonly DispatcherPriority _priority;

        private bool _queued;

		public DelayedCall(DelayedHandler handler, DispatcherPriority priority)
		{
			_handler = handler;
			_priority = priority;
		}

		public void Enqueue()
		{
		    if (_queued) return;
		    _queued = true;

		    var dispatcher = Application.Current == null || SnoopModes.MultipleDispatcherMode
		        ? Dispatcher.CurrentDispatcher
		        : Application.Current.Dispatcher;

		    dispatcher.BeginInvoke(_priority, new DispatcherOperationCallback(Process), null);
		}


		private object Process(object arg)
		{
			_queued = false;

			_handler();

			return null;
		}
	}
}
