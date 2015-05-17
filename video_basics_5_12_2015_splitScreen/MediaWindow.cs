using System;
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
        public byte[, ,] frame_pix_data = null; //[j2, i, k];
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

    }

    public class MediaWindow
    {
        public int Width = 0;       //width of the viewport in pixels
        public int Height = 0;      //height of the viewport in pixels
        public double MouseX = 0.0; //location of the mouse along X
        public double MouseY = 0.0; //location of the mouse along Y

        //double alpha = 0.0;
        EyeHelper EyeTracker = new EyeHelperTOBII();/////////////////////////////////////////////////////////EYE TRACKER

        public static List<VFrame> Vframe_repository = new List<VFrame>();
        public static List<Screen> Screens = new List<Screen>();

        ColorAnalysis RGBColor = new ColorAnalysis();
        public int frameNumber;

        // An image object to hold the latest camera frame
        VBitmap Videoimage;// = new VBitmap(120, 120);
        //this object represents a single video input device [camera or video file]
        public static VideoIN Video = new VideoIN();

        StreamWriter sw;
        //C_View is a class defined in the C_geometry.cs file and is just a helper object for managing viewpoint / tragetpoint camera mechanics
        public GLutils.C_View Viewer = new GLutils.C_View();

        //initialization function. Everything you write here is executed once in the begining of the program
        public void Initialize()
        {
            //////////////////////////////////////////////////////////////////////////////////////////////EYE TRACKER INIT
            try
            {
                //     socket = IO.Socket("http://127.0.0.1:6789");
                EyeTracker.Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            ///////////////////////////////////////////////////////////////////////////////////END OF EYE TRACKER INIT
            //intialize Video capturing from primary camera [0] at a low resolution
            VideoIN.EnumCaptureDevices();
            //test
            //...using webcam inputs
            //Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            //...use video

            //Video.StartVideoFile(@"C:\Users\anakano\Dropbox\__QuantitativeShare\final\inception.avi");
            //Video.StartVideoFile(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\quantitative_aesthetics\video_basics_5_12_2015\_testVideo2.avi");
            Video.StartVideoFile(@"C:\Users\anakano.WIN.000\Desktop\gsd6432\inception.avi");
            System.Threading.Thread.Sleep(500);
            Video.SetResolution(360, 240);

            sw = new StreamWriter(@"frame_111.csv");
        }
        public void Close()
        {
            sw.Close();
            EyeTracker.ShutDown();
        }

        //window property
        public static int rx = 0;
        public static int ry = 0;

        //gaze property
        int lr = -1; //focusing on left or right image
        byte[] gazeColor = new byte[3];
        public static Vector3d dpointNorm = new Vector3d();

        //global control
        static bool playbackmode = false;
        static int maxframes = 180;
        static int skippedFrameRange = 10;
        static bool blackout = true;
        static int screenCount = 3;
        
        public static int other(int n)
        {
            //if (_lr == 0) { return 1; }
            //else if (_lr == 1) { return 0; }
            //else { return -1; }
            if (n < Screens.Count -1) { return n + 1; }
            else { return 0; }
        }

        public int sorter(VFrame a, VFrame b)
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

        public static int maskAvgRGBTransition(int analyzedFrame, int gazeMaskNum, byte[] gazeRGB)
        {
            int nf = 0;
            float minColorQuality = 255;

            byte colorQuality = gazeRGB.Max();
            int colorQualityIndex = gazeRGB.ToList().IndexOf(colorQuality); //pick between redness, greenness, blueness for the gaze

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    float frameColorQuality = Vframe_repository[i].maskAvgRGBColor[gazeMaskNum][colorQualityIndex];   //get colorQuality (red, blue, greenness) value in mask number matching the gaze
                    float colorQualityDiff = Math.Abs(colorQuality - frameColorQuality);
                    if (colorQualityDiff < minColorQuality)
                    {
                        minColorQuality = colorQualityDiff;
                        nf = i;
                    }
                }
            }
            return nf;
        }

        //animation function. This contains code executed 20 times per second.
        public void OnFrameUpdate()
        {
            if (Vframe_repository.Count >= maxframes && !playbackmode)
            {
                playbackmode = true;
                if (Video.IsVideoCapturing && Screens.Count == 0)
                {
                    for (int i = 0; i < screenCount; i++)
                    {
                        double l = (double)i * (double)rx / (double)screenCount;
                        double b = 0.5 * (double)ry * (1.0 - (1.0 / (double)screenCount));
                        double w = (double)rx / (double)screenCount;
                        double h = (double)ry / (double)screenCount;
                        Screens.Add(new Screen(i, l, b, w, h));

                    }
                }
                Screens[0].ison = true;
            }
            if (playbackmode)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////gaze calculation
                
                Vector3d lnorm = new Vector3d(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X,
                                              EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y, 0.0);
                Vector3d rnorm = new Vector3d(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X,
                                              EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y, 0.0);
                dpointNorm = (lnorm + rnorm) * 0.5;
                dpointNorm.Y = (1.0 - dpointNorm.Y) * ry;
                dpointNorm.X = (1.0 - dpointNorm.X) * rx;

                dpointNorm = new Vector3d(MouseX / (double)Width * (double)rx, MouseY / (double)Height*(double)ry, 0.0);////////CHANGE IT!!

                GL.ClearColor(0.0f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

                for (int i = 0; i < Screens.Count; i++)
                {
                    Screens[i].OnTimeLapse(dpointNorm);
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////drawing for each screen
                bool hasZoomingScreen = false;
                for (int i = 0; i < Screens.Count; i++)
                {
                    if (Screens[i].iszooming) //the zooming vbit must be drawn first
                    {
                        hasZoomingScreen = true;
                        Screens[i].DrawVbit();
                        Screens[other(i)].DrawVbit();
                        break;
                    }
                }
                if (!hasZoomingScreen)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (Screens[i].ison) //the zooming vbit must be drawn first
                        {
                            Screens[i].DrawVbit();
                        }
                    }
                }
                GL.PointSize(30.0f);///////////////////////////////////////////////////////////////////////////////////////////VISUALIZE GAZE
                GL.Color4(gazeColor[0] / 255.0, gazeColor[1] / 255.0, gazeColor[2] / 255.0, 1.0);
                GL.Begin(PrimitiveType.Points);
                //GL.Vertex2(focus[0], focus[1] );
                GL.Vertex2(dpointNorm.X * rx, dpointNorm.Y * ry);
                GL.End();

                if (blackout)
                {
                    Random rnd = new Random();
                    //draw points near the gaze point
                    float block = (float)rx * 0.1f;
                    double blockRadius = 5.0;
                    int sampleRadius = 50;
                    Vector3d blockPoint = new Vector3d(rnd.Next(0, sampleRadius), rnd.Next(0, sampleRadius), 0.0);

                }
                ///////////////////////////////////////////////////////////////////////////////////////////////////end of interaction

                //////////////////////////////////////////////////////////////////////////////////////////////////////.//////draw black cover
                GL.Color4(0.0, 0.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex2(0, 0);
                GL.Vertex2(rx, 0);
                GL.Vertex2(rx, ry/4);
                GL.Vertex2(0, ry/4);

                GL.Vertex2(0, 3*ry/4);
                GL.Vertex2(rx, 3 * ry / 4);
                GL.Vertex2(rx, ry);
                GL.Vertex2(0, ry);
                GL.End();
            }

            else
            {
                if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
                //recalculate the video frame if the camera got a new one
                if (Video.NeedUpdate) Video.UpdateFrame(true);

                VideoPixel[,] px = Video.Pixels;
                rx = Video.ResX;
                ry = Video.ResY;
                Videoimage = new VBitmap(rx, ry);
                Videoimage.FromVideo(Video);

                GL.ClearColor(0.6f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Projection);
                //GL.RasterPos2()
                //GL.PixelZoom()
                GL.LoadIdentity();
                GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

                //initialization to create histogram from bins (alternative to openCV)
                /*List<int> RGB_bin = new List<int>();
                for (int i = 0; i < 10; i++)
                {
                    RGB_bin.Add(0);
                }
                */

                Videoimage.Draw(0.0, 0.0, rx, ry, 1.0);
                double totalMovement = 0.0;

                for (int j = 0; j < ry; ++j)
                {
                    for (int i = 0; i < rx; ++i)
                    {
                        //draw screen
                        //GL.PointSize((float)(1.0 + Video.Pixels[j, i].V * 20.0));
                        GL.PointSize((float)(rx));
                        //draw pixels
                        GL.Color4(px[j, i].R, px[j, i].G, px[j, i].B, 1.0);
                        GL.Begin(PrimitiveType.Points);
                        GL.Vertex2(i, j);
                        GL.End();
                    }
                }
                sw.WriteLine();
                frameNumber++;

                RGBColor.FrameUpdate(px, rx, ry);

                for (int j = 0; j < ry; ++j)
                {
                    for (int i = 0; i < rx; ++i)
                    {
                        //optical flow 
                        double diff = Math.Abs(px[j, i].V - px[j, i].V0);
                        //angle 
                        double optFlowAngle = Math.Atan2(px[j, i].mx, px[j, i].my);
                        totalMovement += diff;

                        //draw movement 
                        //GL.PointSize((float)(diff * 10.0));
                        //GL.Color4(1.0, 0.0, 0.0, 0.5);
                        //GL.Begin(PrimitiveType.Points);
                        //GL.Vertex2(i * Width/rx, j * Height/ry);
                        //GL.End();
                    }
                }
                VFrame vf = new VFrame();
                //vf.frame_pix_data = (byte[, ,])RGBColor.imgdataBGR.Clone();
                int j2;
                vf.frame_pix_data = new byte[ry, rx, 3];
                for (int k = 0; k < 3; k++)
                {
                    for (int j = 0; j < ry; j++)
                    {
                        j2 = ry - j - 1;
                        for (int i = 0; i < rx; i++)
                        {
                            vf.frame_pix_data[j, i, k] = RGBColor.imgdataBGR[j2, i, k];
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
                vf.totalMovement = totalMovement;

                double cBlue = CvInvoke.cvCompareHist(vf.HistoR.Histogram, vf.HistoB.Histogram, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
                //////////////////////////////////////////////////////////////////////////////color palette code
                ColorQuant ColorQuantizer = new ColorQuant();
                Colormap initialCMap = ColorQuantizer.MedianCutQuantGeneral(vf, rx, ry, 20);    //sort by frequency
                Colormap DiffColorMap = ColorQuantizer.SortByDifference(initialCMap);   //sort intialCMap again by the different; distinct color
                Colormap HueColorMap = ColorQuantizer.SortByHue(initialCMap);   //sort intialCMap by hue
                var a = ColorQuantizer.TranslateHSV(DiffColorMap[0]);
                vf.domiHue = a[0];

                ////////////////////////////////////////////////////////////////////////end of color palette cod


                ///////////////////////////////////////////////////////////////////optical flow
                //double totalMovement = 0.0;
                //for (int j = 0; j < ry; ++j)
                //{
                //    for (int i = 0; i < rx; ++i)
                //    {
                //        double diff = Math.Abs(px[j, i].V - px[j, i].V0);
                //        GL.PointSize((float)(diff * 50.0));
                //        GL.Color4(1.0, 0.0, 0.0, 0.5);
                //        GL.Begin(PrimitiveType.Points);
                //        GL.Vertex2(i , j );
                //        GL.End();

                //        //angle 
                //        double optFlowAngle = Math.Atan2(px[j, i].mx, px[j, i].my);
                //        // optFlowAngle +=  

                //        totalMovement += diff;
                //    }
                //}
                //vf.totalMovement = totalMovement;
                /////////////////////////////////////////////////////////////end of optical flow

                Vframe_repository.Add(vf);
            }
        }
    }
}

