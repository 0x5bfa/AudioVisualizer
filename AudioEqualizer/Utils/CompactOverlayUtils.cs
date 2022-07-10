using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace AudioEqualizer.Utils
{
    public class CompactOverlayUtils
    {
        public async void EnterCompactOverlay()
        {
            await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
            var size = new Size(500, 320);
            ApplicationView.GetForCurrentView().TryResizeView(size);
        }

        public async void ExitCompactOverlay()
        {
            await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
        }
    }
}
