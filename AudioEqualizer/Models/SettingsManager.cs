using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioEqualizer.Models
{
    class SettingsManager
    {
        public object GetSettingElement(string elementName)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            object value = localSettings.Values[elementName];

            return value;
        }

        public void SetSettingElement(string elementName, object value)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[elementName] = value;
        }

        public void DeleteSettingElement(string elementName)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(elementName);
        }
    }
}
