// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Snoop.Converters;

namespace Snoop.MethodsTab
{
    public partial class MethodsControl
    {
        public MethodsControl()
        {
            InitializeComponent();
            DependencyPropertyDescriptor.FromProperty(RootTargetProperty, typeof(MethodsControl)).AddValueChanged(this, RootTargetChanged);

            //DependencyPropertyDescriptor.FromProperty(TargetProperty, typeof(MethodsControl)).AddValueChanged(this, TargetChanged);
            DependencyPropertyDescriptor.FromProperty(ComboBox.SelectedValueProperty, typeof(ComboBox)).AddValueChanged(ComboBoxMethods, ComboBoxMethodChanged);
            DependencyPropertyDescriptor.FromProperty(IsSelectedProperty, typeof(MethodsControl)).AddValueChanged(this, IsSelectedChanged);

            CheckBoxUseDataContext.Checked += _checkBoxUseDataContext_Checked;
            CheckBoxUseDataContext.Unchecked += _checkBoxUseDataContext_Unchecked;
        }

        void _checkBoxUseDataContext_Unchecked(object sender, RoutedEventArgs e)
        {
            ProcessCheckedProperty();
        }

        private void _checkBoxUseDataContext_Checked(object sender, RoutedEventArgs e)
        {
            ProcessCheckedProperty();
        }

        private void ProcessCheckedProperty()
        {
            if (!IsSelected || !CheckBoxUseDataContext.IsChecked.HasValue || !(RootTarget is FrameworkElement))
                return;

            SetTargetToRootTarget();
        }

        private void SetTargetToRootTarget()
        {
            if (CheckBoxUseDataContext.IsChecked.Value && RootTarget is FrameworkElement && ((FrameworkElement)RootTarget).DataContext != null)
            {
                Target = ((FrameworkElement)RootTarget).DataContext;
            }
            else
            {
                Target = RootTarget;
            }
        }

        private void IsSelectedChanged(object sender, EventArgs args)
        {
            if (IsSelected)
            {
                //this.Target = this.RootTarget;
                SetTargetToRootTarget();
            }
        }

