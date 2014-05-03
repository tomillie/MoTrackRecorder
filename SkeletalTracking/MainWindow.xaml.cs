using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MoTrackRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool closing = false;
        private bool recording = false;
        private const int skeletonCount = 6;
        private Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        private List<JointType> joints = new List<JointType>(){ JointType.Head, 
                                                                JointType.ShoulderCenter, 
                                                                JointType.ShoulderRight, 
                                                                JointType.ShoulderLeft,
                                                                JointType.ElbowRight,
                                                                JointType.ElbowLeft,
                                                                JointType.WristRight,
                                                                JointType.WristLeft,
                                                                JointType.HandRight,
                                                                JointType.HandLeft,
                                                                JointType.Spine,
                                                                JointType.HipCenter,
                                                                JointType.HipRight,
                                                                JointType.HipLeft,
                                                                JointType.KneeRight,
                                                                JointType.KneeLeft,
                                                                JointType.AnkleRight,
                                                                JointType.AnkleLeft,
                                                                JointType.FootRight,
                                                                JointType.FootLeft    };

        private Dictionary<JointType, List<float>> xPosition = new Dictionary<JointType, List<float>>();
        private Dictionary<JointType, List<float>> yPosition = new Dictionary<JointType, List<float>>();
        private Dictionary<JointType, List<float>> zPosition = new Dictionary<JointType, List<float>>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            // Smoothing and correction implemented in Kinect SDK
            // This code snippet is commented due to comparison of my results with Kinect's

            //var parameters = new TransformSmoothParameters
            //{
            //    Smoothing = 0.3f,
            //    Correction = 0.0f,
            //    Prediction = 0.0f,
            //    JitterRadius = 1.0f,
            //    MaxDeviationRadius = 0.5f
            //};
            //sensor.SkeletonStream.Enable(parameters);


            sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            // Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                startRecording.IsEnabled = false;
                return;
            }

            // Enables START button
            if (!stopRecording.IsEnabled)
            {
                startRecording.IsEnabled = true;
            }

            GetCameraPoint(first, e);

        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

                // If recording, save positions of x, y and z in every frame (30 per second)
                if (recording)
                {
                    for (int i = 0; i < joints.Count; i++)
                    {
                        xPosition[joints[i]].Add(first.Joints[joints[i]].Position.X);
                        yPosition[joints[i]].Add(first.Joints[joints[i]].Position.Y);
                        zPosition[joints[i]].Add(first.Joints[joints[i]].Position.Z);
                    }
                }
            }
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                // Gets the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }
                    sensor.Dispose();
                }
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            Joint scaledJoint = joint.ScaleTo(1366, 768, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);

        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            StopKinect(kinectSensorChooser1.Kinect);
        }

        private void startRecording_Click(object sender, RoutedEventArgs e)
        {
            // Clear all
            for (int i = 0; i < joints.Count; i++)
            {
                xPosition.Clear();
                yPosition.Clear();
                zPosition.Clear();
            }

            // Initialize if it does not exist yet
            for (int i = 0; i < joints.Count; i++)
            {
                xPosition.Add(joints[i], new List<float>());
                yPosition.Add(joints[i], new List<float>());
                zPosition.Add(joints[i], new List<float>());
            }

            recording = true;
            // Hint label
            recordingLabel.Content = "Recording . . .";
            // Recording buttons
            startRecording.IsEnabled = false;
            stopRecording.IsEnabled = true;
            // Export buttons
            saveJsonButton.IsEnabled = false;
            saveXmlButton.IsEnabled = false;
            saveCsvButton.IsEnabled = false;
        }

        private void stopRecording_Click(object sender, RoutedEventArgs e)
        {
            recording = false;
            // Hint label
            recordingLabel.Content = "Stopped, you can export now";
            // Recording buttons
            stopRecording.IsEnabled = false;
            startRecording.IsEnabled = true;
            // Export buttons
            saveJsonButton.IsEnabled = true;
            saveXmlButton.IsEnabled = true;
            saveCsvButton.IsEnabled = true;
        }

        private void saveJsonButton_Click(object sender, RoutedEventArgs e)
        {
            workInBackground("JSON file (.*json)|*.json", toJson);
        }

        private void saveXmlButton_Click(object sender, RoutedEventArgs e)
        {
            workInBackground("XML file (.*xml)|*.xml", toXml);
        }

        private void saveCsvButton_Click(object sender, RoutedEventArgs e)
        {
            workInBackground("CSV file (.*csv)|*.csv", toCsv);
        }

        /**
         * Converts all positions of joints in all frames to JSON output.
         * 
         * Example of output: 
         * <pre>
         * {    
         *  "all":[
         *      {       
         *             "jointType":"Head",
         *             "position":[
         *                {
         *                   "frame":"1",
         *                   "x":"0,8966634",
         *                   "y":"1,025259",
         *                   "z":"2,306435"
         *                }
         *                . . . 
         * </pre>
         */
        private string toJson()
        {
            string jsonContent = "{\"all\":[";
            List<float> tempJointXPositions = new List<float>() { };
            List<float> tempJointYPositions = new List<float>() { };
            List<float> tempJointZPositions = new List<float>() { };

            for (int i = 0; i < joints.Count; i++)
            {
                jsonContent += "{\"jointType\":\"" + joints[i] + "\",\"position\":[";
                tempJointXPositions = xPosition[joints[i]];
                tempJointYPositions = yPosition[joints[i]];
                tempJointZPositions = zPosition[joints[i]];
                for (int j = 0; j < tempJointXPositions.Count; j++)
                {
                    jsonContent += "{\"frame\":\"" + (j + 1) + "\","
                                    + "\"x\":\"" + tempJointXPositions[j] + "\","
                                    + "\"y\":\"" + tempJointYPositions[j] + "\","
                                    + "\"z\":\"" + tempJointZPositions[j] + "\"}";
                    if (j != tempJointXPositions.Count - 1)
                    {
                        jsonContent += ",";
                    }
                }
                jsonContent += "]}";
                if (i != joints.Count - 1)
                {
                    jsonContent += ",";
                }
            }
            jsonContent += "]}";


            return jsonContent;
        }

        /**
         * Converts all positions of joints in all frames to XML output.
         * 
         * Example of output: 
         * <pre>
         *  <?xml version="1.0" encoding="UTF-8"?>
         *  <joints>
         *      <joint>
         *          <type>Head</type>
         *          <positions>
         *              <position>
         *                  <frame>1</frame>
         *                  <x>0,7269982</x>
         *                  <y>1,04084</y>
         *                  <z>2,300994</z>
         *              </position>   
         *              <position>
         *                  <frame>2</frame>
         *                  <x>0,727215</x>
         *                  <y>1,040738</y>
         *                  <z>2,301057</z>
         *              </position>
         *          </positions>
         *      </joint>
         *  </joints>
         * </pre>
         */
        private string toXml()
        {
            string xmlContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><joints>";
            List<float> tempJointXPositions = new List<float>() { };
            List<float> tempJointYPositions = new List<float>() { };
            List<float> tempJointZPositions = new List<float>() { };

            for (int i = 0; i < joints.Count; i++)
            {
                xmlContent += "<joint><type>" + joints[i] + "</type><positions>";
                tempJointXPositions = xPosition[joints[i]];
                tempJointYPositions = yPosition[joints[i]];
                tempJointZPositions = zPosition[joints[i]];
                for (int j = 0; j < tempJointXPositions.Count; j++)
                {
                    xmlContent += "<position>"
                                    + "<frame>" + (j + 1) + "</frame>"
                                    + "<x>" + tempJointXPositions[j] + "</x>"
                                    + "<y>" + tempJointYPositions[j] + "</y>"
                                    + "<z>" + tempJointZPositions[j] + "</z>"
                                    + "</position>";
                }
                xmlContent += "</positions></joint>";
            }
            xmlContent += "</joints>";

            return xmlContent;
        }

        /**
        * Converts all positions of joints in all frames to CSV output.
        * 
        * Example of output: 
        * <pre>
        *   Head, Head, Head, ShoulderRight, ShoulderRight, ShoulderRight, ShoulderCenter . . .
        *   "0,7779385","0,9686465","2,232532","0,777445","0,9685029","2,232491", "0,665211" . . .
        *   . . . (every frame written in new line)
        * </pre>
        */
        private string toCsv()
        {
            string csvContent = "";
            int numberOfFrames = 0;
            List<float> tempJointXPositions = new List<float>() { };
            List<float> tempJointYPositions = new List<float>() { };
            List<float> tempJointZPositions = new List<float>() { };


            int k = 0;
            for (int i = 0; i < joints.Count; i++)
            {


                for (int j = 0; j < 3; j++)
                {
                    csvContent += joints[i];

                    if (k != (joints.Count - 1) || j != 2)
                    {
                        csvContent += ",";
                    }
                    else
                    {
                        csvContent += Environment.NewLine;
                    }
                }

                k++;

            }


            numberOfFrames = xPosition[joints[0]].Count;

            for (int j = 0; j < numberOfFrames; j++)
            {

                k = 0;
                for (int i = 0; i < joints.Count; i++)
                {

                    tempJointXPositions = xPosition[joints[i]];
                    tempJointYPositions = yPosition[joints[i]];
                    tempJointZPositions = zPosition[joints[i]];

                    csvContent += "\"" + tempJointXPositions[j] + "\","
                                + "\"" + tempJointYPositions[j] + "\","
                                + "\"" + tempJointZPositions[j] + "\"";

                    if (k != (joints.Count - 1))
                    {
                        csvContent += ",";
                    }
                    else
                    {
                        csvContent += Environment.NewLine;
                    }

                    k++;

                }

            }

            return csvContent;
        }

        private void workInBackground(string filter, Func<string> exportMethod)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = filter;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                recordingLabel.Content = "Processing . . .";
                exportProgressBar.Visibility = Visibility.Visible;

                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs args)
                {
                    BackgroundWorker b = o as BackgroundWorker;
                    string name = dialog.FileName;
                    using (System.IO.FileStream fs = System.IO.File.Create(name))
                    {
                        byte[] info = new System.Text.UTF8Encoding(true).GetBytes(exportMethod());
                        fs.Write(info, 0, info.Length);
                    }
                });
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object o, RunWorkerCompletedEventArgs args)
                {
                    recordingLabel.Content = "Saved";
                    exportProgressBar.Visibility = Visibility.Hidden;
                });
                bw.RunWorkerAsync();
            }
        }

    }
}
