// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.ComponentModel;
using System.Configuration;

namespace Snoop.Properties
{
	// This class allows you to handle specific events on the settings class:
	//  The SettingChanging event is raised before a setting's value is changed.
	//  The PropertyChanged event is raised after a setting's value is changed.
	//  The SettingsLoaded event is raised after the setting values are loaded.
	//  The SettingsSaving event is raised before the setting values are saved.
	internal sealed partial class Settings
	{
	    private void SettingChangingEventHandler(object sender, SettingChangingEventArgs e)
		{
			// Add code to handle the SettingChangingEvent event here.
		}

		private void SettingsSavingEventHandler(object sender, CancelEventArgs e)
		{
			// Add code to handle the SettingsSaving event here.
		}
	}
}
