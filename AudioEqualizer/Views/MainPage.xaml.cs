using AudioEqualizer.Dialogs;
using AudioEqualizer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioEqualizer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // Set custom title bar
            Window.Current.SetTitleBar(AppTitle);
        }

        private async void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var file = await OpenFilePicker();

            if (file == null) return;

            if (Path.GetExtension(file.FileType) == ".mp4")
            {
                SelectedFileName.Text = Path.GetFileName(file.Path);
                TitleAppName.Text = "";
                PreviewTitle.Text = "";
                ContentFrame.Navigate(typeof(VideoPlayerPage), file);
            }
            else
            {
                SelectedFileName.Text = Path.GetFileName(file.Path);
                TitleAppName.Text = "";
                PreviewTitle.Text = "";
                ContentFrame.Navigate(typeof(AudioPlayerPage), file);
            }
        }

        private async Task<StorageFile> OpenFilePicker()
        {
            var picker = new FileOpenPicker();

            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".wmv");

            var file = await picker.PickSingleFileAsync();

            // Add file object to file list cache
            StorageApplicationPermissions.MostRecentlyUsedList.Add(file, file.Name);
            return file;
        }

        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog();
            _ = await aboutDialog.ShowAsync();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // The app can specify a property to restore the context of the previous page when returning,
            // but since the Win2D UI is not restored, navigate the blank page to stop playback and save the context in the container.
            ContentFrame.Navigate(typeof(BlankPage));

            this.Frame.Navigate(typeof(SettingsPage.SettingsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
