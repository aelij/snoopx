﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Snoop.DebugListenerTab;

namespace Snoop.Properties {
    
    
    [CompilerGenerated()]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue(@"<?xml version=""1.0"" encoding=""utf-16""?>
<WindowPlacement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Length>44</Length>
<Flags>0</Flags>
<WindowState>1</WindowState>
<MinimizedPosition>
<X>-1</X>
<Y>-1</Y>
</MinimizedPosition>
<MaximizedPosition>
<X>-1</X>
<Y>-1</Y>
</MaximizedPosition>
<NormalPosition>
<Left>10</Left>
<Top>10</Top>
<Right>650</Right>
<Bottom>490</Bottom>
</NormalPosition>
</WindowPlacement>")]
        public WindowPlacement SnoopUIWindowPlacement {
            get {
                return ((WindowPlacement)(this["SnoopUIWindowPlacement"]));
            }
            set {
                this["SnoopUIWindowPlacement"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue(@"<?xml version=""1.0"" encoding=""utf-16""?>
<WindowPlacement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Length>44</Length>
<Flags>0</Flags>
<WindowState>1</WindowState>
<MinimizedPosition>
<X>-1</X>
<Y>-1</Y>
</MinimizedPosition>
<MaximizedPosition>
<X>-1</X>
<Y>-1</Y>
</MaximizedPosition>
<NormalPosition>
<Left>10</Left>
<Top>10</Top>
<Right>541</Right>
<Bottom>36</Bottom>
</NormalPosition>
</WindowPlacement>")]
        public WindowPlacement AppChooserWindowPlacement {
            get {
                return ((WindowPlacement)(this["AppChooserWindowPlacement"]));
            }
            set {
                this["AppChooserWindowPlacement"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue(@"<?xml version=""1.0"" encoding=""utf-16""?>
<WindowPlacement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Length>44</Length>
<Flags>0</Flags>
<WindowState>1</WindowState>
<MinimizedPosition>
<X>-1</X>
<Y>-1</Y>
</MinimizedPosition>
<MaximizedPosition>
<X>-1</X>
<Y>-1</Y>
</MaximizedPosition>
<NormalPosition>
<Left>10</Left>
<Top>10</Top>
<Right>541</Right>
<Bottom>36</Bottom>
</NormalPosition>
</WindowPlacement>")]
        public WindowPlacement ZoomerWindowPlacement {
            get {
                return ((WindowPlacement)(this["ZoomerWindowPlacement"]));
            }
            set {
                this["ZoomerWindowPlacement"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("True")]
        public bool ShowDefaults {
            get {
                return ((bool)(this["ShowDefaults"]));
            }
            set {
                this["ShowDefaults"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("False")]
        public bool ShowPreviewer {
            get {
                return ((bool)(this["ShowPreviewer"]));
            }
            set {
                this["ShowPreviewer"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        public SnoopSingleFilter[] SnoopDebugFilters {
            get {
                return ((SnoopSingleFilter[])(this["SnoopDebugFilters"]));
            }
            set {
                this["SnoopDebugFilters"] = value;
            }
        }
        
        [UserScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfPropertyFilterSet xmlns:xsi=\"htt" +
            "p://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSch" +
            "ema\">\r\n  <PropertyFilterSet>\r\n    <DisplayName>Layout</DisplayName>\r\n    <IsDefa" +
            "ult>false</IsDefault>\r\n    <IsEditCommand>false</IsEditCommand>\r\n    <Properties" +
            ">\r\n      <string>width</string>\r\n      <string>height</string>\r\n      <string>ac" +
            "tualwidth</string>\r\n      <string>actualheight</string>\r\n      <string>margin</s" +
            "tring>\r\n      <string>padding</string>\r\n      <string>canvas.left</string>\r\n    " +
            "  <string>canvas.top</string>\r\n    </Properties>\r\n  </PropertyFilterSet>\r\n  <Pro" +
            "pertyFilterSet>\r\n    <DisplayName>Grid/Dock</DisplayName>\r\n    <IsDefault>false<" +
            "/IsDefault>\r\n    <IsEditCommand>false</IsEditCommand>\r\n    <Properties>\r\n      <" +
            "string>grid.</string>\r\n      <string>dockpanel.dock</string>\r\n    </Properties>\r" +
            "\n  </PropertyFilterSet>\r\n  <PropertyFilterSet>\r\n    <DisplayName>Color</DisplayN" +
            "ame>\r\n    <IsDefault>false</IsDefault>\r\n    <IsEditCommand>false</IsEditCommand>" +
            "\r\n    <Properties>\r\n      <string>color</string>\r\n      <string>background</stri" +
            "ng>\r\n      <string>foreground</string>\r\n      <string>borderbrush</string>\r\n    " +
            "  <string>fill</string>\r\n      <string>stroke</string>\r\n    </Properties>\r\n  </P" +
            "ropertyFilterSet>\r\n  <PropertyFilterSet>\r\n    <DisplayName>ItemsControl</Display" +
            "Name>\r\n    <IsDefault>false</IsDefault>\r\n    <IsEditCommand>false</IsEditCommand" +
            ">\r\n    <Properties>\r\n      <string>items</string>\r\n      <string>selected</strin" +
            "g>\r\n    </Properties>\r\n  </PropertyFilterSet>\r\n</ArrayOfPropertyFilterSet>")]
        public PropertyFilterSet[] PropertyFilterSets {
            get {
                return ((PropertyFilterSet[])(this["PropertyFilterSets"]));
            }
            set {
                this["PropertyFilterSets"] = value;
            }
        }
    }
}
