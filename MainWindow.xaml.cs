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

using Microsoft.Kinect;
using NUI3D;

namespace LaserLineVsSquare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private BodyFrameManager bodyFrameManager;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            sensor = KinectSensor.GetDefault(); // get the default Kinect sensor 
            sensor.Open();

            bodyFrameManager = new BodyFrameManager();
            bodyFrameManager.Init(sensor, skeletonImg, gameMenu, background, gameOver, score, restartLabel, colorVideo);
        }
    }
}
