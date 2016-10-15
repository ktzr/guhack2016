using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;


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

            return Math.Acos((a * a + b * b - c * c) / (2 * a * b)) * 180 / Math.PI;
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
            return tolerance < Math.Abs(180 - getAngleOfSeparation(body, joint1, joint2, joint3));



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
            return tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.Neck, JointType.SpineShoulder, JointType.SpineMid)) ||
                   tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase));


        }

        /// <summary>
        /// Returns true is neck is strate within tolerance. 
        /// </summary>
        public static Boolean isNeckStraight(Body body, double tolerance)
        {
            // head,neck,spineShoulder
            return tolerance < Math.Abs(180 - getAngleOfSeparation(body, JointType.Head, JointType.Neck, JointType.SpineShoulder));


        }



    }static class Exersise {
        static Boolean hasStarted = false;
        /// <summary>
        /// function returns 1 when task complete 
        /// </summary>
        /// 
        public static int moveLeftArm(Body body, double tolerance)
        {
            JointType SpineShoulder = JointType.SpineShoulder;
            JointType ShoulderLeft = JointType.ShoulderLeft;
            JointType ElbowLeft = JointType.ElbowLeft;
            JointType WristLeft = JointType.WristLeft;
            double startAngle = 120;
            double endAngle = 170; //todo let angle be reflex 


            if (!CheckBodyForm.isSpineStraight(body, tolerance))
            {
                return -1;
                //todo  msg strighten spine
            }
            if (!CheckBodyForm.isSraight(body, tolerance, ShoulderLeft, ElbowLeft, WristLeft))
            {
                return -2;
                //todo  msg strighten arm
            }
            if (!Exersise.hasStarted)
            {
                //todo prompt go to start angle (draw box/line showing where arm should be)

                if (CheckBodyForm.isAtAngle(body, tolerance, startAngle, SpineShoulder, ShoulderLeft, ElbowLeft))
                {
                    Exersise.hasStarted = true;
                }
            }
            else
            {
                //todo promp to go to end angle  (draw box/line showing where arm should be)
                if (CheckBodyForm.isAtAngle(body, tolerance, endAngle, SpineShoulder, ShoulderLeft, ElbowLeft))
                {
                    //todo congradulate
                    Exersise.hasStarted = false;
                    return 1; 
                }
            }
            return 0;
        }

    }
    class instructun
    {


    }

}
