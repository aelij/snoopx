// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;

namespace Snoop.MethodsTab
{
    public class TypeComparerByName : IComparer<Type>
    {
        #region IComparer<Type> Members

        public int Compare(Type x, Type y)
        {
            return x.Name.CompareTo(y.Name);
        }

        #endregion
    }
}
