using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WebEye.Controls.Wpf;
using AForge.Video;
using AForge.Video.DirectShow;

using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using Accord.Video.FFMPEG;
using Indes2;
using static Indes2.MainWindow;

namespace Indes2
{
    class WebCamManager : IDisposable
    {


        public ObservableCollection<FilterInfo> VideoDevices { get; set; }
        private FilterInfo _currentDevice;


        public System.Windows.Controls.Image ImageLocal { get; private set; }
        public bool IsWebCamLive { get;  set; }
        public System.Windows.Controls.Image ImageLive { get; private set; }
        public bool IsWebCamLocal1Play { get;  set; }
        public bool IsWebCamLocal2Play { get;  set; }

        private IVideoSource _videoSource;
        private VideoFileWriter _writer;

        public WebCamManager(ref System.Windows.Controls.Image imgLocal,
                            ref System.Windows.Controls.Image imgLive, 
                            ref bool isWebCamLive)
        {
            ImageLocal = imgLocal;
            ImageLive = imgLive;
            IsWebCamLive = isWebCamLive;

            IsWebCamLocal1Play = false;
            IsWebCamLocal2Play = false;

            GetVideoDevices();
        }

        private void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in devices)
            {
                VideoDevices.Add(device);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
            }
            else
            {
                MessageBox.Show("No webcam found");
            }
        }


        public void StopCamera(bool localCam = true)
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= video_NewFrame;
            }
            if (localCam) ImageLocal.Dispatcher.Invoke(() => ImageLocal.Source = null);
            else if (IsWebCamLive) ImageLive.Dispatcher.Invoke(() => ImageLive.Source = null);
        }

        public void Dispose()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
            }
            _writer?.Dispose();
        }


        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    var bi = bitmap.ToBitmapImage();
                    bi.Freeze();
                    ImageLocal.Dispatcher.Invoke(() => ImageLocal.Source = bi);
                    if (IsWebCamLive) ImageLive.Dispatcher.Invoke(() => ImageLive.Source = bi);

                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StopCamera();
            }
        }
        private FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; }
        }

    

        public void StartCamera(LiveCamStatus cameraId)
        {

            switch (cameraId)
            {
                case LiveCamStatus.webCamLocal2:
                    StopCamera();
                    _videoSource = new MJPEGStream("http://192.168.1.192:8089/video");
                    _videoSource.NewFrame += video_NewFrame;
                    _videoSource.Start();
                    IsWebCamLocal1Play = false;
                    IsWebCamLocal2Play = true;
                    break;
                case LiveCamStatus.webCamLocal1:
                    if (CurrentDevice != null)
                    {
                        StopCamera();
                        _videoSource = new AForge.Video.DirectShow.VideoCaptureDevice(CurrentDevice.MonikerString);
                        _videoSource.NewFrame += video_NewFrame;
                        _videoSource.Start();
                        IsWebCamLocal1Play = true;
                        IsWebCamLocal2Play = false;
                    }
                    else
                    {
                        MessageBox.Show("Current device can't be null");
                    }
                    break;


                default:
                    break;
            }
        }

    }
}
