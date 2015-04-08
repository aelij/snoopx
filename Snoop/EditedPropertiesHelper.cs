using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Snoop
{
    public class EditedPropertiesHelper
    {
        private static readonly object _lock = new object();

        private static readonly Dictionary<Dispatcher, Dictionary<VisualTreeItem, List<PropertyValueInfo>>> _itemsWithEditedProperties =
            new Dictionary<Dispatcher, Dictionary<VisualTreeItem, List<PropertyValueInfo>>>();

        public static void AddEditedProperty(Dispatcher dispatcher, VisualTreeItem propertyOwner, PropertyInformation propInfo)
        {
            lock (_lock)
            {
                List<PropertyValueInfo> propInfoList;
                Dictionary<VisualTreeItem, List<PropertyValueInfo>> dispatcherList;

                // first get the dictionary we're using for the given dispatcher
                if (!_itemsWithEditedProperties.TryGetValue(dispatcher, out dispatcherList))
                {
                    dispatcherList = new Dictionary<VisualTreeItem, List<PropertyValueInfo>>();
                    _itemsWithEditedProperties.Add(dispatcher, dispatcherList);
                }

                // now get the property info list for the owning object 
                if (!dispatcherList.TryGetValue(propertyOwner, out propInfoList))
                {
                    propInfoList = new List<PropertyValueInfo>();
                    dispatcherList.Add(propertyOwner, propInfoList);
                }

                // if we already have a property of that name on this object, remove it
                var existingPropInfo = propInfoList.FirstOrDefault(l => l.PropertyName == propInfo.DisplayName);
                if (existingPropInfo != null)
                {
                    propInfoList.Remove(existingPropInfo);
                }

                // finally add the edited property info
                propInfoList.Add(new PropertyValueInfo
                {
                    PropertyName = propInfo.DisplayName,
                    PropertyValue = propInfo.Value
                });
            }
        }

        public static void DumpObjectsWithEditedProperties()
        {
            if (_itemsWithEditedProperties.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat
                (
                    "Snoop dump as of {0}{1}--- OBJECTS WITH EDITED PROPERTIES ---{1}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Environment.NewLine
                );

            int dispatcherCount = 1;

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (KeyValuePair<Dispatcher, Dictionary<VisualTreeItem, List<PropertyValueInfo>>> dispatcherKvp in _itemsWithEditedProperties)
            {
                if (_itemsWithEditedProperties.Count > 1)
                {
                    sb.AppendFormat("-- Dispatcher #{0} -- {1}", dispatcherCount++, Environment.NewLine);
                }

                foreach (KeyValuePair<VisualTreeItem, List<PropertyValueInfo>> objectPropertiesPair in dispatcherKvp.Value)
                {
                    sb.AppendFormat("Object: {0}{1}", objectPropertiesPair.Key, Environment.NewLine);
                    foreach (PropertyValueInfo propInfo in objectPropertiesPair.Value)
                    {
                        sb.AppendFormat
                            (
                                "\tProperty: {0}, New Value: {1}{2}",
                                propInfo.PropertyName,
                                propInfo.PropertyValue,
                                Environment.NewLine
                            );
                    }
                }

                if (_itemsWithEditedProperties.Count > 1)
                {
                    sb.AppendLine();
                }
            }

            Debug.WriteLine(sb.ToString());
            Clipboard.SetText(sb.ToString());
        }
    }
}