// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop.Utilities
{
	public class DelayedCall
	{
        private readonly Action _handler;
        private readonly DispatcherPriority _priority;

        private bool _queued;

		public DelayedCall(Action handler, DispatcherPriority priority)
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

		    dispatcher.InvokeAsync(() =>
		    {
		        _queued = false;
		        _handler();
		    }, _priority);
		}
	}
}
