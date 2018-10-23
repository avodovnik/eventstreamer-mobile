using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Xamarin.Forms;

using Plugin.BluetoothLE;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;

namespace SensorCollector
{
    public class MovesenseDevice
    {
        public string Name { get; set; }
        public string Serial { get { return this.Uuid.ToString(); } }
        public bool IsConnectable { get; set; }
        public int TxPower { get; private set; }
        public IDevice Device { get; private set; }
        public Guid Uuid { get; set; }

        // todo: add bt device holder
        public void TrySet(IScanResult result)
        {
            if (this.Uuid == Guid.Empty)
            {
                this.Device = result.Device;
                this.Uuid = this.Device.Uuid;
            }

            try
            {
                if (this.Uuid == result.Device.Uuid)
                {
                    this.Name = result.Device.Name;

                    var ad = result.AdvertisementData;
                    this.IsConnectable = ad.IsConnectable;
                    //this.LocalName = ad.LocalName;
                    this.TxPower = ad.TxPower;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }



    public partial class MainPage : ContentPage
    {
        public ObservableCollection<SelectableItem<MovesenseDevice>> DeviceList { get; set; }

        IDisposable scan;
        public IAdapter BleAdapter => CrossBleAdapter.Current;

        private void UpdateStatus(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                lblStatus.Text = message;
            });
            // todo: make an observable
        }

        public MainPage()
        {
            InitializeComponent();

            DeviceList = new ObservableCollection<SelectableItem<MovesenseDevice>>();
            this.BindingContext = this;
            this.lstDevices.ItemsSource = DeviceList;

        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                this.DeviceList.Clear();
            });

            if (this.IsScanning)
            {
                scan?.Dispose();
                this.IsScanning = false;
                this.UpdateStatus("Doing nothing...");
            }
            else
            {
                if(BleAdapter.Status == AdapterStatus.PoweredOn) {
                   DoScan();
                }
                else {
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


        private void DoScan() {
            UpdateStatus("Scanning for Movesense devices...");
            this.IsScanning = true;
            this.scan = this.BleAdapter
                .Scan()
                .Buffer(TimeSpan.FromSeconds(3))
                .Subscribe(results =>
                {
                    // doing this to prevent multiple sensors showing up
                    lock (this.DeviceList)
                    {
                        foreach (var result in results)
                        {
                            if (!(result.Device?.Name?.StartsWith("movesense", StringComparison.InvariantCultureIgnoreCase)).GetValueOrDefault(false))
                            {
                                // unless it's a Movesense device, just give up
                                continue;
                            }

                            var dev = this.DeviceList.FirstOrDefault(x => x.Data.Uuid.Equals(result.Device.Uuid));

                            if (dev != null)
                            {
                                dev.Data.TrySet(result);
                            }
                            else
                            {
                                dev = new SelectableItem<MovesenseDevice>(new MovesenseDevice());
                                dev.Data.TrySet(result);

                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    // for some reason, we need to double check this, again
                                    if (DeviceList.Any(x => x.Data.Uuid == dev.Data.Uuid)) return;

                                    this.DeviceList.Add(dev);
                                });
                            }
                        }
                    }
                });
        }

        public bool IsScanning { get; private set; }


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

    }
}

// {Binding Source={x:Reference toggleFindDevices}, Path=toggleFindDevices.IsToggled}
