using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Globalization;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    static class CheckBodyForm
    {
        /// <summary>
        /// Calculates angle of seperation between joint1 and 2 and joint2 and 3.
        /// in radians
        /// </summary>
        public static double getAngleOfSeparation(Body body, JointType joint1, JointType joint2, JointType joint3)
        {

            double a = lengthBetweenJoints(body, joint2, joint3);
            double b = lengthBetweenJoints(body, joint1, joint2);
            double c = lengthBetweenJoints(body, joint1, joint3);

            double output = Math.Acos((a * a + b * b - c * c) / (2 * a * b)) * 180 / Math.PI;
            return output;
            /*
                        if (output.Equals(double.NaN))
                        {
                            return 180.0;
                        }
                        else
                        {
                        }*/
        }

        /// <summary>
        /// Calculates length between two joints.
        /// </summary>
        public static double lengthBetweenJoints(Body body, JointType joint1, JointType joint2)
        {
            double XDistance = body.Joints[joint1].Position.X - body.Joints[joint2].Position.X;
            double YDistance = body.Joints[joint1].Position.Y - body.Joints[joint2].Position.Y;



            return Math.Sqrt(YDistance * YDistance + XDistance * XDistance);

        }

        /// <summary>
        /// Returns true is 3 joints are strate within tolerance.
        /// </summary>
        public static Boolean isSraight(Body body, double tolerance, JointType joint1, JointType joint2, JointType joint3)
        {
            return tolerance > Math.Abs(180 - getAngleOfSeparation(body, joint1, joint2, joint3));



        }

        /// <summary>
        /// Returns true is 3 joints at angle, within tolerance.
        /// </summary>
        public static Boolean isAtAngle(Body body, double tolerance, double andgle, JointType joint1, JointType joint2, JointType joint3)
        {
            //fix this
            // neck,spineshoulder,spineMid,spineBase
            return tolerance < Math.Abs(andgle - getAngleOfSeparation(body, joint1, joint2, joint3));



        }


        /// <summary>
        /// Returns true is spine is strate within tolerance.
        /// </summary>
        public static Boolean isSpineStraight(Body body, double tolerance)
        {
            return tolerance > Math.Abs(180 - getAngleOfSeparation(body, JointType.Neck, JointType.SpineShoulder, JointType.SpineMid)) ||
                   tolerance > Math.Abs(180 - getAngleOfSeparation(body, JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase));


        }

        /// <summary>
        /// Returns true is neck is strate within tolerance.
        /// </summary>
        public static Boolean isNeckStraight(Body body, double tolerance)
        {
            // head,neck
            return tolerance > Math.Abs(180 - getAngleOfSeparation(body, JointType.Head, JointType.Neck, JointType.SpineShoulder));


        }



    }
    static class Exercise
    {
        static Boolean hasStarted = false;
        /// <summary>
        /// function returns 1 when task complete
        /// </summary>
        ///
        public static int moveLeftArm(Body body, double tolerance, double armTolerance, double startAngle, double endAngle)
        {

            JointType SpineShoulder = JointType.SpineShoulder;
            JointType ShoulderLeft = JointType.ShoulderLeft;
            JointType ElbowLeft = JointType.ElbowLeft;
            JointType WristLeft = JointType.WristLeft;

            JointType[] bodyParts = new JointType[] { SpineShoulder, ShoulderLeft, ElbowLeft, WristLeft };
            foreach (JointType part in bodyParts)
            {
                if (body.Joints[part].TrackingState != TrackingState.Tracked)
                {
                    return -72;
                }
            }

            if (!CheckBodyForm.isSpineStraight(body, tolerance))//&& !Exercise.hasStarted)
            {
                return -1;
            }
            if (!CheckBodyForm.isSraight(body, armTolerance, ShoulderLeft, ElbowLeft, WristLeft))//&& !Exercise.hasStarted)
            {
                return -2;
            }
            //draw box/line showing where arm should be
            if (Exercise.hasStarted && CheckBodyForm.isAtAngle(body, tolerance, endAngle, SpineShoulder, ShoulderLeft, ElbowLeft) &&
                body.Joints[WristLeft].Position.Y > body.Joints[ShoulderLeft].Position.Y)
            {
                return 1;
            }
            if (Exercise.hasStarted)
            {
                return -45;
            }
            if (CheckBodyForm.isAtAngle(body, tolerance, startAngle, SpineShoulder, ShoulderLeft, ElbowLeft) &&
                body.Joints[WristLeft].Position.Y < body.Joints[ShoulderLeft].Position.Y)
            {
                Exercise.hasStarted = true;
            }

            return -100;
        }
        public static Tuple<Point, Point> printStartProjection(Body body, double startAngle)
        {
            double accuteAngle = startAngle - 90.0;
            double sin = Math.Sin((Math.PI * accuteAngle) / 180);
            double cos = Math.Cos((Math.PI * accuteAngle) / 180);

            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderLeft].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));


            double projX = pointInDepthspace.X + linelenght * sin;
            double projY = pointInDepthspace.Y - linelenght * cos;

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point end = new Point(projX, projY);

            return Tuple.Create(start, end);

        }

        public static Tuple<Point, Point> printEndProjection(Body body, double endAngle)
        {

            double accuteAngle;

            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderLeft].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));

            //  if (endAngle <= 180)
            //  {
            accuteAngle = endAngle - 90.0;
            double projX = pointInDepthspace.X + linelenght * Math.Sin((Math.PI * accuteAngle) / 180);
            double projY = pointInDepthspace.Y + linelenght * Math.Cos((Math.PI * accuteAngle) / 180);
            //   }
            //  else
            //   {
            //        accuteAngle = endAngle - 180.0;
            //        double projX = pointInDepthspace.X + linelenght * Math.Cos((Math.PI * accuteAngle) / 180);
            //        double projY = pointInDepthspace.Y + linelenght * Math.Sin((Math.PI * accuteAngle) / 180);
            //    }

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point end = new Point(projX, projY);
            return Tuple.Create(start, end);

        }

    }


    static class Exercise1Part2
    {
        static Boolean hasStarted = false;
        /// <summary>
        /// function returns 1 when task complete
        /// </summary>
        ///
        public static int moveRightArm(Body body, double tolerance, double armTolerance, double startAngle, double endAngle)
        {

            JointType SpineShoulder = JointType.SpineShoulder;
            JointType ShoulderRight = JointType.ShoulderRight;
            JointType ElbowRight = JointType.ElbowRight;
            JointType WristRight = JointType.WristRight;

            JointType[] bodyParts = new JointType[] { SpineShoulder, ShoulderRight, ElbowRight, WristRight };
            foreach (JointType part in bodyParts)
            {
                if (body.Joints[part].TrackingState != TrackingState.Tracked)
                {
                    return -72;
                }
            }

            if (!CheckBodyForm.isSpineStraight(body, tolerance))//&& !Exercise.hasStarted)
            {
                return -1;
            }
            if (!CheckBodyForm.isSraight(body, armTolerance, ShoulderRight, ElbowRight, WristRight))//&& !Exercise.hasStarted)
            {
                return -2;
            }
            // (draw box/line showing where arm should be)
            if (Exercise1Part2.hasStarted && CheckBodyForm.isAtAngle(body, tolerance, endAngle, SpineShoulder, ShoulderRight, ElbowRight) &&
                body.Joints[WristRight].Position.Y > body.Joints[ShoulderRight].Position.Y)
            {
                return 1;
            }
            if (Exercise1Part2.hasStarted)
            {
                return -45;
            }
            if (CheckBodyForm.isAtAngle(body, tolerance, startAngle, SpineShoulder, ShoulderRight, ElbowRight) &&
                body.Joints[WristRight].Position.Y < body.Joints[ShoulderRight].Position.Y)
            {
                Exercise1Part2.hasStarted = true;
            }

            return -100;
        }
        public static Tuple<Point, Point> printStartProjection(Body body, double startAngle)
        {
            double accuteAngle = startAngle - 90.0;
            double sin = Math.Sin((Math.PI * accuteAngle) / 180);
            double cos = Math.Cos((Math.PI * accuteAngle) / 180);

            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderRight].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));


            double projX = pointInDepthspace.X - linelenght * sin;
            double projY = pointInDepthspace.Y - linelenght * cos;

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point end = new Point(projX, projY);


            return Tuple.Create(start, end);

        }

        public static Tuple<Point, Point> printEndProjection(Body body, double endAngle)
        {

            double accuteAngle;

            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderRight].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));

            //  if (endAngle <= 180)
            //  {
            accuteAngle = endAngle - 90.0;
            double projX = pointInDepthspace.X - linelenght * Math.Sin((Math.PI * accuteAngle) / 180);
            double projY = pointInDepthspace.Y + linelenght * Math.Cos((Math.PI * accuteAngle) / 180);
            //   }
            //  else
            //   {
            //        accuteAngle = endAngle - 180.0;
            //        double projX = pointInDepthspace.X - linelenght * Math.Cos((Math.PI * accuteAngle) / 180);
            //        double projY = pointInDepthspace.Y + linelenght * Math.Sin((Math.PI * accuteAngle) / 180);
            //    }

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point end = new Point(projX, projY);
            return Tuple.Create(start, end);

        }

    }

    static class Exercise2
    {
        static Boolean hasStarted = false;
        /// <summary>
        /// function returns 1 when task complete
        /// </summary>
        ///
        public static int bendLeftArm(Body body, double tolerance, double armTolerance)
        {

            JointType SpineShoulder = JointType.SpineShoulder;
            JointType ShoulderLeft = JointType.ShoulderLeft;
            JointType ElbowLeft = JointType.ElbowLeft;
            JointType WristLeft = JointType.WristLeft;

            JointType[] bodyParts = new JointType[] { SpineShoulder, ShoulderLeft, ElbowLeft, WristLeft };
            foreach (JointType part in bodyParts)
            {
                if (body.Joints[part].TrackingState != TrackingState.Tracked)
                {
                    return -72;
                }
            }

            if (!CheckBodyForm.isSpineStraight(body, tolerance))//&& !Exercise.hasStarted)
            {
                return -1;
            }

            if (hasStarted &&
                CheckBodyForm.isSraight(body, armTolerance, SpineShoulder, ShoulderLeft, ElbowLeft) &&
                CheckBodyForm.isAtAngle(body, tolerance, 90, ShoulderLeft, ElbowLeft, WristLeft) &&
                body.Joints[WristLeft].Position.Y > body.Joints[ShoulderLeft].Position.Y)
            {
                return 1;
            }
            if (hasStarted)
            {
                return -45;
            }
            if (CheckBodyForm.isSraight(body, armTolerance, SpineShoulder, ShoulderLeft, ElbowLeft))
            {
                hasStarted = true;
            }

            return -100;
        }
        public static Tuple<Point, Point> printStartProjection(Body body)//, double startAngle)
        {
            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderLeft].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));


            double projX = pointInDepthspace.X + linelenght;
            double projY = pointInDepthspace.Y;

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point end = new Point(projX, projY);



            return Tuple.Create(start, end);

        }

        public static Tuple<Point, Point, Point, Point> printEndProjection(Body body)
        {

            DepthSpacePoint pointInDepthspace = KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.ShoulderLeft].Position);
            double linelenght = ((KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position).Y) -
                (KinectSensor.GetDefault().CoordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineBase].Position).Y));


            double midX = pointInDepthspace.X + linelenght/2;
            double midY = pointInDepthspace.Y;

            double endX = pointInDepthspace.X + linelenght / 2;
            double endY = pointInDepthspace.Y + linelenght / 2;

            Point start = new Point(pointInDepthspace.X, pointInDepthspace.Y);
            Point mid = new Point(midX, midY);
            Point end = new Point(endX, endY);

            return Tuple.Create(start, mid, mid, end);

        }

    }

}