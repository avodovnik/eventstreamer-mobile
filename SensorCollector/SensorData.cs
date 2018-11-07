using System;
using System.Linq;

namespace SensorCollector
{
    public class SensorData
    {
        public long timeStamp;
        public double accX;
        public double accY;
        public double accZ;
        public double gyroX;
        public double gyroY;
        public double gyroZ;
        public double magX;
        public double magY;
        public double magZ;

        public SensorData(MdsLibrary.Model.IMU9Data.Values3D[] accelerometer,
                          MdsLibrary.Model.IMU9Data.Values3D[] gyro,
                          MdsLibrary.Model.IMU9Data.Values3D[] magnetometer)
        {
            this.timeStamp = DateTime.UtcNow.Ticks;

            this.accX = accelerometer.Average(x => x.X);
            this.accY = accelerometer.Average(x => x.Y);
            this.accZ = accelerometer.Average(x => x.Z);

            this.gyroX = gyro.Average(x => x.X);
            this.gyroY = gyro.Average(x => x.Y);
            this.gyroZ = gyro.Average(x => x.Z);

            this.magX = magnetometer.Average(x => x.X);
            this.magY = magnetometer.Average(x => x.Y);
            this.magZ = magnetometer.Average(x => x.Z);
        }
    }
}
