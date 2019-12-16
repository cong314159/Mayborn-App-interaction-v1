using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
// other name space
using Microsoft.Kinect;

namespace mayborn_kinect_music_app_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // General
        private const float irSouceValueMax = (float)ushort.MaxValue;
        private const float irSourceScale = 0.5f;
        private const float irOutputValueMin = 0.01f;
        private const float irOutputValueMax = 1.0f;
        // Kinect related
        private KinectSensor sensor = null;
        private InfraredFrameReader irFrameReader = null;
        private ColorFrameReader crFrameReader = null;
        private FrameDescription irfd = null;
        private FrameDescription crfd = null;
        // Display related
        private WriteableBitmap irBitmap = null;
        private WriteableBitmap crBitmap = null;
        private string statusText = null;

        public MainWindow()
        {
            this.sensor = KinectSensor.GetDefault();
            this.irFrameReader = this.sensor.InfraredFrameSource.OpenReader();
            this.irFrameReader.FrameArrived += this.irFrameReader_FrameArrived;
            //this.crFrameReader = this.sensor.ColorFrameSource.OpenReader();
            //this.crFrameReader.FrameArrived += this.crFrameReader_FrameArrived;
            this.irfd = this.sensor.InfraredFrameSource.FrameDescription;
            this.crfd = this.sensor.ColorFrameSource.FrameDescription;
            this.irBitmap = new WriteableBitmap(this.irfd.Width, this.irfd.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
            this.crBitmap = new WriteableBitmap(this.crfd.Width, this.crfd.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.sensor.Open();
            this.statusText = this.sensor.IsAvailable ? "Kinect Running" : "Kinect NOT Available";
            // Mark explanation
            this.DataContext = this;
            this.InitializeComponent();
        }

        private void irFrameReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            using (InfraredFrame irFrame = e.FrameReference.AcquireFrame())
            {
                if(irFrame != null)
                {
                    using (Microsoft.Kinect.KinectBuffer irBuffer = irFrame.LockImageBuffer())
                    {
                        if((this.irfd.Width * this.irfd.Height == irBuffer.Size / this.irfd.BytesPerPixel) &&
                            (this.irfd.Width == this.irBitmap.PixelWidth) && (this.irfd.Height == this.irBitmap.PixelHeight))
                        {
                            this.irFrameDataProcess(irBuffer.UnderlyingBuffer, irBuffer.Size);
                        }
                    }
                }
            }
        }

        private unsafe void irFrameDataProcess(IntPtr underlyingBuffer, uint size)
        // the "unsafe" here is important
        {
            ushort* frameData = (ushort*)underlyingBuffer;
            this.irBitmap.Lock();
            float* backBuffer = (float*)this.irBitmap.BackBuffer;
            for(int i = 0; i < (int)(size / this.irfd.BytesPerPixel); i++) // i++ and ++i?
            {
                backBuffer[i] = Math.Min(irOutputValueMax, (frameData[i] / irSouceValueMax * irSourceScale * (1.0f - irOutputValueMin)) + irOutputValueMin);
            }
            this.irBitmap.AddDirtyRect(new Int32Rect(0, 0, this.irBitmap.PixelWidth, this.irBitmap.PixelHeight));
            this.irBitmap.Unlock();
        }

        public ImageSource irBitmap_ImageSource
        {
            get
            {
                return this.irBitmap;
            }
        }

        public ImageSource crBitmap_ImageSource
        {
            get
            {
                return this.crBitmap;
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.statusText = this.sensor.IsAvailable ? "Kinect Running" : "Kinect NOT Available";
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.irFrameReader != null)
            {
                this.irFrameReader.Dispose();
                this.irFrameReader = null;
            }

            if (this.sensor != null)
            {
                this.sensor.Close();
                this.sensor = null;
            }
        }
    }
}
