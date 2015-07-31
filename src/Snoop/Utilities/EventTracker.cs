// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using Snoop.Annotations;

namespace Snoop.Utilities
{
	public delegate void EventTrackerHandler(TrackedEvent newEvent);

	/// <summary>
	/// Random class that tries to determine what element handled a specific event.
	/// Doesn't work too well in the end, because the static ClassHandler doesn't get called
	/// in a consistent order.
	/// </summary>
	public class EventTracker : INotifyPropertyChanged, IComparable
	{
		public EventTracker(Type targetType, RoutedEvent routedEvent)
		{
			_targetType = targetType;
			RoutedEvent = routedEvent;
		}


		public event EventTrackerHandler EventHandled;


		public bool IsEnabled
		{
			get { return _isEnabled; }
			set
			{
				if (_isEnabled != value)
				{
					_isEnabled = value;
					if (_isEnabled && !_everEnabled)
					{
						_everEnabled = true;
						EventManager.RegisterClassHandler(_targetType, RoutedEvent, new RoutedEventHandler(HandleEvent), true);
					}
					OnPropertyChanged();
				}
			}
		}
		private bool _isEnabled;

		public RoutedEvent RoutedEvent { get; }

	    public string Category => RoutedEvent.OwnerType.Name;

	    public string Name => RoutedEvent.Name;


	    private void HandleEvent(object sender, RoutedEventArgs e) 
		{
			// Try to figure out what element handled the event. Not precise.
			if (_isEnabled) 
			{
				var entry = new EventEntry(sender, e.Handled);
				if (_currentEvent != null && _currentEvent.EventArgs == e) 
				{
					_currentEvent.AddEventEntry(entry);
				}
				else 
				{
					_currentEvent = new TrackedEvent(e, entry);
					OnEventHandled(_currentEvent);
				}
			}
		}

		private TrackedEvent _currentEvent;
		private bool _everEnabled;
		private readonly Type _targetType;


		#region IComparable Members
		public int CompareTo(object obj)
		{
			var otherTracker = obj as EventTracker;
		    if (otherTracker == null)
		    {
		        return 1;
		    }

		    return Category == otherTracker.Category
		        ? string.CompareOrdinal(RoutedEvent.Name, otherTracker.RoutedEvent.Name)
		        : string.CompareOrdinal(Category, otherTracker.Category);
		}
		#endregion

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

	    protected void OnEventHandled(TrackedEvent newevent)
	    {
	        var handler = EventHandled;
	        handler?.Invoke(newevent);
	    }

	    [NotifyPropertyChangedInvocator]
	    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	    {
	        var handler = PropertyChanged;
	        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }
	}


	[DebuggerDisplay("TrackedEvent: {EventArgs}")]
	public class TrackedEvent : INotifyPropertyChanged
	{
		public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator)
		{
			EventArgs = routedEventArgs;
			AddEventEntry(originator);
		}


		public RoutedEventArgs EventArgs { get; }

	    public EventEntry Originator => Stack[0];

	    public bool Handled
		{
			get { return _handled; }
			set
			{
				_handled = value;
				OnPropertyChanged("Handled");
			}
		}
		private bool _handled;

		public object HandledBy
		{
			get { return _handledBy; }
			set
			{
				_handledBy = value;
				OnPropertyChanged("HandledBy");
			}
		}
		private object _handledBy;

		public ObservableCollection<EventEntry> Stack { get; } = new ObservableCollection<EventEntry>();


	    public void AddEventEntry(EventEntry eventEntry)
		{
			Stack.Add(eventEntry);
			if (eventEntry.Handled && !Handled)
			{
				Handled = true;
				HandledBy = eventEntry.Handler;
			}
		}


		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(GetType().GetProperty(propertyName) != null);
		    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}


	public class EventEntry
	{
		public EventEntry(object handler, bool handled)
		{
			Handler = handler;
			Handled = handled;
		}

		public bool Handled { get; }

	    public object Handler { get; }
	}
}
