using AudioEqualizer.Dialogs;
using AudioEqualizer.ViewModels;
using AudioVisualizer;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace AudioEqualizer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AudioPlayerPage : Page
    {
        MediaPlayer _player;
        PlaybackSource _playbackSource;
        SourceConverter _source;

        const uint spectrumBarCount = 50;
        SpectrumData _emptySpectrum = SpectrumData.CreateEmpty(2, spectrumBarCount, ScaleType.Linear, ScaleType.Linear, 0, 20000);
        SpectrumData _previousSpectrum;
        SpectrumData _previousPeakSpectrum;

        TimeSpan _rmsRiseTime = TimeSpan.FromMilliseconds(50);
        TimeSpan _rmsFallTime = TimeSpan.FromMilliseconds(50);
        TimeSpan _peakRiseTime = TimeSpan.FromMilliseconds(10);
        TimeSpan _peakFallTime = TimeSpan.FromMilliseconds(3000);
        TimeSpan _frameDuration = TimeSpan.FromMilliseconds(16.7);
        CanvasTextFormat _spectrumTextFormat;

        AudioPlayerPageViewModel vm = new AudioPlayerPageViewModel();

        bool _insidePositionUpdate = false;

        bool isPlayingNow = false;

        double previouslyVolumeSize = 0;

        public AudioPlayerPage()
        {
            this.InitializeComponent();

            _player = new MediaPlayer();
            _player.MediaOpened += Player_MediaOpened;
            _player.PlaybackSession.PositionChanged += Player_PositionChanged;

            _playbackSource = PlaybackSource.CreateFromMediaPlayer(_player);
            _playbackSource.SourceChanged += PlaybackSource_Changed;

            _source = (SourceConverter)Resources["source"];

            _source.RmsRiseTime = TimeSpan.FromMilliseconds(50);
            _source.RmsFallTime = TimeSpan.FromMilliseconds(50);
            _source.PeakRiseTime = TimeSpan.FromMilliseconds(50);
            _source.PeakFallTime = TimeSpan.FromMilliseconds(1000);

            // this.DataContext = vm;
        }

        private void PlaybackSource_Changed(object sender, IVisualizationSource args)
        {
            _source.Source = _playbackSource.Source;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _player.Source = MediaSource.CreateFromStorageFile(e.Parameter as StorageFile);

            var file = e.Parameter as StorageFile;
            if (vm.AudioFileFullPath == file.Path)
            {
                if (vm.AudioSeekSliderPosition != TimeSpan.Zero)
                {
                    _player.PlaybackSession.Position = vm.AudioSeekSliderPosition;
                }
            }
            else
            {
                vm.AudioFileFullPath = file.Path;
            }

            if (vm.AudioVolumeSize != 0)
            {
                _player.Volume = vm.AudioVolumeSize;
                CustomizeVolumeIcon(_player.Volume);
                VolumeFlyoutSizeTextBlock.Text = _player.Volume.ToString();
                VolumeSlider.Value = _player.Volume * 100;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            _player.Pause();
        }

        private async void Player_PositionChanged(MediaPlaybackSession session, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    _insidePositionUpdate = true;
                    AudioPositionSeekSlider.Value = session.Position.TotalSeconds;
                    CurrentPlaybackPositionTextBlock.Text = session.Position.ToString("h\\:mm\\:ss");
                    vm.AudioSeekSliderPosition = session.Position;
                    _insidePositionUpdate = false;
                });
        }

        private async void Player_MediaOpened(object sender, object e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    AudioPositionSeekSlider.Maximum = _player.PlaybackSession.NaturalDuration.TotalSeconds;
                    TotalTimeTextBlock.Text = _player.PlaybackSession.NaturalDuration.ToString("h\\:mm\\:ss");
                });
        }

        private void PlayBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlayingNow == false)
            {
                // Pause -> Play
                _player.Play();
                isPlayingNow = true;

                // Glyph: "PauseBold"
                PlayBackButtonIcon.Glyph = "\uf8ae";
            }
            else
            {
                // Play -> Pause
                _player.Pause();
                isPlayingNow = false;

                // Glyph: "PlaySolid"
                PlayBackButtonIcon.Glyph = "\uf5b0";
            }
        }

        private void AudioPositionSeekSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_insidePositionUpdate)
            {
                _player.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void Spectrum_CreateResources(object sender, CreateResourcesEventArgs args)
        {
            _spectrumTextFormat = new CanvasTextFormat();
            _spectrumTextFormat.VerticalAlignment = CanvasVerticalAlignment.Center;
            _spectrumTextFormat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
            _spectrumTextFormat.FontSize = 9;
        }

        private void Spectrum_Draw(object sender, VisualizerDrawEventArgs args)
        {
            var drawingSession = (CanvasDrawingSession)args.DrawingSession;
            Color gridlineColor = Colors.WhiteSmoke;
            Color textColor = Colors.WhiteSmoke;

            // 目盛用のマージン
            Thickness margin = new Thickness(35, 8, 8, 8);

            // グラフの描画可能範囲を計算
            Vector2 barSize =
                new Vector2((float)(args.ViewExtent.Width - margin.Left - margin.Right) / spectrumBarCount,
                            (float)(args.ViewExtent.Height - margin.Top * 2 - margin.Bottom * 2));

            // スペクトラムの長方形のグラフの領域を描画
            Rect boundingRectTop = new Rect(margin.Left, margin.Top, args.ViewExtent.Width - margin.Right - margin.Left, barSize.Y);

            // データが存在し、ソースが再生状態の場合はデータを取得し、それ以外の場合は空を使用します
            var spectrumData = args.Data != null && Spectrum.Source?.PlaybackState == SourcePlaybackState.Playing ? args.Data.Spectrum.LogarithmicTransform(spectrumBarCount, 20f, 20000f) : _emptySpectrum;

            _previousSpectrum = spectrumData.ApplyRiseAndFall(_previousSpectrum, _rmsRiseTime, _rmsFallTime, _frameDuration);
            _previousPeakSpectrum = spectrumData.ApplyRiseAndFall(_previousPeakSpectrum, _peakRiseTime, _peakFallTime, _frameDuration);

            var logSpectrum = _previousSpectrum.ConvertToDecibels(-50, 0);
            var logPeakSpectrum = _previousPeakSpectrum.ConvertToDecibels(-50, 0);

            float fStepWidth = (float)boundingRectTop.Width / 20;
            float freq = 20.0f;
            float fStep = (float)Math.Pow(1000.0, 1.0 / 20.0);

            // 垂直グリッドを1kHzごとに描画(0 - 20kHz)
            for (int f = 0; f < 20; f++, freq *= fStep)
            {
                float X = f * fStepWidth + (float)margin.Left;

                if (f != 0)
                {
                    drawingSession.DrawLine(X, (float)boundingRectTop.Top, X, (float)boundingRectTop.Bottom, gridlineColor);
                }

                string freqText = freq < 100.0f ? $"{freq:F0}" :
                                  freq < 1000.0f ? $"{10 * Math.Round(freq / 10):F0}" :
                                  freq < 10000.0f ? $"{freq / 1e3:F1}k" : $"{freq / 1e3:F0}k";

                drawingSession.DrawText(freqText, X + fStepWidth / 2, (float)boundingRectTop.Bottom + 10, textColor, _spectrumTextFormat);
            }

            // グラフの枠を描画
            drawingSession.DrawRectangle(boundingRectTop, gridlineColor);

            // 平行グリッドを10dbごとに描画
            for (int i = -50; i <= 0; i += 10)
            {
                float topY = 0;

                if (i == 0)
                {
                    topY = (float)boundingRectTop.Top - (float)i * (float)boundingRectTop.Height / 50.0f + 3;
                }
                else
                {
                    topY = (float)boundingRectTop.Top - (float)i * (float)boundingRectTop.Height / 50.0f;
                    drawingSession.DrawLine((float)boundingRectTop.Left, topY, (float)boundingRectTop.Right, topY, gridlineColor);
                }

                drawingSession.DrawText($"{i}dB", (float)boundingRectTop.Left - (float)margin.Left / 2, topY, textColor, _spectrumTextFormat);

            }

            // スペクトルバー(縦のバー)を描画する
            for (int index = 0; index < spectrumBarCount; index++)
            {
                float barX = (float)margin.Left + index * barSize.X;

                float spectrumBarHeight = barSize.Y * (1.0f - logSpectrum[0][index] / -50.0f);

                Rect rect = new Rect(barX, (float)boundingRectTop.Bottom - spectrumBarHeight, (2 * barSize.X) / 3, spectrumBarHeight);
                drawingSession.FillRoundedRectangle(rect, 2, 2, Colors.WhiteSmoke);
            }

            Vector2 prevPointLeft = new Vector2();

            // ゆっくりとした減衰線を描くためのスペクトルポイントを描画
            for (int index = 0; index < spectrumBarCount; index++)
            {
                float X = (float)margin.Left + index * barSize.X + barSize.X / 2;

                Vector2 leftPoint = new Vector2(X, (float)boundingRectTop.Bottom - barSize.Y * (1.0f - logPeakSpectrum[0][index] / -50.0f));

                if (index != 0)
                {
                    float brushWidth = 1.5f;
                    drawingSession.DrawLine(prevPointLeft, leftPoint, Colors.WhiteSmoke, brushWidth);
                }

                prevPointLeft = leftPoint;
            }
        }

        private void VolumeFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            if(_player.Volume == 0)
            {
                // Restore previously volume size
                _player.Volume = previouslyVolumeSize;
                VolumeFlyoutSizeTextBlock.Text = _player.Volume.ToString();
                VolumeSlider.Value = _player.Volume * 100;

                vm.AudioVolumeSize = _player.Volume;
                CustomizeVolumeIcon(_player.Volume);
            }
            else
            {
                // Save current volume size
                previouslyVolumeSize = _player.Volume;
                _player.Volume = 0;
                VolumeFlyoutSizeTextBlock.Text = "0";
                VolumeSlider.Value = 0;

                vm.AudioVolumeSize = _player.Volume;
                CustomizeVolumeIcon(_player.Volume);
            }
        }

        private void CustomizeVolumeIcon(double volumeSize)
        {
            if (volumeSize != 0)
            {
                VolumeButtonSecondaryIcon.Glyph = "\ue995";
                VolumeFlyoutButtonSecondaryIcon.Glyph = "\ue995";
            }

            if (volumeSize == 0)
            {
                // Glyph: "Mute" ]x
                VolumeFlyoutButtonIcon.Glyph = "\ue74f";
                VolumeButtonIcon.Glyph = "\ue74f";
                VolumeButtonSecondaryIcon.Glyph = "\ue74f";
                VolumeFlyoutButtonSecondaryIcon.Glyph = "\ue74f";
            }
            else if (volumeSize <= 0.25)
            {
                // Glyph: "Volume0" ]
                VolumeFlyoutButtonIcon.Glyph = "\ue992";
                VolumeButtonIcon.Glyph = "\ue992";
            }
            else if (volumeSize <= 0.5)
            {
                // Glyph: "Volume1" ]>
                VolumeFlyoutButtonIcon.Glyph = "\ue993";
                VolumeButtonIcon.Glyph = "\ue993";
            }
            else if (volumeSize <= 0.75)
            {
                // Glyph: "Volume2" ]>>
                VolumeFlyoutButtonIcon.Glyph = "\ue994";
                VolumeButtonIcon.Glyph = "\ue994";
            }
            else if (volumeSize <= 1)
            {
                // Glyph: "Volume" == "Volume3" ]>>>
                VolumeFlyoutButtonIcon.Glyph = "\ue995";
                VolumeButtonIcon.Glyph = "\ue995";
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_player == null)
            {
                return;
            }

            if(_player.Volume == 0 && previouslyVolumeSize != 0)
            {
                return;
            }

            _player.Volume = e.NewValue / 100;
            previouslyVolumeSize = _player.Volume;
            VolumeFlyoutSizeTextBlock.Text = e.NewValue.ToString();

            vm.AudioVolumeSize = _player.Volume;
            CustomizeVolumeIcon(_player.Volume);
        }

        private async void EqualizerEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var equalizerEditorDialog = new EqualizerEditorDialog();
            //equalizerEditorDialog.Margin = new Thickness(0, 32, 0, 0);
            var result = await equalizerEditorDialog.ShowAsync();
        }
    }
}
