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
        public byte[, ,] pix_data = null; //[j2, i, k];
        public byte[, ,] recreate_pix_data = null; //[j2, i, k];
        //qualifiers
        public double avgr = 0.0;
        public double avgg = 0.0;
        public double avgb = 0.0;
        public List<float[]> maskAvgRGBColor;
        public double totalMovement = 0.0;
        public double domiHue = 0.0;
        public Colormap initialCMap; 
        public Colormap DiffColorMap; 
        public RGBHisto HistoR;
        public RGBHisto HistoG;
        public RGBHisto HistoB;
        public int frameNumber;
        public Vector2d motionCentroid = new Vector2d();
        public double[] motionMaskSum = new double[5];
        public Vector2d[] motionMaskDir = new Vector2d [5];
        public Vector2d motionDir;
        public Vector2d mDirSmth;
    }

    public class MediaWindow
    {
        int aaa = 0;
        VBitmap vbit;

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
        public static bool multipleScreen = false;
        static int maxframes = 1200;
        static int minframes = 1200;

        static int skippedFrameRange = 10;
        public static int screenCount = 3;

        //data
        public static List<VFrame> Vframe_repository = new List<VFrame>();
        public static List<Screen> Screens = new List<Screen>();

        //infrastructure
        static VideoIN Video = new VideoIN(); //this object represents a single video input device [camera or video file]
        static VideoIN CalibrationVideo = new VideoIN(); 
        static EyeHelper EyeTracker = new EyeHelperTOBII();
        //C_View is a class defined in the C_geometry.cs file and is just a helper object for managing viewpoint / tragetpoint camera mechanics
        public GLutils.C_View Viewer = new GLutils.C_View();
        static CvAnalysis CV = new CvAnalysis();
        static ColorQuant ColorQuantizer = new ColorQuant();
        Colormap initialCMap = new Colormap();
        Colormap diffColorMap = new Colormap();
        static int frameNumber;

        //gaze property
        public static Vector3d dpoint = new Vector3d();
        public static List<Vector3d> gazeL = new List<Vector3d>();
        static byte[] gazeColor = new byte[3];
        static Vector3d gazeOptFlowVector = new Vector3d(0.0, 0.0, 0.0);
        int lastf_gazeMedium = 8;
        public static Vector3d gazeMedium = new Vector3d();
        public static double deviation = 0.0;

        //computer setup
        public bool Laptop = true;
        
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
            VideoIN.EnumCaptureDevices();
            //Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            if (Laptop)
            {
                CalibrationVideo.StartVideoFile(@"C:\Users\anakano\Dropbox\__QuantitativeShare\final\countdown.avi");
                //Video.StartVideoFile(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\videos\birdman3_converted.avi");
                Video.StartVideoFile(@"C:\Users\anakano\Dropbox\__QuantitativeShare\final\inception.avi");
                //Video.StartVideoFile(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\videos\Chungking_Express\Chungking_Express_converted.avi");
            }
            else
            {
                CalibrationVideo.StartVideoFile(@"C:\Users\anakano.WIN.000\Desktop\gsd6432\countdown.avi");
                Video.StartVideoFile(@"C:\Users\anakano.WIN.000\Desktop\gsd6432\inception.avi");
            }

            System.Threading.Thread.Sleep(2000);
            Video.SetResolution(360, 240);   //reduce resolution so that each frame is taken into the repository
            CalibrationVideo.SetResolution(360, 240);
        }
 
        public void Close()
        {
            EyeTracker.ShutDown(); 
            Video.Stop();
            CalibrationVideo.Stop();
            //sw.Close();
            sr.Close();
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

        //not used
        public static int maskAvgRGBTransition(int analyzedFrame, int gazeMaskNum, byte[] gazeRGB, bool complementary)
        {
            int nf = 0;
            double minColorQuality = 255;
            double maxColorQuality = 0;

            byte cq = gazeRGB.Max();
            int colorQualityIndex = gazeRGB.ToList().IndexOf(cq); //pick between rgb for the gaze
            double colorQuality = (double)cq;
            for (int i = 0; i < 3; i++)
            {
                if (i != colorQualityIndex) { colorQuality -= 0.5 * (double)gazeRGB[i]; }
            }
            colorQuality /= 255.0;
            

            for (int i = 0; i < Vframe_repository.Count; i++)
            {
                if (i > analyzedFrame + skippedFrameRange || i < analyzedFrame - skippedFrameRange) //skip the analyzed frame 
                {
                    double frameColorQuality = Vframe_repository[i].maskAvgRGBColor[gazeMaskNum][colorQualityIndex];   //get colorQuality (red, blue, greenness) value in mask number matching the gaze
                    double colorQualityDiff = Math.Abs(colorQuality - frameColorQuality);

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
        //end of not used

        public static int maskN(double _i, double _j)
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
            dpoint.X = dpoint.X * rx;
            if (!Laptop) { dpoint = new Vector3d(this.MouseX / (double)Width * (double)rx, this.MouseY / (double)Height * (double)ry, 0.0); }
            gazeL.Add(dpoint);
            if (gazeL.Count > 30) { gazeL.RemoveAt(0); }

            //gaze medium
            if (gazeL.Count >= lastf_gazeMedium)
            {
                gazeMedium = new Vector3d(0.0, 0.0, 0.0);
                deviation = 0;
                for (int i = 0; i < lastf_gazeMedium; i++) { gazeMedium += gazeL[gazeL.Count - i - 1]; }
                gazeMedium *= (1.0 / lastf_gazeMedium);
                for (int i = 0; i < lastf_gazeMedium; i++) { deviation += (gazeL[gazeL.Count - i - 1] - gazeMedium).LengthSquared; } //"standard dev"
                deviation = Math.Sqrt(deviation);
            }

        }

        void CreateScreens()
        {
            if (Video.IsVideoCapturing && Screens.Count == 0)
            {
                for (int i = 0; i < screenCount; i++)
                {
                    if (multipleScreen)
                    {
                        double l = (double)i * (double)rx / (double)screenCount;
                        double b = 0.0;//0.5 * (double)ry * (1.0 - (1.0 / (double)screenCount));
                        double w = (double)rx / (double)screenCount;
                        double h = (double)ry; /// (double)screenCount;
                        Screen sc = new Screen(i, l, b, w, h);

                        sc.tx0 = (double)i / (double)screenCount;
                        sc.tx1 = ((double)i + 1.0) / (double)screenCount;

                        sc.ty0 = 0.0;
                        sc.ty1 = 1.0;
                        Screens.Add(sc);
                        Screen.sequenceDurationEnlarge = 3.0;
                    }
                    else {
                        double l = 0.0;
                        double b = 0.0;//0.5 * (double)ry * (1.0 - (1.0 / (double)screenCount));
                        double w = (double)rx;
                        double h = (double)ry; /// (double)screenCount;
                        Screen sc = new Screen(i, l, b, w, h);

                        sc.tx0 = 0.0;
                        sc.tx1 = 1.0; 

                        sc.ty0 = 0.0;
                        sc.ty1 = 1.0;
                        Screens.Add(sc);
                        Screen.sequenceDurationEnlarge = 1.0;
                    }
                }
            }
        }

        void PlayBack()
        {

            //Screen.framecount++;
            //for (int i = 0; i < Screens.Count; i++)
            //{
            //    Screens[i].OnTimeLapse(dpoint);
            //}
            //for (int i = 0; i < Screens.Count; i++)
            //{
            //    Screens[i].FrameUpdate();
            //}
            //for (int i = 0; i < Screens.Count; i++)
            //{
            //    if (Screens[i].ison) { Screens[i].DrawVbit(); }
            //}

            //aaa++;
            //Console.WriteLine("aaa = "+aaa);
            //byte[, ,] pre_px = Vframe_repository[aaa].pix_data;

            //VBitmap Videoimage = new VBitmap(rx, ry);
            //Videoimage.FromFrame(pre_px);
            //Videoimage.Draw(0.0, 0.0, rx, ry, 1.0);
        }

        void VisualizeGaze(Vector2d p)
        {
            double alpha_gaze = 0.8;
            double startFade_gaze = 20;
            GL.PointSize((float)(rx / 15));
            if (frameNumber > minframes - startFade_gaze) { GL.Color4(255.0 / 255.0, 165.0 / 255.0, 0.0, alpha_gaze * Math.Cos((minframes - frameNumber) / minframes) * 60 * Math.PI); }
            else { GL.Color4(255.0 / 255.0, 100.0 / 255.0, 0.0, alpha_gaze); }
            GL.Begin(PrimitiveType.Points);
            GL.Vertex2(p.X, p.Y);
            //GL.Vertex2(CV.motionCentroid.X , CV.motionCentroid.Y);    //motion centroid check
            GL.End();
        }

        void DrawFullScreen(VideoIN vi)
        {
            VBitmap Videoimage = new VBitmap(rx, ry);
            Videoimage.FromVideo(vi);
            Videoimage.Draw(0.0, 0.0, rx, ry, 1.0);
        }

        void BuildVfRepo() 
        {
            VideoPixel[,] px = Video.Pixels;
            VFrame vf = new VFrame();
            CV.FrameUpdate(px, rx, ry);   

            vf.initialCMap = initialCMap;
            vf.DiffColorMap = diffColorMap;
            var a = ColorQuantizer.TranslateHSV(vf.DiffColorMap[0]);
            vf.domiHue = a[0];
            //double threshold = 5.0; 

            int j2;
            vf.pix_data = new byte[ry, rx, 3];
            //vf.recreate_pix_data = new byte[ry, rx, 3];
            for (int j = 0; j < ry; j++)
            {
                j2 = ry - j - 1;
                for (int i = 0; i < rx; i++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        vf.pix_data[j, i, k] = CV.imgdataBGR[j2, i, k];
                    }
                    //double min = 442.0;
                    //int minId = 0;
                    //for (int k = 0; k < DiffColorMap.Count; k++)
                    //{
                    //    byte[] px_d = { CV.imgdataBGR[j2, i, 0], CV.imgdataBGR[j2, i, 1], CV.imgdataBGR[j2, i, 2] };
                    //    double dist = ColorDist(px_d, DiffColorMap[k]);
                    //    if (dist < threshold)
                    //    {
                    //        minId = k;
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        if (dist < min)
                    //        {
                    //            min = dist;
                    //            minId = k;
                    //        }
                    //    }
                    //}
                    ////vf.recreate_pix_data[j, i, 2] = initialCMap[minId].R;
                    ////vf.recreate_pix_data[j, i, 1] = initialCMap[minId].G;
                    ////vf.recreate_pix_data[j, i, 0] = initialCMap[minId].B;

                    //vf.recreate_pix_data[j, i, 2] = DiffColorMap[minId].R;
                    //vf.recreate_pix_data[j, i, 1] = DiffColorMap[minId].G;
                    //vf.recreate_pix_data[j, i, 0] = DiffColorMap[minId].B;
                }
            }
            vf.avgr = CV.avgr;
            vf.avgg = CV.avgg;
            vf.avgb = CV.avgb;
            vf.maskAvgRGBColor = CV.maskAvgRGBColor;
            vf.HistoR = CV.HistoR;
            vf.HistoG = CV.HistoG;
            vf.HistoB = CV.HistoB;
            vf.motionCentroid = CV.motionCentroid;
            vf.motionMaskSum = CV.motionMaskSum;
            vf.motionMaskDir = CV.motionMaskDir;
            vf.motionDir = CV.motionDir;
            vf.mDirSmth = CV.motionDir;

            Vframe_repository.Add(vf);
            if (Vframe_repository.Count > 2)
            {
                Vframe_repository[Vframe_repository.Count - 2].mDirSmth = (Vframe_repository[Vframe_repository.Count - 3].motionDir + Vframe_repository[Vframe_repository.Count - 3].motionDir) * 0.5;
            }
        }

        public void OnFrameUpdate()  //executed 20d times per second.
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

            if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
            frameNumber++;
            if (Video.NeedUpdate)
            {
                Video.UpdateFrame(true); //recalculate the video frame if the camera got a new one
                rx = Video.ResX;
                ry = Video.ResY;

                if (Vframe_repository.Count <= maxframes) 
                {
                    //////////////////////////////////////////////////////////////////////////////color palette code
                    initialCMap = ColorQuantizer.MedianCutQuantGeneral(Video, 16);
                    diffColorMap = ColorQuantizer.SortByDifference(initialCMap);   //sort intialCMap again by the different; distinct color
                    ////////////////////////////////////////////////////////////////////////end of color palette cod

                    BuildVfRepo();
                    if (Vframe_repository.Count > 1)
                    {
                        if (Vframe_repository[Vframe_repository.Count - 1].pix_data == Vframe_repository[Vframe_repository.Count - 2].pix_data)
                            Console.WriteLine("vf repeated");
                    }
                }
                else
                {
                    Console.WriteLine("THIS IS THE END OF MOVIE "+ Vframe_repository.Count );
                }
            }

            if (Vframe_repository.Count >= minframes && !playbackmode)
            {
                CalibrationVideo.Stop();
				playbackmode = true;
                CreateScreens();
                //Screens[2].ison = true;
                for (int i = 0; i < screenCount; i++)
                {
                    Screens[i].ison = true;
                }
                Console.WriteLine("playing back, VFR.C = " + Vframe_repository.Count);
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
                //else { DrawFullScreen (CalibrationVideo); }
                CalculateGaze();

                //visualize video property data
                bool visMotion = false;
                bool visColor = true;
                bool visHisto = false;
                bool recordGaze = false;
                bool visGaze = true;
                if (visMotion) { VisualizeMotion(); }
                if (visColor) { VisualizeColor(); }
                if (visHisto) { VisualizeHisto(); }
                if (recordGaze) { checkVideo = true;  RecordGaze(); }
                if (visGaze && !recordGaze) { ReadAndVisualizeGaze(); }
            }
        }
        
        void VisualizeMotion()
        {
            VideoPixel[,] px = Video.Pixels;

            for (int j = 0; j < ry; ++j)
            {
                for (int i = 0; i < rx; ++i)
                {
                    if (px[j, i].mx > 0 )
                        GL.Color4(1.0, 1.0, 0.0, 0.5);
                    else
                        GL.Color4(0.0, 1.0, 1.0, 0.5);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex2(i, j);
                    GL.Vertex2(i + px[j, i].mx * 10.0, j + px[j, i].my * 10.0);
                    GL.End();
                }
            }
        }

        static double ColorDist(VideoPixel px, RGBA_Quad quad)
        {
            return Math.Sqrt((quad.R / 255.0 - px.R) * (quad.R / 255.0 - px.R)
                             + (quad.G / 255.0 - px.G) * (quad.G / 255.0 - px.G)
                             + (quad.B / 255.0 - px.B) * (quad.B / 255.0 - px.B));
        }
        static double ColorDist(byte[] px, RGBA_Quad quad)
        {
            return Math.Sqrt((quad.R - px[2]) * (quad.R - px[2])
                             + (quad.G - px[1]) * (quad.G - px[1])
                             + (quad.B - px[0]) * (quad.B - px[0]));
        }

        VBitmap recreateImg;
        void VisualizeColor_notused()
        {
            if ( recreateImg == null) { recreateImg = new VBitmap(rx, ry); }
            //byte[, ,] recreate_pix_data = new byte[ry, rx, 3];
            //recreateImg.FromVideo(Video);

            ColorQuant ColorQuantizer = new ColorQuant();
            Colormap initialCMap = ColorQuantizer.MedianCutQuantGeneral(Video, 16);
            Colormap diffColorMap = ColorQuantizer.SortByDifference(initialCMap);

            double threshold = 0.1;
            for (int j = 0; j < ry; j++)
            {
                for (int i = 0; i < rx; i++)
                {
                    double min = 2.0;
                    int minId = -1;
                    for (int k = 0; k < diffColorMap.Count; k++)
                    {
                        double dist = ColorDist(Video.Pixels[j, i], diffColorMap[k]);
                        if (dist < threshold)
                        {
                            minId = k;
                            break;
                        }
                        else
                        {
                            if (dist < min)
                            {
                                min = dist;
                                minId = k;
                            }
                        }
                    }
                    if (minId < 3)
                    {
                        recreateImg.Pixels[j, i].R = diffColorMap[minId].R / 255.0;
                        recreateImg.Pixels[j, i].G = diffColorMap[minId].G / 255.0;
                        recreateImg.Pixels[j, i].B = diffColorMap[minId].B / 255.0;
                    }
                    else
                    {
                        recreateImg.Pixels[j, i].R = 0.0;
                        recreateImg.Pixels[j, i].G = 0.0;
                        recreateImg.Pixels[j, i].B = 0.0;
                    }
                }
                recreateImg.Draw(0.0, 0.0, rx, ry, 1.0);
            }
        }

        void VisualizeColor()
        {
            if (recreateImg == null) { recreateImg = new VBitmap(rx, ry); }
            VideoPixel[,] px = Video.Pixels;

            double threshold = 5.0;

            int j2;
            byte[,,] recreate_pix_data = new byte[ry, rx, 3];
            for (int j = 0; j < ry; j++)
            {
                j2 = ry - j - 1;
                for (int i = 0; i < rx; i++)
                {
                    double min = 442.0;
                    int minId = -1;
                    for (int k = 0; k < diffColorMap.Count; k++)
                    {
                        byte[] px_d = { CV.imgdataBGR[j2, i, 0], CV.imgdataBGR[j2, i, 1], CV.imgdataBGR[j2, i, 2] };
                        double dist = ColorDist(px_d, diffColorMap[k]);
                        if (dist < threshold)
                        {
                            minId = k;
                            break;
                        }
                        else
                        {
                            if (dist < min)
                            {
                                min = dist;
                                minId = k;
                            }
                        }
                    }

                    if (minId < 3)
                    {
                        recreate_pix_data[j, i, 2] = diffColorMap[minId].R;
                        recreate_pix_data[j, i, 1] = diffColorMap[minId].G;
                        recreate_pix_data[j, i, 0] = diffColorMap[minId].B;
                    }
                    else
                    {
                        recreate_pix_data[j, i, 2] = 0;
                        recreate_pix_data[j, i, 1] = 0;
                        recreate_pix_data[j, i, 0] = 0;
                    }
                }
            }
            recreateImg.FromFrame(recreate_pix_data);
            recreateImg.Update();
            recreateImg.Draw(0, 0, rx, ry, 1.0);

            for (int i = 0; i < diffColorMap.Count; i++)
            {
                //visualize the palette
                RGBA_Quad quad = diffColorMap[i];
                GL.PointSize(40);
                GL.Color4(quad.R / 255.0, quad.G / 255.0, quad.B / 255.0, 1.0);
                GL.Begin(PrimitiveType.Points);
                GL.Vertex2(10 + i * 5, 30);
                GL.End();
            }
            GL.End();
        }

        void VisualizeHisto()
        {
            GL.Color4(1.0, 0.0, 0.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < CV.HistoRA.Length; ++i)
            {
                GL.Vertex2(i * 30.0, (double)CV.HistoRA[i] / 500.0);
            }
            GL.End();

            GL.Color4(0.0, 1.0, 0.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < CV.HistoGA.Length; ++i)
            {
                GL.Vertex2(i * 30.0, (double)CV.HistoGA[i] / 500.0);
            }
            GL.End();

            GL.Color4(0.0, 0.0, 1.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < CV.HistoBA.Length; ++i)
            {
                GL.Vertex2(i * 30.0, (double)CV.HistoBA[i] / 500.0);
            }
            GL.End();
        }

        StreamWriter sw = new StreamWriter("gaze.csv");
        void RecordGaze()
        {
            if (Vframe_repository.Count > maxframes) { return; }
            sw.WriteLine(Vframe_repository.Count + "," + dpoint.X + "," + dpoint.Y);
        }

        StreamReader sr = new StreamReader("gazeread.csv");
        List<Vector2d> readL= new List<Vector2d>();
        void ReadAndVisualizeGaze()
        {
            if (readL.Count == 0)
            {
                while (!sr.EndOfStream)
                {
                    string[] values = sr.ReadLine().Split(',');
                    readL.Add(new Vector2d(Double.Parse(values[1]), Double.Parse(values[2])));
                }
            }
            VisualizeGaze(readL[Vframe_repository.Count - 1]);
        }
    }
}

