using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using Tobii.Gaze.Core;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace eyetrack
{
    public struct EyeData3D
    {
        public EyeData3D(GazeDataEye g)
        {            
            EyePosition = ToVector3d(g.EyePositionFromEyeTrackerMM);
            EyePositionBoxNorm = ToVector3d(g.EyePositionInTrackBoxNormalized);
            GazePosition = ToVector3d(g.GazePointFromEyeTrackerMM);
            GazePositionScreenNorm = ToVector3d(g.GazePointOnDisplayNormalized);
        }

        
        public Vector3d EyePosition;
        public Vector3d EyePositionBoxNorm;
        public Vector3d GazePosition;
        public Vector3d GazePositionScreenNorm;

        public static explicit operator EyeData3D(GazeDataEye b)  
        {
            EyeData3D d = new EyeData3D(b);  
            return d;
        }

        public static Vector3d ToVector3d(Point3D b)  
        {
            Vector3d d = new Vector3d(b.X, b.Y, b.Z);
            return d;
        }


        public static Vector3d ToVector3d(Point2D b)
        {
            Vector3d d = new Vector3d(b.X, b.Y, 0.0);
            return d;
        }

        public static EyeData3D operator +(EyeData3D c1, EyeData3D c2)
        {
            EyeData3D d = new EyeData3D();
            d.EyePosition = c1.EyePosition + c2.EyePosition;
            d.EyePositionBoxNorm = c1.EyePositionBoxNorm + c2.EyePositionBoxNorm;
            d.GazePosition = c1.GazePosition + c2.GazePosition;
            d.GazePositionScreenNorm = c1.GazePositionScreenNorm + c2.GazePositionScreenNorm;
            return d;
        }

        public static EyeData3D operator *(EyeData3D c1, double c2)
        {
            EyeData3D d = new EyeData3D();
            d.EyePosition = c1.EyePosition * c2;
            d.EyePositionBoxNorm = c1.EyePositionBoxNorm * c2;
            d.GazePosition = c1.GazePosition * c2;
            d.GazePositionScreenNorm = c1.GazePositionScreenNorm * c2;
            return d;
        }
    }

    public class EyeHelper
    {
        public Vector3d ScreenTopLeft;
        public Vector3d ScreenTopRight;
        public Vector3d ScreenBottomLeft;

        public Vector3d[] ScreenP = new Vector3d[4];
        public Vector3d ScreenCenter = new Vector3d();
        public Vector3d ScreenX = new Vector3d();
        public Vector3d ScreenY = new Vector3d();
        public Vector3d ScreenZ = new Vector3d();
        public double ScreenW = 0.0;
        public double ScreenH = 0.0;

        public Matrix4d EyeToScreen = new Matrix4d();

        virtual public void Init() {
          
        }

        virtual public void ShutDown()
        {
        }

        static protected EyeData3D eyeLeft;
        static protected EyeData3D eyeRight;

        static protected EyeData3D eyeLeftSmooth;
        static protected EyeData3D eyeRightSmooth;

        static public double MovementSmoothing = 0.85;

        virtual public EyeData3D EyeLeft
        {
            get
            {
                return eyeLeft;
            }
        }

        virtual public EyeData3D EyeRight
        {
            get
            {
                return eyeRight;
            }
        }

        virtual public EyeData3D EyeLeftSmooth
        {
            get
            {
                return eyeLeftSmooth;
            }
        }

        virtual public EyeData3D EyeRightSmooth
        {
            get
            {
                return eyeRightSmooth;
            }
        }

      

    }

    public class EyeHelperTOBII: EyeHelper
    {
        Uri TrackerURL;
        IEyeTracker Tracker = null;
        Thread TrackerThread = null;

       

        override public void Init()
        {
            base.Init();

            if (TrackerURL != null) return;
            TrackerURL = new EyeTrackerCoreLibrary().GetConnectedEyeTracker();

            try
            {
                Tracker = new EyeTracker(TrackerURL);

                Tracker.EyeTrackerError += EyeTrackerError;
                Tracker.GazeData += EyeTrackerGazeData;

                TrackerThread = CreateAndRunEventLoopThread(Tracker);

                Console.WriteLine("Connecting...");
                Tracker.Connect();
                Console.WriteLine("Connected");

                // Good habit to start by retrieving device info to check that communication works.
                PrintDeviceInfo(Tracker);

                DisplayArea da = Tracker.GetDisplayArea();
                ScreenBottomLeft = EyeData3D.ToVector3d(da.BottomLeft);
                ScreenTopRight = EyeData3D.ToVector3d(da.TopRight);
                ScreenTopLeft = EyeData3D.ToVector3d(da.TopLeft);

                ScreenCenter = (ScreenTopRight + ScreenBottomLeft) * 0.5;
                ScreenX = ScreenTopRight - ScreenTopLeft;
                ScreenY = ScreenTopLeft - ScreenBottomLeft;

                ScreenW = ScreenX.Length;
                ScreenH = ScreenY.Length;

                ScreenX.Normalize();
                ScreenY.Normalize();

                ScreenZ = Vector3d.Cross(ScreenX, ScreenY);
                ScreenZ.Normalize();

                ScreenY = Vector3d.Cross(ScreenZ, ScreenX);
                ScreenY.Normalize();

                Matrix4d mt = new Matrix4d();

                mt.M11 = ScreenX.X;
                mt.M21 = ScreenX.Y;
                mt.M31 = ScreenX.Z;

                mt.M12 = ScreenY.X;
                mt.M22 = ScreenY.Y;
                mt.M32 = ScreenY.Z;

                mt.M13 = ScreenZ.X;
                mt.M23 = ScreenZ.Y;
                mt.M33 = ScreenZ.Z;

                mt.M41 = 0.0; //translation
                mt.M42 = 0.0;
                mt.M43 = 0.0;

                mt.M14 = 0.0;
                mt.M24 = 0.0;
                mt.M34 = 0.0;
                mt.M44 = 1.0;


                EyeToScreen = (Matrix4d.CreateTranslation(-ScreenCenter.X, -ScreenCenter.Y, -ScreenCenter.Z)) * mt;

                /* Vector3d tl = Vector3d.TransformPosition(ScreenCenter, EyeToScreen);
                 Console.WriteLine(tl);

                 tl = Vector3d.TransformPosition(ScreenTopLeft, EyeToScreen);
                 Console.WriteLine(tl);

                 tl = Vector3d.TransformPosition(ScreenTopRight, EyeToScreen);
                 Console.WriteLine(tl);

                 tl = Vector3d.TransformPosition(ScreenBottomLeft, EyeToScreen);
                 Console.WriteLine(tl);*/

                Console.WriteLine("Start tracking...");
                Tracker.StartTracking();
                Console.WriteLine("Tracking started");


            }
            catch (EyeTrackerException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        override public void ShutDown()
        {
            
            if (Tracker == null) return;
            Tracker.StopTracking();
            Tracker.Disconnect();

            if (TrackerThread != null)
            {
                Tracker.BreakEventLoop();
                TrackerThread.Join();
            }

            if (Tracker != null)
            {
                Tracker.Dispose();
            }

            base.ShutDown();
        }

        private static void PrintDeviceInfo(IEyeTracker tracker)
        {
            var info = tracker.GetDeviceInfo();
            Console.WriteLine("Serial number: {0}", info.SerialNumber);

            var trackBox = tracker.GetTrackBox();
            Console.WriteLine("Track box front upper left ({0}, {1}, {2})", trackBox.FrontUpperLeftPoint.X, trackBox.FrontUpperLeftPoint.Y, trackBox.FrontUpperLeftPoint.Z);
        }

        private static Thread CreateAndRunEventLoopThread(IEyeTracker tracker)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    tracker.RunEventLoop();
                }
                catch (EyeTrackerException ex)
                {
                    Console.WriteLine("An error occurred in the eye tracker event loop: " + ex.Message);
                }

                Console.WriteLine("Leaving the event loop.");
            });

            thread.Start();

            return thread;
        }

        static private GazeData gazeData;

        public GazeData GazeData
        {
            get
            {
                return gazeData;
            }
        }


        private static void EyeTrackerGazeData(object sender, GazeDataEventArgs e)
        {
            gazeData = e.GazeData;

            // Console.Write("{0} ", gazeData.Timestamp / 1e6); // in seconds
            //Console.Write("{0} ", gazeData.TrackingStatus);

            if (gazeData.TrackingStatus == TrackingStatus.BothEyesTracked ||
                gazeData.TrackingStatus == TrackingStatus.OnlyLeftEyeTracked ||
                gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyLeft)
            {
                eyeLeft = (EyeData3D)gazeData.Left;
                eyeLeftSmooth = eyeLeftSmooth * MovementSmoothing + eyeLeft * (1.0 - MovementSmoothing);
                //Console.Write("[{0:N4},{1:N4}] ", gazeData.Left.GazePointOnDisplayNormalized.X, gazeData.Left.GazePointOnDisplayNormalized.Y);
            }
            else
            {
                //Console.Write("[-,-] ");
            }

            if (gazeData.TrackingStatus == TrackingStatus.BothEyesTracked ||
                gazeData.TrackingStatus == TrackingStatus.OnlyRightEyeTracked ||
                gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyRight)
            {
                eyeRight = (EyeData3D)gazeData.Right;
                eyeRightSmooth = eyeRightSmooth * MovementSmoothing + eyeRight * (1.0 - MovementSmoothing);
                //Console.Write("[{0:N4},{1:N4}] ", gazeData.Right.GazePointOnDisplayNormalized.X, gazeData.Right.GazePointOnDisplayNormalized.Y);
            }
            else
            {
                //Console.Write("[-,-] ");
            }

            //Console.WriteLine();
        }

        private static void EyeTrackerError(object sender, EyeTrackerErrorEventArgs e)
        {
            Console.WriteLine("ERROR: " + e.Message);
        }

    }

}
