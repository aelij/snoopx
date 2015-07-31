// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views
{
	/// <summary>
	/// Interaction logic for SnoopabilityFeedbackWindow.xaml
	/// </summary>
	public partial class SnoopabilityFeedbackWindow
	{
		public SnoopabilityFeedbackWindow()
		{
			InitializeComponent();
		}

		public string SnoopTargetName
		{
			get { return TbWindowName.Text; }
			set { TbWindowName.Text = value; }
		}
	}
}
