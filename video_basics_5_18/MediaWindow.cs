﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using C_sawapan_media;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Tobii.Gaze.Core;

namespace testmediasmall
{
    public class VFrame
    {
        public byte[, ,] pix_data = null; //[j2, i, k];
        //qualifiers
        public double avgr = 0.0;
        public double avgg = 0.0;
        public double avgb = 0.0;
        public List<float[]> maskAvgRGBColor;
        public double totalMovement = 0.0;
        public double domiHue = 0.0; //dominant Hue
        public RGBHisto HistoR;
        public RGBHisto HistoG;
        public RGBHisto HistoB;
        public int frameNumber;
        public double optFlowMovement;
        public Vector3d optFlowAngle;
        public double[] maskOpticalFlowMovement = new double[5];
        public Vector3d[] maskOpticalFlowVector = new Vector3d[5];
    }

    public class MediaWindow
    {
        //window property
        public static int rx = 0;
        public static int ry = 0;
        public int Width = 0;       //width of the viewport in pixels
        public int Height = 0;      //height of the viewport in pixels
        public double MouseX = 0.0; //location of the mouse along X
        public double MouseY = 0.0; //location of the mouse along Y

        //global control
        static bool checkVideo = false;
        static bool playbackmode = false;
        static int maxframes = 300;
        static int minframes = 40;
        static int skippedFrameRange = 10;
        static bool blackout = true;
        static int screenCount = 3;

        //data
        public static List<VFrame> Vframe_repository = new List<VFrame>();
        public static List<Screen> Screens = new List<Screen>();

        //infrastructure
        static VideoIN Video = new VideoIN(); //this object represents a single video input device [camera or video file]
        static VideoIN CalibrationVideo = new VideoIN(); 
        static EyeHelper EyeTracker = new EyeHelperTOBII();
        //C_View is a class defined in the C_geometry.cs file and is just a helper object for managing viewpoint / tragetpoint camera mechanics
        public GLutils.C_View Viewer = new GLutils.C_View();
        static ColorAnalysis RGBColor = new ColorAnalysis();
        static int frameNumber;

        //gaze property
        public static Vector3d dpoint = new Vector3d();
        static byte[] gazeColor = new byte[3];
        static Vector3d gazeOptFlowVector = new Vector3d(0.0, 0.0, 0.0);

        public void Initialize()
        {
            try
            {
                //     socket = IO.Socket("http://127.0.0.1:6789");
                EyeTracker.Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            //intialize Video capturing from primary camera [0] at a low resolution
            VideoIN.EnumCaptureDevices();

            //Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            //CalibrationVideo.StartVideoFile(@"C:\Users\anakano\Dropbox\__QuantitativeShare\final\countdown.avi");
            CalibrationVideo.StartVideoFile(@"C:\Users\anakano.WIN.000\Desktop\gsd6432\countdown.avi"); 
            //Video.StartVideoFile(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\birdman3_converted.avi");
            //Video.StartVideoFile(@"C:\Users\anakano\Dropbox\__QuantitativeShare\final\inception.avi");
            Video.StartVideoFile(@"C:\Users\anakano.WIN.000\Desktop\gsd6432\inception.avi");

            System.Threading.Thread.Sleep(2000);
            Video.SetResolution(120, 80);   //reduce resolution so that each frame is taken into the repository
            CalibrationVideo.SetResolution(360, 240);
        }
 
        public void Close()
        {
            EyeTracker.ShutDown(); 
            Video.Stop();
            CalibrationVideo.Stop();
        }

        public static int other(int n)
        {
            return (n + 1) % Screens.Count;
            //if (_lr == 0) { return 1; }
            //else if (_lr == 1) { return 0; }
            //else { return -1; }
            //if (n < Screens.Count - 1) { return n + 1; }
            //else { return 0; }
        }

        private static int sorter(VFrame a, VFrame b)
        {
            //return a.avgr.CompareTo(b.avgr);
            //return a.avgg.CompareTo(b.avgg);
            //return a.avgb.CompareTo(b.avgb);
            //return a.totalMovement.CompareTo(b.totalMovement);
            return a.domiHue.CompareTo(b.domiHue);

            //double aa = a.domiHue + a.totalMovement*0.1;
            //double bb = b.domiHue + b.totalMovement*0.1;
            //return aa.CompareTo(bb);
        }

        public static int domiHueTransition(int analyzedFrame, bool complementary)
        {
            int nf = 0;
            double minDomiHue = 10;
            double domiHueDiff;

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    //complementary
                    if (complementary)
                    {
                        domiHueDiff = Math.Abs((Vframe_repository[analyzedFrame].domiHue + 0.5) - Vframe_repository[i].domiHue);
                        if (domiHueDiff < minDomiHue)
                        {
                            minDomiHue = domiHueDiff;
                            nf = i; //if there are multiple frames with the same domiHue, then pick the earliest frame
                        }
                    }
                    else //pick the similar dominant color
                    {
                        domiHueDiff = Math.Abs(Vframe_repository[analyzedFrame].domiHue - Vframe_repository[i].domiHue);
                        if (domiHueDiff < minDomiHue)
                        {
                            minDomiHue = domiHueDiff;
                            nf = i; //if there are multiple frames with the same domiHue, then pick the earliest frame
                        }
                    }
                }
            }
            return nf;
        }

