using AudioEqualizer.Models;
using AudioEqualizer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioEqualizer.ViewModels
{
    public class AudioPlayerPageViewModel
    {
        SettingsManager settingsManager = new SettingsManager();

        public AudioPlayerPageViewModel()
        {
            if (settingsManager.GetSettingElement(nameof(AudioSeekSliderPosition)) == null)
            {
                AudioSeekSliderPosition = TimeSpan.Zero;
            }
        }


        public TimeSpan AudioSeekSliderPosition
        {
            get
            {
                TimeSpan time =  (TimeSpan)settingsManager.GetSettingElement(nameof(AudioSeekSliderPosition));
                return time;
            }
            set
            {
                settingsManager.SetSettingElement(nameof(AudioSeekSliderPosition), value);
            }
        }

        public double AudioVolumeSize
        {
            get
            {
                return Convert.ToDouble(settingsManager.GetSettingElement(nameof(AudioVolumeSize)));
            }
            set
            {
                settingsManager.SetSettingElement(nameof(AudioVolumeSize), value);
            }
        }

        public string AudioFileFullPath
        {
            get
            {
                return Convert.ToString(settingsManager.GetSettingElement(nameof(AudioFileFullPath)));
            }
            set
            {
                settingsManager.SetSettingElement(nameof(AudioFileFullPath), value);
            }
        }
    }
}
