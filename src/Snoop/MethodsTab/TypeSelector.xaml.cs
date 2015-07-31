// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Snoop.MethodsTab
{
    public partial class TypeSelector : ITypeSelector
    {
        public TypeSelector()
        {
            InitializeComponent();

            Loaded += TypeSelector_Loaded;            
        }

        public static List<Type> GetDerivedTypes(Type baseType)
        {
            var typesAssignable = (
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where baseType.IsAssignableFrom(type)
                select type).ToList();

            if (!baseType.IsAbstract)
            {
                typesAssignable.Add(baseType);
            }

            typesAssignable.Sort(new TypeComparerByName());

            return typesAssignable;
        }

        public List<Type> DerivedTypes { get; set; }

        private void TypeSelector_Loaded(object sender, RoutedEventArgs e)
        {

            if (DerivedTypes == null)
                DerivedTypes = GetDerivedTypes(BaseType);

            ComboBoxTypes.ItemsSource = DerivedTypes;
        }

        public Type BaseType { get; set; }

        public object Instance
        {
            get;
            private set;
        }

        private void buttonCreateInstance_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Instance = Activator.CreateInstance((Type)ComboBoxTypes.SelectedItem);
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
