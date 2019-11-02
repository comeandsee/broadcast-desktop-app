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


namespace Indes2
{

    public partial class MainWindow : IDisposable
    {
        private WebCameraId cameraId = null;
        private int howMuchFiles = 10;

        private List<int> files = new List<int>();

        private Playlist video1List = new Playlist();
        private Playlist video2List = new Playlist();
        private Playlist video3List = new Playlist();

        private bool isWebCamLive = false;
        private bool isCamLive = false;
        private bool isWebCamLocal1Play = false;
        private bool isWebCamLocal2Play = false;


        private enum LiveCamStatus
        {
            webCamLocal1 = 1,
            webCamLocal2 = 2,
            video1 = 3,
            video2 = 4,
            playlist = 5
        }

        private String path = "Resources/";

        public ICommand StartSourceCommand { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            InitializeFilesList();
            GetVideoDevices();
        }


        private void PlayVideoList(Playlist playlist, MediaElement mediaElement, bool live = false)
        {
            // LOCAL

            if (!playlist.CheckIfPlaylistDone() && !live)
            {
                String uri = $"{path}{playlist.GetVideoList()[playlist.NextIndex]}.mp4";
                playlist.NextIndex += 1;
                mediaElement.Source = new Uri(uri, UriKind.Relative);
            }
            else if (playlist.CheckIfPlaylistNotNull() && !live)
            {
                playlist.NextIndex = 0;
                PlayVideoList(playlist, mediaElement);
            }

            //LIVE

            if (!playlist.CheckIfPlaylistLiveDone() && live)
            {
                String uri = $"{path}{playlist.GetVideoList()[playlist.NextIndexLive]}.mp4";
                playlist.NextIndexLive += 1;
                mediaElement.Source = new Uri(uri, UriKind.Relative);
            }
            else if (playlist.CheckIfPlaylistNotNull() && live)
            {
                playlist.NextIndexLive = 0;
                PlayVideoList(playlist, mediaElement, live);
            }

        }


        //INITIALIZE
        private void InitializeFilesList()
        {
            for (int i = 1; i <= howMuchFiles; i++)
            {
                files.Add(i);
            }

            foreach (int element in files)
            {
                listBox.Items.Add(element);
            }

        }
        private void InitializeCamera()
        {
            var x = webCameraControl.GetVideoCaptureDevices();

            if (x != null)
            {
                IEnumerator<WebCameraId> enumerator = x.GetEnumerator();
                if (enumerator.MoveNext()) cameraId = enumerator.Current;
            }
        }

        // MIX LIVE
        private void ManagerLiveMix(LiveCamStatus status)
        {

            switch (status)
            {
                case LiveCamStatus.webCamLocal1:

                    ColorButtonsForWebCamLocal1();
                    StopIfCamLivePlay();

                    if (isWebCamLocal1Play || isWebCamLocal2Play)
                    {
                        ColorButtonsForVideoLive();
                        imageLive.Visibility = Visibility.Visible;
                        turnOnButton(buttonMixLive1);
                        isWebCamLive = true;
                    }

                    break;

                case LiveCamStatus.video1:
                    if (video1List.CheckIfPlaylistNotNull())
                    {
                        ColorButtonsForVideo1();

                        liveME.Tag = 1;
                        StopIfWebCamLivePlay();
                        StartLive();
                        video1List.NextIndexLive = video1List.NextIndex - 1;
                        PlayVideoList(video1List, liveME, true);
                    }
                    break;

                case LiveCamStatus.video2:

                    if (video2List.CheckIfPlaylistNotNull())
                    {
                        ColorButtonsForVideo2();

                        liveME.Tag = 2;
                        StopIfWebCamLivePlay();
                        StartLive();
                        video2List.NextIndexLive = video2List.NextIndex - 1;
                        PlayVideoList(video2List, liveME, true);
                    }
                    break;

                case LiveCamStatus.playlist:
                    if (video3List.CheckIfPlaylistNotNull())
                    {
                        ColorButtonsForPlaylist();

                        liveME.Tag = 3;
                        StopIfWebCamLivePlay();
                        StartLive();
                        PlayVideoList(video3List, liveME, true);
                    }
                    break;
                default:
                    break;
            }
        }