        public object RootTarget
        {
            get { return GetValue(RootTargetProperty); }
            set { SetValue(RootTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RootTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RootTargetProperty =
            DependencyProperty.Register("RootTarget", typeof(object), typeof(MethodsControl), new UIPropertyMetadata(null));

        private void RootTargetChanged(object sender, EventArgs e)
        {
            if (IsSelected)
            {
                CheckBoxUseDataContext.IsEnabled = (RootTarget is FrameworkElement) && ((FrameworkElement)RootTarget).DataContext != null;
                SetTargetToRootTarget();
            }
        }



        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(MethodsControl), new UIPropertyMetadata(false));



        private static void TargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                var methodsControl = (MethodsControl)sender;

                methodsControl.EnableOrDisableDataContextCheckbox();

                var methodInfos = GetMethodInfos(methodsControl.Target);
                methodsControl.ComboBoxMethods.ItemsSource = methodInfos;

                methodsControl.ResultProperties.Visibility = Visibility.Collapsed;
                methodsControl.ResultStringContainer.Visibility = Visibility.Collapsed;
                methodsControl.ParametersContainer.Visibility = Visibility.Collapsed;

                //if this target has the previous method info, set it
                for (int i = 0; i < methodInfos.Count && methodsControl._previousMethodInformation != null; i++)
                {
                    var methodInfo = methodInfos[i];
                    if (methodInfo.Equals(methodsControl._previousMethodInformation))
                    {
                        methodsControl.ComboBoxMethods.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void EnableOrDisableDataContextCheckbox()
        {
            if (CheckBoxUseDataContext.IsChecked.HasValue && CheckBoxUseDataContext.IsChecked.Value)
                return;

            if (!(Target is FrameworkElement) || ((FrameworkElement)Target).DataContext == null)
            {
                CheckBoxUseDataContext.IsEnabled = false;
            }
            else
            {
                CheckBoxUseDataContext.IsEnabled = true;
            }
        }

        private SnoopMethodInformation _previousMethodInformation;
        private void ComboBoxMethodChanged(object sender, EventArgs e)
        {
            var selectedMethod = ComboBoxMethods.SelectedValue as SnoopMethodInformation;
            if (selectedMethod == null || Target == null)
                return;            

            var parameters = selectedMethod.GetParameters(Target.GetType());
            ItemsControlParameters.ItemsSource = parameters;

            ParametersContainer.Visibility = parameters.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            ResultProperties.Visibility = ResultStringContainer.Visibility = Visibility.Collapsed;

            _previousMethodInformation = selectedMethod;
        }

        public object Target
        {
            get { return GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(object), typeof(MethodsControl), new UIPropertyMetadata(TargetChanged));

        public void InvokeMethodClick(object sender, RoutedEventArgs e)
        {
            var selectedMethod = ComboBoxMethods.SelectedValue as SnoopMethodInformation;
            if (selectedMethod == null)
                return;

            object[] parameters = new object[ItemsControlParameters.Items.Count];

            if (!TryToCreateParameters(parameters))
                return;

            TryToInvokeMethod(selectedMethod, parameters);
        }

        private bool TryToCreateParameters(object[] parameters)
        {
            try
            {
                for (int index = 0; index < ItemsControlParameters.Items.Count; index++)
                {
                    var paramInfo = ItemsControlParameters.Items[index] as SnoopParameterInformation;
                    if (paramInfo == null)
                        return false;

                    if (paramInfo.ParameterType.Equals(typeof(DependencyProperty)))
                    {
                        DependencyPropertyNameValuePair valuePair = paramInfo.ParameterValue as DependencyPropertyNameValuePair;
                        parameters[index] = valuePair.DependencyProperty;
                    }
                    //else if (paramInfo.IsCustom || paramInfo.IsEnum)
                    else if (paramInfo.ParameterValue == null || paramInfo.ParameterType.IsAssignableFrom(paramInfo.ParameterValue.GetType()))
                    {
                        parameters[index] = paramInfo.ParameterValue;
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);
                        parameters[index] = converter.ConvertFrom(paramInfo.ParameterValue);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error creating parameter");
                return false;
            }
        }

        private void TryToInvokeMethod(SnoopMethodInformation selectedMethod, object[] parameters)
        {
            try
            {
                var returnValue = selectedMethod.MethodInfo.Invoke(Target, parameters);

                if (returnValue == null)
                {
                    SetNullReturnType(selectedMethod);
                    return;
                }
                ResultStringContainer.Visibility = TextBlockResult.Visibility = TextBlockResultLabel.Visibility = Visibility.Visible;

                TextBlockResultLabel.Text = "Result as string: ";
                TextBlockResult.Text = returnValue.ToString();

                var properties = returnValue.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                //var properties = PropertyInformation.GetAllProperties(returnValue, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

                if (properties.Length == 0)
                {
                    ResultProperties.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ResultProperties.Visibility = Visibility.Visible;
                    PropertyInspector.RootTarget = returnValue;
                }
            }
            catch (Exception ex)
            {
                string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show(message, "Error invoking method");
            }
        }

        private void SetNullReturnType(SnoopMethodInformation selectedMethod)
        {
            if (selectedMethod.MethodInfo.ReturnType == typeof(void))
            {
                ResultStringContainer.Visibility = ResultProperties.Visibility = Visibility.Collapsed;
            }
            else
            {
                ResultProperties.Visibility = Visibility.Collapsed;
                ResultStringContainer.Visibility = Visibility.Visible;
                TextBlockResult.Text = string.Empty;
                TextBlockResultLabel.Text = "Method evaluated to null";
                TextBlockResult.Visibility = Visibility.Collapsed;
            }
        }

        private static IList<SnoopMethodInformation> GetMethodInfos(object o)
        {
            if (o == null)
                return new ObservableCollection<SnoopMethodInformation>();

            Type t = o.GetType();
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod);

            var methodsToReturn = new List<SnoopMethodInformation>();

            foreach (var method in methods)
            {
                if (method.IsSpecialName)
                    continue;

                var info = new SnoopMethodInformation(method);
                info.MethodName = method.Name;
                
                methodsToReturn.Add(info);
            }
            methodsToReturn.Sort();            

            return methodsToReturn;
        }

        private void ChangeTarget_Click(object sender, RoutedEventArgs e)
        {
            if (RootTarget == null)
                return;

            var paramCreator = new ParameterCreator();
            paramCreator.TextBlockDescription.Text = "Delve into the new desired target by double-clicking on the property. Clicking OK will select the currently delved property to be the new target.";
            paramCreator.Title = "Change Target";
            paramCreator.RootTarget = RootTarget;
            paramCreator.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            paramCreator.ShowDialog();

            if (paramCreator.DialogResult.HasValue && paramCreator.DialogResult.Value)
            {
                Target = paramCreator.SelectedTarget;
            }
        }

    }
       
}
