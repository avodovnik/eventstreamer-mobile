using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Xamarin.Forms;

using Plugin.BluetoothLE;
using System.Collections.ObjectModel;

namespace SensorCollector
{
    public class MovesenseDevice
    {
        public string Name { get; set; }
        public string Serial { get; set; }
    }


    public partial class MainPage : ContentPage
    {
        public ObservableCollection<SelectableItem<MovesenseDevice>> DeviceList { get; set; }

        public MainPage()
        {
            InitializeComponent();

            //this.listView.ItemsSource = new List<SampleItem>(new SampleItem[] {
            //    new SampleItem() { Name = "Test 1"},
            //    new SampleItem() { Name = "Test 2"}
            //});

            DeviceList = new ObservableCollection<SelectableItem<MovesenseDevice>>();
            this.BindingContext = this;
            this.lstDevices.ItemsSource = DeviceList;
        }

        IDisposable scan;
        public IAdapter BleAdapter => CrossBleAdapter.Current;

        private void DoScan()
        {
            lblStatus.Text = "Scanning for devices...";
            busyIndicator.IsVisible = true;
            busyIndicator.IsRunning = true;

            this.DeviceList.Clear();

            //this.scan = this
            //.adapter
            //.Scan()
            //.Buffer(TimeSpan.FromSeconds(1))
            //.ObserveOn(RxApp.MainThreadScheduler)
            //.Subscribe(
            //    results =>
            //    {
            //        var list = new List<ScanResultViewModel>();
            //        foreach (var result in results)
            //        {
            //            var dev = this.Devices.FirstOrDefault(x => x.Uuid.Equals(result.Device.Uuid));

            //            if (dev != null)
            //            {
            //                dev.TrySet(result);
            //            }
            //            else
            //            {
            //                dev = new ScanResultViewModel();
            //                dev.TrySet(result);
            //                list.Add(dev);
            //            }
            //        }
            //        if (list.Any())
            //            this.Devices.AddRange(list);
            //    },
            //    ex => dialogs.Alert(ex.ToString(), "ERROR")
            //)
            //.DisposeWith(this.DeactivateWith);
            


            scan = this.BleAdapter.Scan()
                       .Buffer(TimeSpan.FromSeconds(1))
                       .Subscribe(OnScanResults);
        }

        public void StopScanning()
        {
            lblStatus.Text = "Doing nothing...";
            busyIndicator.IsVisible = false;
            busyIndicator.IsRunning = false;
            this.scan?.Dispose();
        }


        private void OnScanResults(IList<IScanResult> results)
        {
            var items = 
                results
                    .Where(x => (x.Device.Name?.StartsWith("Movesense", StringComparison.InvariantCultureIgnoreCase)).GetValueOrDefault(false))
                .Select(x => new SelectableItem<MovesenseDevice>(new MovesenseDevice() { Name = x.Device.Name, Serial = x.Device.Uuid.ToString() }));

            Device.BeginInvokeOnMainThread(() =>
            {
                this.DeviceList = new ObservableCollection<SelectableItem<MovesenseDevice>>(items);
                this.lstDevices.ItemsSource = DeviceList;
            });
           
        }

        //async void OnScanResult(IScanResult result)
        //{
        //    // Only interested in Movesense devices
        //    if (result.Device.Name != null)
        //    {
        //        System.Diagnostics.Debug.WriteLine(result.Device.Name);
        //        if (result.Device.Name.StartsWith("Movesense", StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            lock (DeviceList)
        //            {
        //                if (DeviceList.FirstOrDefault(x => x.Data.Name == result.Device.Name) != null)
        //                {
        //                    // we already have that device, let's just ignore it
        //                }
        //                else
        //                {
        //                    Device.BeginInvokeOnMainThread(() =>
        //                    {
        //                        this.DeviceList.Add(new SelectableItem<MovesenseDevice>(new MovesenseDevice()
        //                        {
        //                            Name = result.Device.Name,
        //                            Serial = result.Device.Uuid.ToString()
        //                        }));
        //                    });
        //                }
        //            }


        //            //// Now do the Mds connection
        //            //var sensor = result.Device;
        //            //StatusLabel.Text = $"Connecting to device {sensor.Name}";
        //            //var movesense = Plugin.Movesense.CrossMovesense.Current;
        //            //await movesense.ConnectMdsAsync(sensor.Uuid);

        //            //// Talk to the device
        //            //StatusLabel.Text = "Getting device info";
        //            //var info = await movesense.GetDeviceInfoAsync(sensor.Name);
        //            //StatusLabel.Text = "Getting battery level";
        //            //var batt = await movesense.GetBatteryLevelAsync(sensor.Name);

        //            //// Turn on the LED
        //            //StatusLabel.Text = "Turning on LED";
        //            //await movesense.SetLedStateAsync(sensor.Name, 0, true);

        //            //await DisplayAlert(
        //            //    "Success",
        //            //    $"Communicated with device {sensor.Name}, firmware version is: {info.DeviceInfo.Sw}, battery: {batt.ChargePercent}",
        //            //    "OK");

        //            //// Turn the LED off again
        //            //StatusLabel.Text = "Turning off LED";
        //            //await movesense.SetLedStateAsync(sensor.Name, 0, false);

        //            //// Disconnect Mds
        //            //StatusLabel.Text = "Disconnecting";
        //            //await movesense.DisconnectMdsAsync(sensor.Uuid);
        //            //StatusLabel.Text = "Disconnected";

        //        }
        //    }
        //}

        void Handle_Toggled(object sender, Xamarin.Forms.ToggledEventArgs e)
        {
            if (!toggleFindDevices.IsToggled)
            {
                StopScanning(); // we need to stop scanning
            }
            else
            {
                if (BleAdapter.Status == AdapterStatus.PoweredOn)
                {
                    DoScan();
                }
                else
                {
                    BleAdapter.WhenStatusChanged().Subscribe(status =>
                    {
                        if (status == AdapterStatus.PoweredOn)
                        {
                            DoScan();
                        }
                    });
                }
            }
        }
    }
}

// {Binding Source={x:Reference toggleFindDevices}, Path=toggleFindDevices.IsToggled}