        //COLORS
        private void ColorButtonsForPlaylist()
        {
            turnOnButton(buttonPL);

            turnOffButton(buttonMixLive1);
            turnOffButton(buttonMixLive2);
            turnOffButton(buttonMixLive3);
            ColorWebLocalButtons();
        }

        private void ColorButtonsForVideo2()
        {
            turnOnButton(buttonMixLive3);

            turnOffButton(buttonMixLive1);
            turnOffButton(buttonPL);
            turnOffButton(buttonMixLive2);
            ColorWebLocalButtons();
        }

        private void ColorButtonsForVideoLive()
        {
            turnOnButton(buttonMixLive1);

            turnOffButton(buttonMixLive2);
            turnOffButton(buttonPL);
            turnOffButton(buttonMixLive3);
            ColorWebLocalButtons();

        }
        private void ColorButtonsForVideo1()
        {
            turnOnButton(buttonMixLive2);

            turnOffButton(buttonMixLive1);
            turnOffButton(buttonPL);
            turnOffButton(buttonMixLive3);
            ColorWebLocalButtons();
        }

        private void ColorWebLocalButtons()
        {
            if (isWebCamLocal1Play) turnOnButton(buttonLC1);
            if (isWebCamLocal2Play) turnOnButton(buttonLC2);
        }

        private void ColorButtonsForWebCamLocal1()
        {
            turnOffButton(buttonMixLive2);
            turnOffButton(buttonMixLive3);
            turnOffButton(buttonPL);
            turnOffButton(buttonLC1);
        }

        private void turnOffButton(Button button)
        {
            button.Background = System.Windows.Media.Brushes.Red;
        }
        private void turnOnButton(Button button)
        {
            button.Background = System.Windows.Media.Brushes.Green;
        }



        // MIX LIVE BUTTONS
        private void ButtonMixLive1_Click(object sender, RoutedEventArgs e)
        {
            ManagerLiveMix(LiveCamStatus.webCamLocal1);
        }

        private void ButtonMixLive2_Click(object sender, RoutedEventArgs e)
        {
            ManagerLiveMix(LiveCamStatus.video1);
        }


        private void ButtonMixLive3_Click(object sender, RoutedEventArgs e)
        {
            ManagerLiveMix(LiveCamStatus.video2);
        }

        //MANAGE LIVE CAM
        private void StopIfWebCamLivePlay()
        {
            if (isWebCamLive)
            {
                StopCamera();
                isWebCamLive = false;
                if (isWebCamLocal1Play) StartCamera(LiveCamStatus.webCamLocal1);
                else if (isWebCamLocal2Play) StartCamera(LiveCamStatus.webCamLocal2);
                imageLive.Visibility = Visibility.Hidden;
            }
        }
        private void StopIfCamLivePlay()
        {
            if (isCamLive)
            {
                liveME.LoadedBehavior = MediaState.Stop;
                liveME.Visibility = Visibility.Hidden;
                isCamLive = false;
            }
        }
        private void StopIfWebCamLocal1Play()
        {
            if (isWebCamLocal1Play)
            {
                webCameraControl.StopCapture();
                webCameraControl.Visibility = Visibility.Hidden;
                isWebCamLocal1Play = false;
            }
        }

        // MIX BUTTONS

        private void ButtonLC1_Click(object sender, RoutedEventArgs e)
        {
            buttonLC1.Background = System.Windows.Media.Brushes.Green;
            buttonLC2.Background = System.Windows.Media.Brushes.Red;
            StopCamera();
            StartCamera(LiveCamStatus.webCamLocal1);
        }

        private void ButtonLC2_Click(object sender, RoutedEventArgs e)
        {
            buttonLC1.Background = System.Windows.Media.Brushes.Red;
            buttonLC2.Background = System.Windows.Media.Brushes.Green;
            StopCamera();
            StartCamera(LiveCamStatus.webCamLocal2);
        }

        private void ButtonPL_Click(object sender, RoutedEventArgs e)
        {
            ManagerLiveMix(LiveCamStatus.playlist);
           
        }

        private void ButtonAddL1_Click(object sender, RoutedEventArgs e)
        {
            ManageVideoList(video1List, listBox1, video1ME);
        }

        private void ButtonAddL2_Click(object sender, RoutedEventArgs e)
        {
            ManageVideoList(video2List, listBox2, video2ME);
        }

