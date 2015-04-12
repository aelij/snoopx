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

			Type type = visual.GetType();

			// Cannot unregister for events once we've registered, so keep the registration simple and only do it once.
			for (Type baseType = type; baseType != null; baseType = baseType.BaseType)
			{
				if (!_registeredTypes.ContainsKey(baseType))
				{
					_registeredTypes[baseType] = baseType;

					RoutedEvent[] routedEvents = EventManager.GetRoutedEventsForOwner(baseType);
					if (routedEvents != null)
					{
						foreach (RoutedEvent routedEvent in routedEvents)
							EventManager.RegisterClassHandler(baseType, routedEvent, new RoutedEventHandler(HandleEvent), true);
					}
				}
			}
		}

		public ObservableCollection<EventInformation> Events
		{
			get { return _events; }
		}
		private readonly ObservableCollection<EventInformation> _events = new ObservableCollection<EventInformation>();

		public static string Filter
		{
			get { return _filter; }
			set
			{
				_filter = value;
				if (_filter != null)
					_filter = _filter.ToLower();
			}
		}

		public static void Stop()
		{
			_current = null;
		}


		private static void HandleEvent(object sender, RoutedEventArgs e)
		{
			if (_current != null && sender == _current._visual)
			{
				if (string.IsNullOrEmpty(Filter) || e.RoutedEvent.Name.ToLower().Contains(Filter))
				{
					_current._events.Add(new EventInformation(e));

					while (_current._events.Count > 100)
						_current._events.RemoveAt(0);
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

		public IEnumerable Properties
		{
			get { return PropertyInformation.GetProperties(_evt); }
		}

		public override string ToString()
		{
			return string.Format("{0} Handled: {1} OriginalSource: {2}", _evt.RoutedEvent.Name, _evt.Handled, _evt.OriginalSource);
		}

		private readonly RoutedEventArgs _evt;
	}
}
