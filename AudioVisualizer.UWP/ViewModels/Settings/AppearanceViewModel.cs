using AudioEqualizer.Models;
using AudioEqualizer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace AudioEqualizer.ViewModels.Settings
{
    public class AppearanceViewModel
    {
        SettingsManager settingsManager = new SettingsManager();
        CompactOverlayUtils compactOverlayUtils = new CompactOverlayUtils();

        public bool SetWindowOnTopAlways
        {
            get
            {
                return Convert.ToBoolean(settingsManager.GetSettingElement(nameof(SetWindowOnTopAlways)));
            }
            set
            {
                settingsManager.SetSettingElement(nameof(SetWindowOnTopAlways), value);

                var view = ApplicationView.GetForCurrentView();
                if (value == true && view.ViewMode != ApplicationViewMode.CompactOverlay)
                {
                    compactOverlayUtils.EnterCompactOverlay();
                }
                else if (value == false && view.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    compactOverlayUtils.ExitCompactOverlay();
                }
            }
        }
    }
}