        private void ButtonAddL3_Click(object sender, RoutedEventArgs e)
        {
            AddVideoToList(video3List, listBox3);
        }


        // MANAGE
        private void AddVideoToList(Playlist videoList, ListBox box)
        {
            if (listBox.SelectedItem != null)
            {
                string choosedVideo = listBox.SelectedItem.ToString();
                videoList.AddVideo(choosedVideo);
                box.Items.Add(choosedVideo);
            }
        }
        private void DelVideoFromList(Playlist videoList, ListBox boxChoosed)
        {
            if (boxChoosed.SelectedItem != null)
            {
                String choosedVideo = boxChoosed.SelectedItem.ToString();
                videoList.DelVideo(choosedVideo);
                boxChoosed.Items.RemoveAt(boxChoosed.SelectedIndex);
            }
        }
        private void ManageVideoList(Playlist playlist, ListBox selectedBox, MediaElement mediaElement)
        {
            AddVideoToList(playlist, selectedBox);
            if (playlist.Count() == 1)
            {
                PlayVideoList(playlist, mediaElement);
            }
        }

        /*   private void StartWebCamera1Live()
           {
               webCameraControlLive.Visibility = Visibility.Visible;
               webCameraControlLive.StartCapture(cameraId);
               isWebCamLive = true;
           }*/
        private void StartLive()
        {
            liveME.Visibility = Visibility.Visible;
            liveME.LoadedBehavior = MediaState.Play;
            isCamLive = true;
        }

        //DELETE
        private void ButtonDel1_Click(object sender, RoutedEventArgs e)
        {
            DelVideoFromList(video1List, listBox1);
        }

        private void ButtonDel2_Click(object sender, RoutedEventArgs e)
        {
            DelVideoFromList(video2List, listBox2);
        }

        private void ButtonDel3_Click(object sender, RoutedEventArgs e)
        {
            DelVideoFromList(video3List, listBox3);
        }


        //Loop for playlist
        private void Video1ME_MediaEnded(object sender, RoutedEventArgs e)
        {
            PlayVideoList(video1List, video1ME);
        }

        private void Video2ME_MediaEnded(object sender, RoutedEventArgs e)
        {
            PlayVideoList(video2List, video2ME);
        }

        private void Video3ME_MediaEnded(object sender, RoutedEventArgs e)
        {
            switch (liveME.Tag)
            {
                case 1:
                    PlayVideoList(video1List, liveME, true);
                    break;
                case 2:
                    PlayVideoList(video2List, liveME, true);
                    break;
                case 3:
                    PlayVideoList(video3List, liveME, true);
                    break;
                default:
                    break;
            }

        }

        public ObservableCollection<FilterInfo> VideoDevices { get; set; }
        private FilterInfo _currentDevice;


        private bool _isIpCameraSource;
        private bool _isWebcamSource;
        private bool _isLiveCam = false;

        private IVideoSource _videoSource;
        private VideoFileWriter _writer;
        private bool _recording;
        private DateTime? _firstFrameTime;
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


        private void StopCamera(bool localCam = true)
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= video_NewFrame;
            }
            if (localCam) image.Dispatcher.Invoke(() => image.Source = null);
            else if (isWebCamLive) imageLive.Dispatcher.Invoke(() => imageLive.Source = null);
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
                    image.Dispatcher.Invoke(() => image.Source = bi);
                    if (isWebCamLive) imageLive.Dispatcher.Invoke(() => imageLive.Source = bi);

                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StopCamera();
            }
        }
        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; }
        }
        private void StartCamera(LiveCamStatus cameraId)
        {

            switch (cameraId)
            {
                case LiveCamStatus.webCamLocal2:
                    StopCamera();
                    _videoSource = new MJPEGStream("http://192.168.1.192:8089/video");
                    _videoSource.NewFrame += video_NewFrame;
                    _videoSource.Start();
                    isWebCamLocal1Play = false;
                    isWebCamLocal2Play = true;
                    break;
                case LiveCamStatus.webCamLocal1:
                    if (CurrentDevice != null)
                    {
                        StopCamera();
                        _videoSource = new AForge.Video.DirectShow.VideoCaptureDevice(CurrentDevice.MonikerString);
                        _videoSource.NewFrame += video_NewFrame;
                        _videoSource.Start();
                        isWebCamLocal1Play = true;
                        isWebCamLocal2Play = false;
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
