// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
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

        //TODO: MOVE SOMEWHERE ELSE. MACIEK
        public static List<Type> GetDerivedTypes(Type baseType)
        {
            List<Type> typesAssignable = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (baseType.IsAssignableFrom(type))
                    {
                        typesAssignable.Add(type);
                    }
                }
            }

            if (!baseType.IsAbstract)
            {
                typesAssignable.Add(baseType);
            }

            typesAssignable.Sort(new TypeComparerByName());

            return typesAssignable;
        }

        public List<Type> DerivedTypes
        {
            get;
            set;
        }

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
