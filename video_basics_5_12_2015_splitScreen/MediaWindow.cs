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

        double alpha = 0.0;
        EyeHelper EyeTracker = new EyeHelperTOBII();/////////////////////////////////////////////////////////EYE TRACKER
        List<Vector3d> gazeL = new List<Vector3d>();/////////////////////////////////////////////////////////EYE GAZE LIST
        double deviation = 0;

        List<VFrame> Vframe_repository = new List<VFrame>();
        ColorAnalysis RGBColor = new ColorAnalysis();
        public int frameNumber;

        // An image object to hold the latest camera frame
        VBitmap Videoimage = new VBitmap(120, 120);
        //this object represents a single video input device [camera or video file]
        VideoIN Video = new VideoIN();

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
        public int rx = 0;
        public int ry = 0;
        public VBitmap[] vbit = new VBitmap[2];

        //gaze property
        int lr = -1; //focusing on left or right image
        byte[] gazeColor = new byte[3];
        Vector3d dpointNorm = new Vector3d();
        Vector3d gazeMedium = new Vector3d(0.25, 0.25, 0.0);
        Vector3d projG = new Vector3d();//projected gazemedium
        Vector3d focus = new Vector3d();
        Vector3d projF = new Vector3d();//projected focus
        int num;  //get mask number of the gaze

        //global control
        bool playbackmode = false;
        int maxframes = 100;
        int skippedFrameRange = 10;
        bool blackout = true;
        int lastf = 8; //number of frames to calculate the gazeMedium

        //frame control
        int[] cframe = new int[2];    //current frame
        double[] cframeSlowPlayback = new double[2];  //reduce the frame rate for playback
        int[] pframe = new int[2];  //frame number of previous clip during transition
        double[] pframeSlowPlayback = new double[2];
        int[] newFrame = new int[2];

        //on/off control
        bool[] ison = new bool[2];

        //zoom control
        bool[] iszooming = new bool[2];
        int[] zoomcount = new int[2];
        int zoomduration = 60;
        double zoomrate = 0.01;

        //fade control
        bool[] isfading = new bool[2];
        int[] fadecount = new int[2];
        int fadeduration = 40;
        double faderate = 0.01;

        int other(int _lr)
        {
            if (_lr == 0) { return 1; }
            else if (_lr == 1) { return 0; }
            else { return -1; }
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

        int domiHueTransition(int analyzedFrame, bool complementary)
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

        public int maskAvgRGBTransition(int analyzedFrame, int gazeMaskNum, byte[] gazeRGB)
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
                        //Console.WriteLine("nf: " + nf);
                    }
                }
            }
            return nf;
        }

        Vector3d projectedGaze(Vector3d gazeInput, Vector2d xyscale, out int leftright)
        {
            Vector3d  pg = new Vector3d ();
            if (0.0 < gazeInput.X && (0.5 * xyscale.X > gazeInput.X) && (0.25 * xyscale.Y < gazeInput.Y) && (0.75 * xyscale.Y > gazeInput.Y))//left screen
            {
                pg.X = gazeInput.X * 2.0;
                pg.Y = (gazeInput.Y - 0.25 * xyscale.Y) * 2.0;
                pg.Z = 0.0;
                leftright = 0;
            }
            else if ((0.5 * xyscale.X < gazeInput.X) && (xyscale.X > gazeInput.X) && (0.25 * xyscale.Y < gazeInput.Y) && (0.75 * xyscale.Y > gazeInput.Y))//right screen
            {
                pg.X = (gazeInput.X - 0.5 * xyscale.X) * 2.0;
                pg.Y = (gazeInput.Y - 0.25 * xyscale.Y) * 2.0;
                pg.Z = 0.0;
                leftright = 1;
            }
            else { leftright = -1; }
            return pg;
        }

        void drawVbit(int sn)
        {
            if (!ison[sn]) { return; }
            //Console.WriteLine(sn + " is drawing");
            double x0, y0, w, h, a;
            double bottom, left;
            if (sn == 0) { left = 0.0; bottom = 0.25 * ry; }
            else { left = 0.5 * rx; bottom = 0.25 * ry; }

            if (!iszooming[sn] || (iszooming[sn] && isfading[sn]))
            {
                x0 = left;
                y0 = bottom;
                w = rx * 0.5;
                h = ry * 0.5;

                byte[, ,] px = Vframe_repository[cframe[sn]].frame_pix_data;
                vbit[sn].FromFrame(px);
                vbit[sn].Draw(x0, y0, w, h, 1.0);
            }

            if (iszooming[sn])
            {
                double s = 1.0 + zoomrate * zoomcount[sn];
                //x0 = Math.Min(rx * 0.5 - focus[0] * s, 0.0); // max and min are used to constrain the frame in view port (no pink!)
                //y0 = Math.Min(ry * 0.5 - focus[1] * s, 0.0);
                x0 = Math.Min(focus.X*(1.0 - s) + left*s, left); // max and min are used to constrain the frame in view port (no pink!)
                y0 = Math.Min(focus.Y * (1.0 - s) + bottom*s, bottom);
                w = Math.Max(rx * s * 0.5, rx * 0.5 - x0);
                h = Math.Max(ry * s * 0.5, ry * 0.5 - y0);
                if (isfading[sn])
                    a = 1.0 - ((double)fadecount[sn]) / ((double)fadeduration);
                else
                    a = 1.0;

                byte[, ,] pre_px = Vframe_repository[pframe[sn]].frame_pix_data;
                vbit[sn].FromFrame(pre_px);
                vbit[sn].Draw(x0, y0, w, h, a);
            }
        }

        //animation function. This contains code executed 20 times per second.
        public void OnFrameUpdate()
        {
            for (int sn = 0; sn < 2; sn++)
            {
                //newFrame = cframe;
                if (Video.IsVideoCapturing && vbit[sn] == null)// && ison[sn])
                {
                    vbit[sn] = new VBitmap(Video.ResX, Video.ResY);
                }
            }
            if (Vframe_repository.Count >= maxframes && !playbackmode)
            {
                playbackmode = true;
                ison[0] = true;
            }
            if (playbackmode)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////gaze calculation
                /* mask
                |  --     3    --  |
                |   1  |  0  |  2  |
                |  --     4    --  |
                */
                Vector3d lnorm = new Vector3d(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X,
                                              EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y, 0.0);
                Vector3d rnorm = new Vector3d(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X,
                                              EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y, 0.0);
                dpointNorm = (lnorm + rnorm) * 0.5;
                dpointNorm.Y = 1.0 - dpointNorm.Y;

                //dpointNorm = new Vector3d(Control.MousePosition.X, Control.MousePosition.Y, 0.0);////////CHANGE IT!!
                dpointNorm = new Vector3d(MouseX / (double)Width, MouseY / (double)Height, 0.0);////////CHANGE IT!!
                //dpointNorm = new Vector3d(MouseX, MouseY, 0.0);////////CHANGE IT!!
                Console.WriteLine("dpointNorm: " + dpointNorm + ", " + Width + ", " + Height);
                gazeL.Add(dpointNorm);
                if (gazeL.Count > 300) { gazeL.RemoveAt(0); }
                //if (gazeL.Count == 1) { gazeMedium = dpointNorm; }
                //else { gazeMedium = 0.5 * gazeMedium + 0.5 * dpointNorm; }



                if (dpointNorm.Y > 0.75) { num = 4; }
                else if (dpointNorm.Y < 0.25) { num = 3; }
                else if (dpointNorm.X < 0.25) { num = 1; }
                else if (dpointNorm.X > 0.75) { num = 2; }
                else { num = 0; }

                if (gazeL.Count >= lastf)
                {
                    gazeMedium = new Vector3d(0.0, 0.0, 0.0);
                    deviation = 0;
                    for (int i = 0; i < lastf; i++) { gazeMedium += gazeL[gazeL.Count - i - 1]; }
                    gazeMedium *= (1.0 / lastf);
                    for (int i = 0; i < lastf; i++) { deviation += 1000 * (gazeL[gazeL.Count - i - 1] - gazeMedium).LengthSquared; } //"standard dev"

                    projG = projectedGaze(gazeMedium, new Vector2d(1.0, 1.0), out lr); //from the begining, determine which screen is looked at

                    double r, g, b;
                    if (lr != -1)
                    {
                        //r = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].R;
                        //g = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].G;
                        //b = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].B;
                        r = vbit[lr].Pixels[(int)projG.Y, (int)projG.X].R * 255.0;
                        g = vbit[lr].Pixels[(int)projG.Y, (int)projG.X].G * 255.0;
                        b = vbit[lr].Pixels[(int)projG.Y, (int)projG.X].B * 255.0;
                    }
                    else
                    {
                        r = 255; g = 255; b = 255;
                    }
                    gazeColor[0] = (byte)r;
                    gazeColor[1] = (byte)g;
                    gazeColor[2] = (byte)b;

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////procedure control for each screen
                    for (int sn = 0; sn < 2; sn++)
                    {
                        if (ison[sn]) // sn = screen number (0 or 1)
                        {
                            ///////////////////////////////////////////////////////////////////////////////////////////frame update
                            cframeSlowPlayback[sn] += 0.2;
                            cframe[sn] = (int)Math.Floor(cframeSlowPlayback[sn]);
                            //zoom transition, slower frame rate
                            if (isfading[sn])
                            {
                                pframeSlowPlayback[sn] += 0.2;
                                pframe[sn] = (int)Math.Floor(pframeSlowPlayback[sn]);
                            }
                            if (cframe[sn] >= Vframe_repository.Count)
                            {
                                cframe[sn] = 0;
                                cframeSlowPlayback[sn] = 0;
                            }
                            if (pframe[sn] >= Vframe_repository.Count)
                            {
                                pframe[sn] = 0;
                                pframeSlowPlayback[sn] = 0;
                            }

                            ///////////////////////////////////////////////////////////////////////////////////////////zoom/fade update
                            if (iszooming[sn]) //post fixation period lasts for zoomduration frames
                            {
                                zoomcount[sn]++;
                                if (isfading[sn]) { fadecount[sn]++; }
                                if (zoomcount[sn] >= zoomduration)
                                {
                                    //here write the code that is executed during the transition period [zoom, cut etc....]
                                    iszooming[sn] = false;
                                    isfading[sn] = false;
                                    Console.WriteLine("zoom and fade stop: "+sn);
                                }
                                if ((zoomcount[sn] >= zoomduration - fadeduration) && !isfading[sn])
                                {
                                    isfading[sn] = true;
                                    Console.WriteLine("fade start: " + sn);

                                    ison[other(sn)] = true;
                                    Console.WriteLine("is on: " + other(sn));
                                    cframe[other(sn)] = cframe [sn];
                                    cframeSlowPlayback[other(sn)] = cframe [sn];

                                    fadecount[sn] = 0;
                                    cframe[sn] = newFrame[sn];
                                    cframeSlowPlayback[sn] = newFrame[sn];
                                }
                            }


                            else if (deviation < 0.25 && deviation > 0.000000001 && lr == sn) //avoid zooming when there's no gaze data (dev = 0)
                            {//deviation just dropped below threshold
                                focus = gazeMedium; //unlike projG, focus remains stable during zooming process
                                focus.X *= rx;
                                focus.Y *= ry;
                                projF = projG; //projected focus
                                projF.X *= rx;
                                projF.Y *= ry;

                                Console.WriteLine("projF: " + projF.X + ", " + projF.Y);
                                iszooming[sn] = true;
                                Console.WriteLine("zoom start: " + sn);
                                zoomcount[sn] = 0;

                                pframe[sn] = cframe[sn]; ///Frame reassignments
                                pframeSlowPlayback[sn] = cframe[sn];

                                //choose which scene to show (just remember it for now, show it later)
                                newFrame[sn] = maskAvgRGBTransition(cframe[sn], num, gazeColor);
                                //newFrame = domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue  


                                //////////////////////////////////////////////////////////////////////////////
                                //double r, g, b;
                                //if (lr != -1)
                                //{
                                //    //r = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].R;
                                //    //g = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].G;
                                //    //b = vbit[lr].Pixels[(int)(1.0 - dpointNorm.Y) * ry, (int)dpointNorm.X * rx].B;
                                //    r = vbit[lr].Pixels[(int)projF.Y, (int)projF.X].R;
                                //    g = vbit[lr].Pixels[(int)projF.Y, (int)projF.X].G;
                                //    b = vbit[lr].Pixels[(int)projF.Y, (int)projF.X].B;
                                //}
                                //else
                                //{
                                //    r = 255; g = 255; b = 255;
                                //}
                                //gazeColor[0] = (byte)r;
                                //gazeColor[1] = (byte)g;
                                //gazeColor[2] = (byte)b;
                                //////////////////////////////////////////////////////////////////////////////
                            }
                            else//normal viewing period 
                            { //write here the code that is executed during normal viewing
                            }
                        }
                    }
                }


                GL.ClearColor(0.0f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////drawing for each screen
                bool hasZoomingScreen = false;
                for (int sn = 0; sn < 2; sn++)
                {
                    if (iszooming[sn]) //the zooming vbit must be drawn first
                    {
                        hasZoomingScreen = true;
                        drawVbit(sn);
                        drawVbit(other(sn));
                        break;
                    }
                }
                if (!hasZoomingScreen)
                {
                    for (int sn = 0; sn < 2; sn++)
                    {
                        if (ison[sn]) //the zooming vbit must be drawn first
                        {
                            drawVbit(sn);
                        }
                    }
                }
                GL.PointSize(30.0f);///////////////////////////////////////////////////////////////////////////////////////////VISUALIZE GAZE
                GL.Color4(gazeColor[0] / 255.0, gazeColor[1] / 255.0, gazeColor[2] / 255.0, 1.0);
                GL.Begin(PrimitiveType.Points);
                //GL.Vertex2(focus[0], focus[1] );
                GL.Vertex2(gazeMedium.X * rx, gazeMedium.Y * ry);
                GL.End();

                if (blackout)
                {
                    Random rnd = new Random();
                    //draw points near the gaze point
                    float block = (float)rx * 0.1f;
                    double blockRadius = 5.0;
                    int sampleRadius = 50;
                    Vector3d blockPoint = new Vector3d(rnd.Next(0, sampleRadius), rnd.Next(0, sampleRadius), 0.0);

                    //GL.PointSize(block);
                    //GL.Begin(PrimitiveType.Points);
                    //for (int i = 0; i < gazeL.Count; i += 30)
                    //{
                    //    //visualize the gaze

                    //    GL.Color4(0.0, 0.0, 0.0, 1);
                    //    GL.Vertex2(gazeL[i].X * rx, gazeL[i].Y * ry);

                    //    // visualize the blackout points
                    //    // GL.PointSize(block);
                    //    // GL.Color4(1.0, 0.0, 0.0, alpha);
                    //    // GL.Begin(PrimitiveType.Points);
                    //    // GL.Vertex2(gazeL[i].X + (blockPoint.X * blockRadius) / sampleRadius, gazeL[i].Y + (blockPoint.Y * blockRadius) / sampleRadius);
                    //    //  GL.End();
                    //}
                    //GL.End();

                    if (alpha < 1.0)
                    {
                        alpha += 0.002;
                    }
                    else
                    {
                        alpha *= 0.8;
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////////////////////////end of interaction

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////draw black cover
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
                vbit[0].FromVideo(Video);

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

                vbit[0].Draw(0.0, 0.0, rx, ry, 1.0);
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

                //checking past 20 frames
                /*GL.PixelZoom(0.5f, 0.5f);
                for (int k = 0; k < 20 && k < Vframe_repository.Count; ++k)
                {
                    VFrame f = Vframe_repository[Vframe_repository.Count - 1 - k];
                    GL.RasterPos2(k * 1.0, 10.0);//////////////////////////////////////////////////////////////////////
                    GL.DrawPixels(rx, ry, PixelFormat.Bgr, PixelType.UnsignedByte, f.frame_pix_data);
                }
                */

                //for splitting screen
                /*
                int nx = 4;
                int ny = 4;
                int sub_x = rx / nx;
                int sub_y = ry / ny;
                */

                //split screen
                /*for (int j = 0; j < ny; ++j)
                {
                    //for (int i = 0; i < nx; ++i)
                    {
                        GL.Color4(0.0, 1.0, 1.0, 1.0);
                        GL.Begin(PrimitiveType.LineStrip);
                        GL.LineWidth(1.0f);
                        for (int k = 0; k < ry; k++)
                        {  
                            GL.Vertex2(sub_x, k); 
                        }
                        GL.End();
                    
                        //RGBColor.FrameUpdate(px, sub_x, sub_y);
                        sub_x+=sub_x;
                    }
                    GL.Color4(0.0, 1.0, 1.0, 1.0);
                    GL.Begin(PrimitiveType.LineStrip);
                    GL.LineWidth(1.0f);
                    for (int k = 0; k < ry; k++)
                    {
                        GL.Vertex2(k, sub_y);
                    }
                    GL.End();
                    sub_y+=sub_y;
                }*/

                //make channel for A channel 

                //int imgdataBGR_int = RGBColor.imgdataBGR;
                //int imgdataBGRA_int = BitConverter.ToInt32(RGBColor.imgdataBGR);

                /*byte[, ,] frameData = new byte[frameNumber,RGBColor.imgdataBGR, 3];

                if (frameData == null)
                {
                    frameData = new byte[1,3,3];
                }
                 * */




                //.............................................................render video image 
                //update the video image and draw it [just for debugging now]
                // Videoimage.FromVideo(Video);                    
                //Videoimage.Draw(-1.0, -1.0, 2.0, 2.0, 0.2);

                //GL.Color4(1.0, 1.0, 1.0, 1.0);

                /*for (int i = 0; i < Video.ResX; ++i)
                {
                    GL.PointSize((float)(1.0+Video.Pixels[0,i].V*20.0));
                    GL.Begin(BeginMode.Points);
                    GL.Vertex2(i, 0.0);
                    GL.End();
                }*/

            }
        }
    }
}