        public static int maskAvgRGBTransition(int analyzedFrame, int gazeMaskNum, byte[] gazeRGB, bool complementary)
        {
            int nf = 0;
            float minColorQuality = 255;
            float maxColorQuality = 0;

            byte colorQuality = gazeRGB.Max();
            int colorQualityIndex = gazeRGB.ToList().IndexOf(colorQuality); //pick between redness, greenness, blueness for the gaze

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    float frameColorQuality = Vframe_repository[i].maskAvgRGBColor[gazeMaskNum][colorQualityIndex];   //get colorQuality (red, blue, greenness) value in mask number matching the gaze
                    float colorQualityDiff = Math.Abs(colorQuality - frameColorQuality);

                    if (complementary) //pick the frame with the least of the qualifier (redness, greenness, blueness)
                    {
                        if (colorQualityDiff > maxColorQuality)
                        {
                            maxColorQuality = colorQualityDiff;
                            nf = i;
                        }
                    }
                    else if (colorQualityDiff < minColorQuality)
                    {
                        minColorQuality = colorQualityDiff;
                        nf = i;
                    }
                }
            }
            return nf;
        }

        public static int maskOpticalFlowTransition(int analyzedFrame, int gazeMaskNum, double gazeOptFlowMovement, Vector3d gazeOptFlowVector, bool complementary)
        {
            int nf = 0;
            double minTotalMovement = 100000;
            double maxTotalMovement = 0;
            double optFlowAngleDiff = 0.0;
            double totalMovementDiff = 0.0;

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    Vector3d gazeAngle = gazeOptFlowVector.Normalized();
                    Vector3d newFrameAngleVector = Vframe_repository[i].maskOpticalFlowVector[gazeMaskNum].Normalized();

                    totalMovementDiff = Math.Abs(gazeOptFlowMovement - Vframe_repository[i].maskOpticalFlowMovement[gazeMaskNum]);
                    optFlowAngleDiff = Vector3d.Dot(gazeOptFlowVector, newFrameAngleVector);    //1 if parallel

                    if (Math.Abs(optFlowAngleDiff) > 0.9) //angle difference should be ~ +/- 30 degree
                    {
                        if (complementary) //show still image if lots of movement, etc 
                        {
                            if (totalMovementDiff > maxTotalMovement)
                            {
                                maxTotalMovement = totalMovementDiff;
                                nf = i;
                            }
                        }
                        else if (totalMovementDiff < minTotalMovement)
                        {
                            minTotalMovement = totalMovementDiff;
                            nf = i;
                        }
                    }
                    else if (Math.Abs(optFlowAngleDiff) > 0.5) //angle difference should be ~ +/- 30 degree
                    {
                        if (complementary) //show still image if lots of movement, etc 
                        {
                            if (totalMovementDiff > maxTotalMovement)
                            {
                                maxTotalMovement = totalMovementDiff;
                                nf = i;
                            }
                        }
                        else if (totalMovementDiff < minTotalMovement)
                        {
                            minTotalMovement = totalMovementDiff;
                            nf = i;
                        }
                    }
                    else nf = analyzedFrame; // no other frame matches then don't switch scenes
                }
            }
            return nf;
        }

        public static int motionPictureWithGaze(int analyzedFrame)
        {
            int nf = 0;
            double minTotalMovement = 100000;
            double optFlowAngleDiff = 0.0;
            double totalMovementDiff = 0.0;

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    optFlowAngleDiff = Vector3d.Dot(gazeOptFlowVector, Vframe_repository[i].optFlowAngle);    //1 if parallel
                    totalMovementDiff = Math.Abs(Vframe_repository[analyzedFrame].totalMovement - Vframe_repository[i].totalMovement);

                    if (Math.Abs(optFlowAngleDiff) > 0.9) //angle difference should be within +/- 30 degree
                    {
                        if (totalMovementDiff < minTotalMovement)
                        {
                            minTotalMovement = totalMovementDiff;
                            nf = i;
                        }
                    }
                }
            }
            return nf;
        }

        public static int maskN(int _i, int _j)
        {
            int n = 0;
            if (_j > 0.75) { n = 4; }
            else if (_j < 0.25) { n = 3; }
            else if (_i < 0.25) { n = 1; }
            else if (_i > 0.75) { n = 2; }
            else { n = 0; }
            return n;
        }

        void CalculateGaze()
        {
            Vector3d lnorm = new Vector3d(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X,
                                              EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y, 0.0);
            Vector3d rnorm = new Vector3d(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X,
                                          EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y, 0.0);
            dpoint = (lnorm + rnorm) * 0.5;
            dpoint.Y = (1.0 - dpoint.Y) * ry;
            dpoint.X = (1.0 - dpoint.X) * rx;

            dpoint = new Vector3d(this.MouseX / (double)Width * (double)rx, this.MouseY / (double)Height * (double)ry, 0.0);////////CHANGE IT!!
        }

        void CreateScreens()
        {
            if (Video.IsVideoCapturing && Screens.Count == 0)
            {
                for (int i = 0; i < screenCount; i++)
                {
                    double l = (double)i * (double)rx / (double)screenCount;
                    double b = 0.0;//0.5 * (double)ry * (1.0 - (1.0 / (double)screenCount));
                    double w = (double)rx / (double)screenCount;
                    double h = (double)ry; /// (double)screenCount;
                    Screen sc = new Screen(i, l, b, w, h);
                    sc.tx0 = i / (double)screenCount;
                    sc.tx1 = (i + 1) / (double)screenCount;
                    sc.ty0 = 0.0;
                    sc.ty1 = 1.0;
                    Screens.Add(sc);
                }
            }
        }

        void PlayBack()
        {
            for (int i = 0; i < Screens.Count; i++)
            {
                Screens[i].OnTimeLapse(dpoint);
            }

            for (int i = 0; i < Screens.Count; i++)
            {
                if (Screens[i].ison) { Screens[i].DrawVbit(); }
            }
        }

        void VisualizeGaze()
        {
            double alpha_gaze = 0.8;
            double startFade_gaze = 20;
            GL.PointSize((float)(rx / 15));
            if (frameNumber > minframes - startFade_gaze) { GL.Color4(255.0 / 255.0, 165.0 / 255.0, 0.0, alpha_gaze * Math.Cos((minframes - frameNumber) / minframes) * 60 * Math.PI); }
            else { GL.Color4(255.0 / 255.0, 100.0 / 255.0, 0.0, alpha_gaze); }
            GL.Begin(PrimitiveType.Points);
            GL.Vertex2(dpoint.X, dpoint.Y);
            GL.End();
        }

        void DrawFullScreen(VideoIN vi)
        {
            VBitmap Videoimage = new VBitmap(rx, ry);
            Videoimage.FromVideo(vi);
            Videoimage.Draw(0.0, 0.0, rx, ry, 1.0);
        }

        void BuildVfRepo() /// need modify
        {
            VideoPixel[,] px = Video.Pixels;
            VFrame vf = new VFrame();
            frameNumber++;

            RGBColor.FrameUpdate(px, rx, ry);   //RGB histogram rendering in the ColorAnalysis file 

            //vf.frame_pix_data = (byte[, ,])RGBColor.imgdataBGR.Clone();
            int j2;
            vf.pix_data = new byte[ry, rx, 3];
            for (int k = 0; k < 3; k++)
            {
                for (int j = 0; j < ry; j++)
                {
                    j2 = ry - j - 1;
                    for (int i = 0; i < rx; i++)
                    {
                        vf.pix_data[j, i, k] = RGBColor.imgdataBGR[j2, i, k];
                    }
                }
            }

            vf.avgr = RGBColor.avgr;
            vf.avgg = RGBColor.avgg;
            vf.avgb = RGBColor.avgb;
            vf.maskAvgRGBColor = RGBColor.maskAvgRGBColor;
            vf.HistoR = RGBColor.HistoR;
            vf.HistoG = RGBColor.HistoG;
            vf.HistoB = RGBColor.HistoB;
            //vf.totalMovement = totalMovement;

            //////////////////////////////////////////////////////////////////////////////color palette code
            ColorQuant ColorQuantizer = new ColorQuant();
            Colormap initialCMap = ColorQuantizer.MedianCutQuantGeneral(vf, rx, ry, 20);    //sort by frequency
            Colormap DiffColorMap = ColorQuantizer.SortByDifference(initialCMap);   //sort intialCMap again by the different; distinct color
            Colormap HueColorMap = ColorQuantizer.SortByHue(initialCMap);   //sort intialCMap by hue
            var a = ColorQuantizer.TranslateHSV(DiffColorMap[0]);
            vf.domiHue = a[0];
            ////////////////////////////////////////////////////////////////////////end of color palette cod

            for (int j = 0; j < ry; ++j)
            {
                for (int i = 0; i < rx; ++i)
                {
                    //optical flow 
                    double diff = Math.Abs(px[j, i].V - px[j, i].V0);
                    vf.totalMovement += diff;

                    //average optical flow angle 
                    Vector3d angle = new Vector3d(px[j, i].mx, px[j, i].my, 0);
                    vf.optFlowAngle += angle;
                    vf.optFlowMovement /= (rx * ry);
                }
            }

            //optical flow for each mask 
            int[] maskPixCount = new int[5];
            for (int j = 0; j < ry; ++j)
            {
                for (int i = 0; i < rx; ++i)
                {
                    double diff = Math.Abs(px[j, i].V - px[j, i].V0);
                    Vector3d angle = new Vector3d(px[j, i].mx, px[j, i].my, 0);
                    maskPixCount[maskN(i, j)]++;
                    vf.maskOpticalFlowMovement[maskN(i, j)] += diff;
                    vf.maskOpticalFlowVector[maskN(i, j)] += angle;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                vf.maskOpticalFlowMovement[i] = vf.maskOpticalFlowMovement[i] / (double)maskPixCount[i];
            }
            Vframe_repository.Add(vf);
        }

        public void OnFrameUpdate()  //executed 20 times per second.
        {
            GL.ClearColor(0.0f, 0.6f, 0.6f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

            if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
            if (Video.NeedUpdate) Video.UpdateFrame(true); //recalculate the video frame if the camera got a new one
            rx = Video.ResX;
            ry = Video.ResY;

            if (Vframe_repository.Count >= minframes && !playbackmode)
            {
                CalibrationVideo.Stop();
				playbackmode = true;
                CreateScreens();
                Screens[0].ison = true;
            }
            if (playbackmode)
			{
                CalculateGaze();
                PlayBack();
            }
            else 
            {
                if (!CalibrationVideo.IsVideoCapturing) return;
                if (CalibrationVideo.NeedUpdate) CalibrationVideo.UpdateFrame(true);

                if (checkVideo) { DrawFullScreen (Video); }
                else { DrawFullScreen (CalibrationVideo); }
                VisualizeGaze();
            }

            if (Vframe_repository.Count <= maxframes)
            {
                BuildVfRepo();
            }

        }
    }
}

