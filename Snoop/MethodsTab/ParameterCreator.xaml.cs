// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;

namespace Snoop.MethodsTab
{
    /// <summary>
    /// Interaction logic for ParameterCreator.xaml
    /// </summary>
    public partial class ParameterCreator 
    {
        public ParameterCreator()
        {
            InitializeComponent();
        }



        public object RootTarget
        {
            get { return GetValue(RootTargetProperty); }
            set { SetValue(RootTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RootTargetProperty =
            DependencyProperty.Register("RootTarget", typeof(object), typeof(ParameterCreator), new UIPropertyMetadata(null));

        public object SelectedTarget
        {
            get;
            private set;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {            
            DialogResult = true;
            SelectedTarget = PropertyInspector.Target;
            Close();            
        }



        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
