using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;

namespace WpfMediaPlayer
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool _isDraggingProgress = false; // 避免拖动进度条时与定时器冲突

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            InitializeTimer();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保所有控件均已创建后再设置初始显示
            VolumePercent.Text = $"{VolumeSlider.Value * 100:0}%";
            Debug.WriteLine("MainWindow loaded");
        }

        // 初始化定时器，每 500 毫秒更新一次 UI 状态
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Debug.WriteLine("Timer started");
        }

        // 定时器事件：更新进度条、当前时间
        private void Timer_Tick(object sender, EventArgs e)
        {
            // 仅在媒体已设置且不在拖动时更新
            if (MediaPlayer.Source != null && !_isDraggingProgress)
            {
                // 如果可获取总时长，按百分比更新进度条
                if (MediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    double current = MediaPlayer.Position.TotalSeconds;
                    double total = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    if (total > 0)
                    {
                        ProgressSlider.Value = (current / total) * 100;
                    }

                    CurrentTimeLabel.Text = TimeSpan.FromSeconds(current).ToString(@"mm\:ss");
                    TotalTimeLabel.Text = "/ " + TimeSpan.FromSeconds(total).ToString(@"mm\:ss");
                }
                else
                {
                    // NaturalDuration 尚不可用：仍更新时间显示（若有），总时长显示占位
                    double current = MediaPlayer.Position.TotalSeconds;
                    CurrentTimeLabel.Text = TimeSpan.FromSeconds(current).ToString(@"mm\:ss");
                    TotalTimeLabel.Text = "/ --:--";
                }
            }
        }

        // 打开文件
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "媒体文件|*.mp4;*.avi;*.wmv;*.mp3;*.wav|所有文件|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Debug.WriteLine($"Opening file: {openFileDialog.FileName}");
                MediaPlayer.Source = new Uri(openFileDialog.FileName);
                MediaPlayer.Play();
            }
        }

        // 播放
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.Source != null)
            {
                MediaPlayer.Play();
            }
        }

        // 暂停
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Pause();
        }

        // 停止
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Stop();
            // 重置进度条和时间
            ProgressSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
            TotalTimeLabel.Text = "/ 00:00";
        }

        // 音量改变
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.Volume = VolumeSlider.Value;
            if (VolumePercent != null)
                VolumePercent.Text = $"{VolumeSlider.Value * 100:0}%";
        }

        // 静音
        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.Volume > 0)
            {
                MediaPlayer.Volume = 0;
                VolumeSlider.Value = 0;
            }
            else
            {
                MediaPlayer.Volume = 0.5;
                VolumeSlider.Value = 0.5;
            }
        }

        // 媒体加载完成时，获取总时长并重置进度条范围
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MediaOpened event fired");
            if (MediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double totalSeconds = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                TotalTimeLabel.Text = "/ " + TimeSpan.FromSeconds(totalSeconds).ToString(@"mm\:ss");
                // 进度条最大值已经是 100（百分比），不需要额外设置
            }
            else
            {
                Debug.WriteLine("MediaOpened: NaturalDuration not available");
                TotalTimeLabel.Text = "/ --:--";
            }
        }

        // 播放结束时自动停止并重置进度
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Stop();
            ProgressSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
        }

        // 进度条拖拽开始（可靠）
        private void ProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isDraggingProgress = true;
        }

        // 进度条拖拽完成（可靠），在此执行 seek
        private void ProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (MediaPlayer.Source != null && MediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double total = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                double newPosition = (ProgressSlider.Value / 100.0) * total;
                MediaPlayer.Position = TimeSpan.FromSeconds(newPosition);
                Debug.WriteLine($"Seek to {newPosition} s");
            }
            _isDraggingProgress = false;
        }
    }
}