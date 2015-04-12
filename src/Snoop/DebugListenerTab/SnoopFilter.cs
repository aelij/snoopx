using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Snoop.Annotations;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public abstract class SnoopFilter : INotifyPropertyChanged
	{
		private bool _isGrouped;
        private string _groupId = string.Empty;
	    private bool _isInverse;

		public void ResetDirtyFlag()
		{
			IsDirty = false;
		}

		public bool IsDirty { get; private set; }

	    public abstract bool FilterMatches(string debugLine);

		public virtual bool SupportsGrouping
		{
			get { return true; }
		}

		public bool IsInverse
		{
			get { return _isInverse; }
			set
			{
				if (value != _isInverse)
				{
					_isInverse = value;
					RaisePropertyChanged();
					RaisePropertyChanged("IsInverseText");
				}
			}
		}

		public string IsInverseText
		{
			get
			{
				return _isInverse ? "NOT" : string.Empty;
			}
		}

		public bool IsGrouped
		{
			get { return _isGrouped; }
			set
			{
				_isGrouped = value;
				RaisePropertyChanged();
				GroupId = string.Empty;
			}
		}

		public virtual string GroupId
		{
			get { return _groupId; }
			set
			{
				_groupId = value;
				RaisePropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
		protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
		{
			IsDirty = true;
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
