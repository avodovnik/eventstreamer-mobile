using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Plugin.BluetoothLE;

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

        public async Task Connect()
        {
            if (DeviceStatus == DeviceStatus.Connecting
               || DeviceStatus == DeviceStatus.Connected)
                return;

            DeviceStatus = DeviceStatus.Connecting;
            this.Device.Connect();
            Debug.WriteLine("Ble Connected!");

            var movesense = Plugin.Movesense.CrossMovesense.Current;
            // Now do the Mds connection
            await movesense.ConnectMdsAsync(Uuid);

            DeviceStatus = DeviceStatus.Connected;

            // TODO: add battery indicator to the UI
            var info = await movesense.GetDeviceInfoAsync(Name);
            var batt = await movesense.GetBatteryLevelAsync(Name);

            this.Battery = batt.ChargePercent;
            this.FirmwareVersion = info.DeviceInfo.Sw;
        }

        public async Task Disconnect()
        {
            DeviceStatus = DeviceStatus.Connecting;

            // Disconnect Mds
            await Plugin.Movesense.CrossMovesense.Current.DisconnectMdsAsync(Uuid);

            // Disconnect SensorKit
            this.Device.CancelConnection();

            Debug.WriteLine("Ble DisConnected!");

            DeviceStatus = DeviceStatus.Discovered;
        }

        public async Task FlashLed() {
            var movesense = Plugin.Movesense.CrossMovesense.Current;

            await movesense.SetLedStateAsync(Name, 0, true);

            Thread.Sleep(500);

            await movesense.SetLedStateAsync(Name, 0, false);
        }

        public DeviceStatus DeviceStatus { get; set; }


        public int Battery { get; private set; }
        public string FirmwareVersion { get; private set; }
    }

    public enum DeviceStatus
    {
        Undefined,
        Discovered,
        Connecting,
        Connected,
        Inactive,
        Logging,
        Recording,
        RecordingDone,
        NotFound,
        Error
    }
}
