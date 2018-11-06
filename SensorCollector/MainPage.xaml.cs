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
using System.Threading;
using Plugin.Movesense;
using Microsoft.Azure.EventHubs;
//using Confluent.Kafka;

// https://notetoself.tech/2018/06/03/acessing-event-hubs-with-confluent-kafka-library/
namespace SensorCollector
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<SelectableItem<MovesenseDevice>> DeviceList { get; set; }

        //public Confluent.Kafka.Producer<Confluent.Kafka.Null, string> _producer;

        //private Confluent.Kafka.Producer<Confluent.Kafka.Null, string> ProducerClient
        //{
        //    get
        //    {
        //        if (_producer == null)
        //        {
        //            if (string.IsNullOrEmpty(UserPreferences.Namespace))
        //            {
        //                DisplayAlert("Missing connection string",
        //                             "The connection string for the event hub is missing. Please enter it into Setting.",
        //                             "Close");

        //                return null;
        //            }

        //            var pConf = new ProducerConfig()
        //            {
        //                BootstrapServers = $"{UserPreferences.Namespace}.servicebus.windows.net:9093",
        //                SecurityProtocol = SecurityProtocolType.Sasl_Ssl,
        //                SaslMechanism = SaslMechanismType.Plain,
        //                GroupId = "$Default",
        //                SaslUsername = "$ConnectionString",
        //                SaslPassword = $"Endpoint=sb://{UserPreferences.Namespace}.servicebus.windows.net/;SharedAccessKeyName={UserPreferences.KeyName};SharedAccessKey={UserPreferences.KeyValue}",
        //                SslCaLocation = "cacert.pem"
        //            };

        //            _producer = new Confluent.Kafka.Producer<Confluent.Kafka.Null, string>(pConf);
        //        }

        //        return _producer;
        //    }
        //}

        IDisposable scan;
        public IAdapter BleAdapter => CrossBleAdapter.Current;

        internal void DisconnectAll()
        {
            this.StopStreaming().ContinueWith((arg) =>
            {
                System.Diagnostics.Debug.WriteLine("Disconnecting all the services.");
            });
        }

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


        private void DoScan()
        {
            // TODO: add proper observable binding, instead of this
            if (IsScanning) return;

            try
            {
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
            catch (Exception e)
            {
                ShowError(e);
            }
        }

        public bool IsScanning { get; private set; }
        private Thread _measureThread = null;

        private long _eventCount = 0;
        private Stopwatch _stopwatch = null;

        private ManualResetEvent _measureMre = new ManualResetEvent(false);

        private List<IMdsSubscription> _subscriptions = new List<IMdsSubscription>();

        private void DisplayStatistics()
        {
            do
            {
                var rate = _eventCount / Math.Max(_stopwatch.ElapsedMilliseconds, 1);
                Device.BeginInvokeOnMainThread(() =>
                {
                    lblStats.Text = $"Event Count: {_eventCount}, Throughput: {rate} e/ms";
                });
            } while (!_measureMre.WaitOne(TimeSpan.FromMilliseconds(500)));
        }

        private async Task StartStreaming()
        {
            _eventCount = 0;
            _stopwatch = new Stopwatch();

            _measureThread = new Thread(DisplayStatistics);
            _measureThread.Start();

            // start streaming
            // find all selected devices
            var items = this.DeviceList.Where(x => x.Selected).Select(x => x.Data);

            //if (ProducerClient == null)
            //{
            //    UpdateStatus("Aborting streaming.");
            //    return;
            //}

            foreach (var sensor in items)
            {
                UpdateStatus($"Connecting to {sensor.Name}");
                // make sure each device is connected
                if (sensor.DeviceStatus != DeviceStatus.Connected)
                {
                    try
                    {
                        await sensor.Connect();
                    }
                    catch (Exception e)
                    {
                        ShowError(e);
                    }
                }

                UpdateStatus($"Subscribing to {sensor.Name}");
                try
                {
                    //await Plugin.Movesense.CrossMovesense.Current.SetupLoggerAsync(sensor.Name);
                    //await Plugin.Movesense.CrossMovesense.Current.SetLoggerStatusAsync(sensor.Name, true);

                    var subscription = await Plugin.Movesense.CrossMovesense.Current.SubscribeIMU9Async(sensor.Name, async (data) =>
                    {
                        var o = new
                        {
                            Mag = data.body.ArrayMagn,
                            Acc = data.body.ArrayAcc,
                            Gyr = data.body.ArrayGyro
                        };


                        //await _producer.ProduceAsync(UserPreferences.EventHubName,
                        //new Message<Null, string>()
                        //{
                        //    Key = null,
                        //    Value = Newtonsoft.Json.JsonConvert.SerializeObject(o)
                        //});

                        EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString("test");

                        //var ed = new EventData(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(o)));
                        //await EventHubClient.SendAsync(ed);
                        _eventCount++;
                    });

                    lock (_subscriptions)
                    {
                        _subscriptions.Add(subscription);
                    }
                }
                catch (Exception e)
                {
                    ShowError(e);
                }
            }
        }

        private void ShowError(Exception e)
        {
            DisplayAlert("Error", $"There was an exception: {e.Message}", "Ah well...");
            Debug.WriteLine(e.StackTrace);
        }

        async Task StopStreaming()
        {
            _measureMre.Set();

            foreach (var sub in _subscriptions)
            {
                try
                {
                    sub.Unsubscribe();
                    UpdateStatus($"Unsubscribing");
                }
                catch (Exception e)
                {
                    ShowError(e);
                }
            }

            lock (_subscriptions)
            {
                _subscriptions.Clear();
            }

            // stop streaming
            var items = this.DeviceList.Where(x => x.Selected).Select(x => x.Data);

            foreach (var sensor in items)
            {
                try
                {
                    UpdateStatus($"Disconnecting {sensor.Name}");
                    await sensor.Disconnect();
                }
                catch (Exception e)
                {
                    ShowError(e);
                }
            }
        }

        async void Handle_Stream_Toggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                await StartStreaming();
            }
            else
            {

                await StopStreaming();
            }

            UpdateStatus("Finished");
        }

        async void Handle_Toggled(object sender, Xamarin.Forms.ToggledEventArgs e)
        {
            try
            {
                var s = sender as Xamarin.Forms.Switch;

                var model = s?.BindingContext as SelectableItem<MovesenseDevice>;
                if (model == null) return;
                var sensor = model.Data;

                if (e.Value)
                {
                    // connect
                    UpdateStatus("Connecting to device " + sensor.Name);

                    await model.Data.Connect();

                    await DisplayAlert(
                        "Success",
                        $"Communicated with device {sensor.Name}, " +
                        $"firmware version is: {sensor.FirmwareVersion}, " +
                        $"battery: {sensor.Battery}",
                        "OK");


                    await sensor.FlashLed();

                    //await movesense.DisconnectMdsAsync(sensor.Uuid);
                    UpdateStatus("Doing nothing...");
                }
                else
                {
                    await sensor.Disconnect();
                    UpdateStatus("Disconnected.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        void Handle_Clicked_Settings(object sender, System.EventArgs e)
        {
            Navigation.PushModalAsync(new SettingsPage());
        }
    }
}

// {Binding Source={x:Reference toggleFindDevices}, Path=toggleFindDevices.IsToggled}
