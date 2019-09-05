using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace RedLightDesktopUWP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {

        private Guid guid = Guid.Parse("E9E2ED52-12AA-405A-AB1F-0C70878EFFD9");
        private Guid guid2 = Guid.Parse("9DE5F89C-E9BF-4073-9A27-C5ED076A3A19");
       

        private BluetoothCommunicator communicator;
        private DataRegister dataRegister;
        private bool isConnected;


        public ObservableCollection<RfcommDeviceDisplay> ResultCollection
        {
            get;
            private set;
        }

        private DeviceWatcher deviceWatcher = null;

        public MainPage()
        {

            this.InitializeComponent();
            App.Current.Suspending += App_Suspending;
            dataRegister = new DataRegister()
                .SetConditionFontIcon(ref ConditionIcon)
                .SetConditionTextBox(ref ConditionData)
                .SetPulseTextBox(ref PulseData)
                .SetSPO2TextBox(ref SPO2Data)
                .SetTempTextBox(ref TempData)
                .SetLocationTextBox(ref LocationData);

            dataRegister.UpdateData("2019;243;59123051;4;098;3750;09820;0230;+3748208;+12688542;");
            

            communicator = new BluetoothCommunicator(guid, ref dataRegister);
            isConnected = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ResultCollection = new ObservableCollection<RfcommDeviceDisplay>();
            DataContext = this;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopWatcher();
        }

        void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Make sure we clean up resources on suspend.
            communicator.Disconnect();
        }

        private void StartUnpairedDeviceWatcher()
        {
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Make sure device name isn't blank
                    if (deviceInfo.Name != "")
                    {
                        ResultCollection.Add(new RfcommDeviceDisplay(deviceInfo));
                    }

                });
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            rfcommInfoDisp.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                });
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //
                });
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(rfcommInfoDisp);
                            break;
                        }
                    }
                });
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    ResultCollection.Clear();
                });
            });

            deviceWatcher.Start();
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }

        private void Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           // UpdatePairingButtons();
        }

        private void Configure_Button_Click(object sender, RoutedEventArgs e)
        {
            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            if (isConnected)
            {
                communicator.Disconnect();
                isConnected = false;
                ConnectDeviceButton.Content = "Connect to Selected Device";
            }
            else
            {
                communicator.Connect((Devices.SelectedItem as RfcommDeviceDisplay).Id);
                isConnected = true;
                ResetMainUI();
                ConnectDeviceButton.Content = "Disconnected from Device";
            }
            
        }

        private void SearchDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher == null)
            {
                SetDeviceWatcherUI();
                StartUnpairedDeviceWatcher();
            }
            else
            {
                ResetMainUI();
            }
        }

        private void SetDeviceWatcherUI()
        {
            SearchDeviceButton.Content = "Stop Searching";
            Devices.Visibility = Visibility.Visible;
            Devices.IsEnabled = true;
        }

        private void ResetMainUI()
        {
            SearchDeviceButton.Content = "Search Device";
            SearchDeviceButton.IsEnabled = true;
            ConnectDeviceButton.Visibility = Visibility.Visible;
            Devices.Visibility = Visibility.Visible;
            Devices.IsEnabled = true;
            StopWatcher();
        }
        private void UpdatePairingButtons()
        {
            RfcommDeviceDisplay deviceDisp = (RfcommDeviceDisplay)Devices.SelectedItem;

            if (null != deviceDisp)
            {
                ConnectDeviceButton.IsEnabled = true;
            }
            else
            {
                ConnectDeviceButton.IsEnabled = false;
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            communicator.SendMessage("TestMessage from Desktop");
        }
    
    }

    public class RfcommDeviceDisplay : INotifyPropertyChanged
    {
        private DeviceInformation deviceInfo;

        public RfcommDeviceDisplay(DeviceInformation deviceInfoIn)
        {
            deviceInfo = deviceInfoIn;
            UpdateGlyphBitmapImage();
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return deviceInfo;
            }

            private set
            {
                deviceInfo = value;
            }
        }

        public string Id
        {
            get
            {
                return deviceInfo.Id;
            }
        }

        public string Name
        {
            get
            {
                return deviceInfo.Name;
            }
        }

        public BitmapImage GlyphBitmapImage
        {
            get;
            private set;
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            deviceInfo.Update(deviceInfoUpdate);
            UpdateGlyphBitmapImage();
        }

        private async void UpdateGlyphBitmapImage()
        {
            DeviceThumbnail deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
            BitmapImage glyphBitmapImage = new BitmapImage();
            await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
            GlyphBitmapImage = glyphBitmapImage;
            OnPropertyChanged("GlyphBitmapImage");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}