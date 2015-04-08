// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Data;
using Snoop.Properties;

namespace Snoop
{
	public class SettingBindingExtension : Binding
	{
		public SettingBindingExtension()
		{
		}

		public SettingBindingExtension(string path) : base(path)
		{
		}

		private void Initialize()
		{
			Source = Settings.Default;
			Mode = BindingMode.TwoWay;
		}
	}
}
