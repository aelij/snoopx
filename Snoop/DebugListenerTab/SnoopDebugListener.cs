using System.Collections.Generic;
using System.Diagnostics;

namespace Snoop.DebugListenerTab
{
	public sealed class SnoopDebugListener : TraceListener
	{
		private readonly IList<IListener> _listeners = new List<IListener>();

		public void RegisterListener(IListener listener)
		{
			_listeners.Add(listener);
		}

		public const string ListenerName = "SnoopDebugListener";

		public SnoopDebugListener()
		{
			Name = ListenerName;
		}

		public override void WriteLine(string str)
		{
			SendDataToListeners(str);
		}

		public override void Write(string str)
		{
			SendDataToListeners(str);
		}

		private void SendDataToListeners(string str)
		{
			foreach (var listener in _listeners)
				listener.Write(str);
		}

		public override void Write(string message, string category)
		{
			SendDataToListeners(message);

			base.Write(message, category);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			SendDataToListeners(message);
			base.TraceEvent(eventCache, source, eventType, id, message);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{
			SendDataToListeners(source);
			base.TraceData(eventCache, source, eventType, id, data);
		}
	}
}
