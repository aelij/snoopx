// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Snoop.Utilities
{
	/// <summary>
	/// Class that shows all the routed events occurring on a visual.
	/// VERY dangerous (cannot unregister for the events) and doesn't work all that great.
	/// Stay far away from this code :)
	/// </summary>
	public class EventsListener
	{
		public EventsListener(Visual visual)
		{
			_current = this;
			_visual = visual;

			var type = visual.GetType();

			// Cannot unregister for events once we've registered, so keep the registration simple and only do it once.
			for (var baseType = type; baseType != null; baseType = baseType.BaseType)
			{
				if (!_registeredTypes.ContainsKey(baseType))
				{
					_registeredTypes[baseType] = baseType;

					var routedEvents = EventManager.GetRoutedEventsForOwner(baseType);
					if (routedEvents != null)
					{
						foreach (var routedEvent in routedEvents)
							EventManager.RegisterClassHandler(baseType, routedEvent, new RoutedEventHandler(HandleEvent), true);
					}
				}
			}
		}

		public ObservableCollection<EventInformation> Events { get; } = new ObservableCollection<EventInformation>();

	    public static string Filter
		{
			get { return _filter; }
			set
			{
				_filter = value;
			    _filter = _filter?.ToLower();
			}
		}

		public static void Stop()
		{
			_current = null;
		}


		private static void HandleEvent(object sender, RoutedEventArgs e)
		{
			if (_current != null && ReferenceEquals(sender, _current._visual))
			{
				if (string.IsNullOrEmpty(Filter) || e.RoutedEvent.Name.ToLower().Contains(Filter))
				{
					_current.Events.Add(new EventInformation(e));

					while (_current.Events.Count > 100)
						_current.Events.RemoveAt(0);
				}
			}
		}

        private readonly Visual _visual;
        
        private static EventsListener _current;
		private static readonly Dictionary<Type, Type> _registeredTypes = new Dictionary<Type, Type>();
		private static string _filter;
	}

	public class EventInformation
	{
		public EventInformation(RoutedEventArgs evt)
		{
			_evt = evt;
		}

		public IEnumerable Properties => PropertyInformation.GetProperties(_evt);

	    public override string ToString()
		{
			return $"{_evt.RoutedEvent.Name} Handled: {_evt.Handled} OriginalSource: {_evt.OriginalSource}";
		}

		private readonly RoutedEventArgs _evt;
	}
}
