// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Snoop.MethodsTab
{
    /// <summary>
    /// Interaction logic for FullTypeSelector.xaml
    /// </summary>
    public partial class FullTypeSelector : ITypeSelector
    {
        public FullTypeSelector()
        {
            InitializeComponent();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var listAssemblies = assemblies.Select(assembly => new AssemblyNamePair
            {
                Name = assembly.FullName, Assembly = assembly
            }).ToList();

            listAssemblies.Sort();

            ComboBoxAssemblies.ItemsSource = listAssemblies;


        }

        private void comboBoxAssemblies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var assembly = ((AssemblyNamePair)ComboBoxAssemblies.SelectedItem).Assembly;

            var types = assembly.GetTypes();

            var typePairs = types.Where(type => type.IsPublic && !type.IsAbstract)
                .Select(type => new TypeNamePair
                {
                    Name = type.Name,
                    Type = type
                }).ToList();

            typePairs.Sort();

            ComboBoxTypes.ItemsSource = typePairs;
        }

        private void buttonCreateInstance_Click(object sender, RoutedEventArgs e)
        {
            var selectedType = ((TypeNamePair)ComboBoxTypes.SelectedItem).Type;

            if (string.IsNullOrEmpty(TextBoxConvertFrom.Text))
            {
                Instance = Activator.CreateInstance(selectedType);
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(selectedType);
                Instance = converter.ConvertFrom(TextBoxConvertFrom.Text);
            }

            DialogResult = true;

            Close();
        }

        public object Instance
        {
            get;
            private set;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }


    }

    //public class TypeNamePair : IComparable
    //{
    //    public string Name { get; set; }

    //    public Type Type { get; set; }

    //    public override string ToString()
    //    {
    //        return Name;
    //    }

    //    #region IComparable Members

    //    public int CompareTo(object obj)
    //    {
    //        return Name.CompareTo(((TypeNamePair)obj).Name);
    //    }

    //    #endregion
    //}

    //public class AssemblyNamePair : IComparable
    //{
    //    public string Name { get; set; }

    //    public Assembly Assembly { get; set; }

    //    public override string ToString()
    //    {
    //        return Name;
    //    }

    //    #region IComparable Members

    //    public int CompareTo(object obj)
    //    {
    //        return Name.CompareTo(((AssemblyNamePair)obj).Name);
    //    }

    //    #endregion
    //}
}
