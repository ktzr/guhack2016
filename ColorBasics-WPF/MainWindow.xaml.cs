//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Wpf.Controls;
    using Microsoft.Kinect.Toolkit.Input;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        Mode _mode = Mode.Return;
        #region Kinect Sensor Instantiation
        private KinectSensor kinectSensor = null;
        private ColorFrameReader colorFrameReader = null;
        private WriteableBitmap colorBitmap = null;
        private string statusText = null;
        #endregion


        #region Body Parts Instantiation
        private const double HandSize = 30;
        private const double JointThickness = 3;
        private const double ClipBoundsThickness = 10;
        private const float InferredZPositionClamp = 0.1f;
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private List<Tuple<JointType, JointType>> bones;
        private int displayWidth;
        private int displayHeight;
        private List<Pen> bodyColors;
        #endregion

        public MainWindow()
        {
            #region Kinect Instantiation
            this.kinectSensor = KinectSensor.GetDefault();
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            #endregion

            #region Body Parts Initialisation
            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));
            #endregion

            #region Kinect Sensor Open
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            this.drawingGroup = new DrawingGroup();
            this.imageSource = new DrawingImage(this.drawingGroup);
            this.DataContext = this;
            this.InitializeComponent();
            #endregion
        }

        #region Event Handler
        public event PropertyChangedEventHandler PropertyChanged;

        /// Gets the bitmap to display
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// Gets or sets the current status text to display
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        /// <summary>
        /// #################      Handles the color frame data arriving from the sensor    ########################
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// #####################       Handles the body frame data arriving from the sensor    #######################
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;


            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {

                    dc.DrawImage(colorBitmap, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    int penIndex = 0;

                    foreach (Body body in this.bodies)
                    {
                        Return_Exercise(body, dc);

                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            //this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            //this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        ///// <summary>
        ///// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        ///// </summary>
        ///// <param name="handState">state of the hand</param>
        ///// <param name="handPosition">position of the hand</param>
        ///// <param name="drawingContext">drawing context to draw to</param>
        //private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        //{
        //    switch (handState)
        //    {
        //        case HandState.Closed:
        //            drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
        //            break;

        //        case HandState.Open:
        //            drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
        //            break;

        //        case HandState.Lasso:
        //            drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
        //            break;
        //    }
        //}

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        #endregion

        #region Methods
        // Add erxercise generics


        //asume joint2 is the common joint, #todo add error checking
        //use cosine rule to find angle at joint 2

        /// <summary>
        /// Calculates angle of seperation between joint1 and 2 and joint2 and 3.
        /// in radians
        /// </summary>
        public double getAngleOfSeparation(Body body, JointType joint1, JointType joint2, JointType joint3)
        {

            double a = lengthBetweenJoints(body, joint2, joint3);
            double b = lengthBetweenJoints(body, joint1, joint2);
            double c = lengthBetweenJoints(body, joint1, joint3);

            return Math.Acos((a * a + b * b - c * c) / (2 * a * b)) * 180 / Math.PI;
        }

        /// <summary>
        /// Calculates length between two joints.
        /// </summary>
        public double lengthBetweenJoints(Body body, JointType joint1, JointType joint2)
        {
            double XDistance = body.Joints[joint1].Position.X - body.Joints[joint2].Position.X;
            double YDistance = body.Joints[joint1].Position.Y - body.Joints[joint2].Position.Y;



            return Math.Sqrt(YDistance * YDistance + XDistance * XDistance);

        }

        /// <summary>
        /// Returns true is spine is strate within tolerance.
        /// </summary>
        public Boolean isSpineStraight(Body body, double tolerance)
        {
              return tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.Neck, JointType.SpineShoulder, JointType.SpineMid)) ||
                   tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase));


        }

        /// <summary>
        /// Returns true is neck is strate within tolerance.
        /// </summary>
        public Boolean isNeckStraight(Body body, double tolerance)
        {
            // head,neck,spineShoulder
            return tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.Head, JointType.Neck, JointType.SpineShoulder));

        }
        #endregion

        bool exercise1Arm1 = false;
        bool exercise1Arm2 = false;

        #region Modes
        private void Exercise_1(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Exercise_1;
        }

        private void Exercise_2(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Exercise_2;
        }

        private void Exercise_3(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Exercise_3;
        }
        private void Return(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Return;
        }
        #endregion

        void Return_Exercise(Body body, DrawingContext dc)
        {
            if (_mode == Mode.Exercise_1)
            {
                if (exercise1Arm1 && exercise1Arm2)
                {
                    exercise1Arm1 = false;
                    exercise1Arm2 = false;
                    _mode = Mode.Return;
                }

                
                double startAngle = 120;
                double endAngle = 110;
                double angleTolerance = 5;
                double armTolerance = 15;

                //end when function returns 1
                int erxerciseCode = Exercise.moveLeftArm(body, angleTolerance, armTolerance, startAngle, endAngle);
                if (erxerciseCode != -72)
                {
                    switch (erxerciseCode)
                    {
                        case -1:
                            spineMsg.Visibility = System.Windows.Visibility.Visible;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            break;
                        case -2:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Visible;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            break;
                        case -100:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            Tuple<Point, Point> startPoints = Exercise.printStartProjection(body, startAngle);
                            dc.DrawLine(new Pen(Brushes.Blue, 13), startPoints.Item1, startPoints.Item2);
                            break;
                        case 1:
                            //ends the erxercise, say well done and all that good stuff
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Visible;
                            exercise1Arm1 = true;
                            break;
                        case -45:
                            Tuple<Point, Point> endPoints = Exercise.printEndProjection(body, endAngle);
                            dc.DrawLine(new Pen(Brushes.Blue, 13), endPoints.Item1, endPoints.Item2);
                            break;

                    }
                }


                //crowbar ex2 in here 
                int erxercise2Code = Exercise1Part2.moveRightArm(body, angleTolerance, armTolerance, startAngle, endAngle);
                if (erxercise2Code != -72)
                {
                    switch (erxercise2Code)
                    {
                        case -1:
                            spineMsg.Visibility = System.Windows.Visibility.Visible;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            break;
                        case -2:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Visible;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            break;
                        case -100:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            Tuple<Point, Point> startPoints = Exercise1Part2.printStartProjection(body, startAngle);
                            dc.DrawLine(new Pen(Brushes.Red, 13), startPoints.Item1, startPoints.Item2);
                            break;
                        case 1:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Visible;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            exercise1Arm2 = true;
                            break;
                        case -45:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            Tuple<Point, Point> endPoints = Exercise1Part2.printEndProjection(body, endAngle);
                            dc.DrawLine(new Pen(Brushes.Red, 13), endPoints.Item1, endPoints.Item2);
                            break;
                    }

                }


            }
            if (_mode == Mode.Exercise_2)
            {
                double angleTolerance = 5;
                double armTolerance = 15;


                int erxercise2op = Exercise2.bendLeftArm(body, angleTolerance, armTolerance);
                if (erxercise2op != -72)
                {
                    switch (erxercise2op)
                    {
                        case -1:
                            spineMsg.Visibility = System.Windows.Visibility.Visible;
                            break;
                        case -100:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            Tuple<Point, Point> startPoints = Exercise2.printStartProjection(body);
                            dc.DrawLine(new Pen(Brushes.Green, 13), startPoints.Item1, startPoints.Item2);
                            break;
                        case 1:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Visible;
                            _mode = Mode.Return;
                            break;
                        case -45:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                            endMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            Tuple<Point, Point, Point, Point> endPoints = Exercise2.printEndProjection(body);
                            dc.DrawLine(new Pen(Brushes.Green, 13), endPoints.Item1, endPoints.Item2);
                            dc.DrawLine(new Pen(Brushes.Green, 13), endPoints.Item3, endPoints.Item4);
                            break;
                        case 0:
                            spineMsg.Visibility = System.Windows.Visibility.Hidden;
                            larmMsg.Visibility = System.Windows.Visibility.Hidden;
                            break;

                    }
                }
            }
            if (_mode == Mode.Exercise_3)
            {
                {
                    double angleTolerance = 3;
                    double armTolerance = 3;


                    int erxercise3op = Exercise3.bendRightArm(body, angleTolerance, armTolerance);
                    if (erxercise3op != -72)
                    {
                        switch (erxercise3op)
                        {
                            case -1:
                                spineMsg.Visibility = System.Windows.Visibility.Visible;
                                break;
                            case -100:
                                spineMsg.Visibility = System.Windows.Visibility.Hidden;
                                larmMsg.Visibility = System.Windows.Visibility.Hidden;
                                Tuple<Point, Point> startPoints = Exercise3.printStartProjection(body);
                                dc.DrawLine(new Pen(Brushes.Green, 13), startPoints.Item1, startPoints.Item2);
                                break;
                            case 1:
                                spineMsg.Visibility = System.Windows.Visibility.Hidden;
                                larmMsg.Visibility = System.Windows.Visibility.Hidden;
                                endMsg.Visibility = System.Windows.Visibility.Visible;
                                _mode = Mode.Return;
                                break;
                            case -45:
                                Tuple<Point, Point, Point, Point> endPoints = Exercise3.printEndProjection(body);
                                dc.DrawLine(new Pen(Brushes.Green, 13), endPoints.Item1, endPoints.Item2);
                                dc.DrawLine(new Pen(Brushes.Green, 13), endPoints.Item3, endPoints.Item4);
                                break;
                            case 0:
                                spineMsg.Visibility = System.Windows.Visibility.Hidden;
                                larmMsg.Visibility = System.Windows.Visibility.Hidden;
                                break;

                        }
                    }
                }
            }
            if (_mode == Mode.Return)
            {
                Exercise.hasStarted = false;
                Exercise1Part2.hasStarted = false;
                spineMsg.Visibility = System.Windows.Visibility.Hidden;
                larmMsg.Visibility = System.Windows.Visibility.Hidden;
                rarmMsg.Visibility = System.Windows.Visibility.Hidden;
                endMsg.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
        }

        public enum Mode
        {
            Exercise_1,
            Exercise_2,
            Exercise_3,
            Return,
        }

    }
}
