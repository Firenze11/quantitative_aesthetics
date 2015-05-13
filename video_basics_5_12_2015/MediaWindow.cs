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

        //***************************************************************add for blackout points
        double alpha = 0.0;
        //******************
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
            EyeTracker.ShutDown();/////////////////////////////////////////////////////////////////////// EYE TRACKER SHUT DOWN
        }

        public int rx = 0;
        public int ry = 0;

        bool playbackmode = false;
        int maxframes = 40;
        int cframe = 0;    //current frame
        double cframeSlowPlayback = 0;  //reduce the frame rate for playback
        int pframe;  //frame number of previous clip during transition
        double pframeSlowPlayback;

        int analyzedFrame;
        int newFrame;
        //double minDomiHue = 10.0;
        double domiHueDiff;

        public VBitmap vbit;

        bool iszooming = false;
        int zoomduration = 100;
        int zoomcount = 0;
        double[] focus = new double[2];

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

        //selection logic for the new scenes
        public int skippedFrameRange = 10;

        public int domiHueTransition(int analyzedFrame, bool complementary)
        {
            int nf = 0;
            double minDomiHue = 10;
            
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
                        Console.WriteLine("nf: " + nf);
                    }
                }
            }
            return nf;
        }

        //animation function. This contains code executed 20 times per second.
        public void OnFrameUpdate()
        {
            newFrame = cframe;
            if (Video.IsVideoCapturing && vbit == null)
            {
                vbit = new VBitmap(Video.ResX, Video.ResY);
            }
            /* mask
            |  --     3    --  |
            |   1  |  0  |  2  |
            |  --     4    --  |
            */
            Vector3d lnorm;
            Vector3d rnorm;

            lnorm = new Vector3d(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X,
                                  EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y, 0.0);
            rnorm = new Vector3d(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X,
                                  EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y, 0.0);

            Vector3d dpointNorm;
            dpointNorm = (lnorm + rnorm) * 0.5;
            Vector3d gazeMedium = new Vector3d(0.5, 0.5, 0.0);

            dpointNorm.Y = 1.0 - dpointNorm.Y;
            //deviation = 100;
            
            //get mask number of the gaze
            int num;    
            if (dpointNorm.Y > 0.75) { num = 4; }
            else if (dpointNorm.Y < 0.25) { num = 3; }
            else if (dpointNorm.X < 0.25) { num = 1; }
            else if (dpointNorm.X > 0.75) { num = 2; }
            else { num = 0; }

            sw.WriteLine(num + "," + dpointNorm.X + "," + dpointNorm.Y + ",");
            Console.WriteLine(num); //end getting mask number of gaze
            
            gazeL.Add(dpointNorm);
            if (gazeL.Count > 300) { gazeL.RemoveAt(0); }
            //if (gazeL.Count == 1) { gazeMedium = dpointNorm; }
            //else { gazeMedium = 0.5 * gazeMedium + 0.5 * dpointNorm; }
            int lastf = 8; //number of frames to calculate the gazeMedium

            if (gazeL.Count >= lastf)
            {
                gazeMedium = new Vector3d(0.0, 0.0, 0.0);
                deviation = 0;
                for (int i = 0; i < lastf; i++) { gazeMedium += gazeL[gazeL.Count - i - 1]; }
                gazeMedium *= (1.0 / lastf);    //take the average of the last #(lastf) frames
                for (int i = 0; i < lastf; i++) { deviation += 1000 * (gazeL[gazeL.Count - i - 1] - gazeMedium).LengthSquared; } //"standard dev"

                if (iszooming) //post fixation period lasts for zoomduration frames
                {
                    zoomcount++;
                    if (zoomcount >= zoomduration)
                    {
                        //here write the code that is executed during the transition period [zoom, cut etc....]
                        iszooming = false;
                    }
                }
                else if (deviation < 0.25 && deviation > 0.000000001) //> 0.000000001 to avoid zooming when there's no gaze data (deviation = 0)
                {//deviation just dropped below threshold
                    iszooming = true;
                    zoomcount = 0;

                    focus[0] = gazeMedium.X * rx;
                    focus[1] = gazeMedium.Y * ry;

                    //////////////////////////////////////////Frame reassignments
                    pframe = cframe;
                    //analyzedFrame = cframe; //this is the frame that will be analyzed for identifying next chunk of film
                    pframeSlowPlayback = cframe;

                    //choose which scene to show
                    //newFrame = domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue   
                    byte[] gazeRGB = {10,10,10};
                    newFrame = maskAvgRGBTransition(cframe, num, gazeRGB);

                    //iszooming = false;
                    cframe = newFrame;
                    cframeSlowPlayback = newFrame;

                    //-----WHY FOR SECOND TIME?

                    //domiHueTransition(minDomiHue, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue   
                    
                    //ZOOM IN ALL THE WAY TO A PIXEL
                }
                else
                {//normal viewing period 
                    //write here the code that is executed during normal viewing
                }
            }
            /////////////////////////////////////////////////////////////////////////////////END OF MATCH GAZE WITH MASK///


            if (Vframe_repository.Count >= maxframes && !playbackmode)
            {
                playbackmode = true;
                //Vframe_repository.Sort(sorter);
            }

            if (playbackmode)
            {
                cframeSlowPlayback += 0.2;
                cframe = (int) Math.Floor(cframeSlowPlayback);

                //zoom transition, slower frame rate
                pframeSlowPlayback += 0.2;
                pframe = (int)Math.Floor(pframeSlowPlayback);

                if (cframe >= Vframe_repository.Count)
                {
                    cframe = 0;
                    cframeSlowPlayback = 0;
                }

                if (pframe >= Vframe_repository.Count)
                {
                    pframe = 0;
                    pframeSlowPlayback = 0;
                }

                //zoomMode false for testing blackout******************************************************************
                bool blackout = true;
                double zoomrate = 0.05;

                GL.ClearColor(1.0f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////interaction
                double x0, y0, w, h, a;

                x0 = 0.0;
                y0 = 0.0;
                w = rx;
                h = ry;

                byte[, ,] px = Vframe_repository[cframe].frame_pix_data;
                vbit.FromFrame(px);
                vbit.Draw(x0, y0, w, h, 1.0);

                if (iszooming)
                {
                    double s = 1.0 + zoomrate * cframe;
                    //x0 = Math.Min(rx * 0.5 - focus[0] * s, 0.0); // max and min are used to constrain the frame in view port (no pink!)
                    //y0 = Math.Min(ry * 0.5 - focus[1] * s, 0.0);
                    x0 = Math.Min(focus[0] * (1.0-s), 0.0); // max and min are used to constrain the frame in view port (no pink!)
                    y0 = Math.Min(focus[1] * (1.0-s), 0.0);
                    w = Math.Max(rx * s, rx - x0);
                    h = Math.Max(ry * s, ry - y0);
                    //a = 1.0 - ((double) zoomcount) / ((double)zoomduration);

                    byte[, ,] pre_px = Vframe_repository[pframe].frame_pix_data;
                    vbit.FromFrame(pre_px);
                    vbit.Draw(x0, y0, w, h, 1.0);
                }
                /*/////////////////////////////////////////////////////////////////original code for drawing vframe
                GL.RasterPos2(0.0, 0.0); //bottom left
                GL.PixelZoom((float)Width / rx * (frameNumber - 190) * 0.1f, (float)Height / ry * (frameNumber - 190) * 0.1f);
                GL.DrawPixels(rx, ry, PixelFormat.Bgr, PixelType.UnsignedByte, Vframe_repository[cframe].frame_pix_data);
                //no alpha here! 
                 * */

                if (blackout)
                {
                    //black out blocks
                    //can be triggered with when the eye gaze is on a particular point
                    Random rnd = new Random();
                    //draw points near the gaze point
                    float block = (float)rx * 0.1f;
                    double blockRadius = 5.0;
                    int sampleRadius = 50;
                    Vector3d blockPoint = new Vector3d(rnd.Next(0, sampleRadius), rnd.Next(0, sampleRadius), 0.0);

                    //Vector3d dpointNorm = new Vector3d(rnd.Next(0, rx), rnd.Next(0, ry), 0.0);
                    //Vector3d dpointNorm = new Vector3d(rnd.Next(0, 5), rnd.Next(0, 5), 0.0);
                    //gazeL.Add(dpointNorm);
                    //if (gazeL.Count > 300) gazeL.RemoveAt(0);

                    //GL.Color4(1.0, 0.0, 0.0, 0.4);
                    //GL.Begin(PrimitiveType.Quads);

                    //GL.Vertex2(0, 10);
                    //GL.Vertex2(20, 20);
                    //GL.Vertex2(50, 10);
                    //GL.Vertex2(0, 40);
                    //GL.End();

                    GL.PointSize(30.0f);///////////////////////////////////////////////////////////////////////////VISUALIZE GAZE
                    GL.Color4(0.0, 1.0, 1.0, 1);
                    GL.Begin(PrimitiveType.Points);
                    GL.Vertex2(focus[0], focus[1] );
                    GL.End();
                    //Console.WriteLine(focus[0] + ", "+focus[1]);

                    GL.PointSize(block);
                    GL.Begin(PrimitiveType.Points);
                    for (int i = 0; i < gazeL.Count; i += 30)
                    {
                        //visualize the gaze

                        GL.Color4(0.0, 0.0, 0.0, 1);
                        GL.Vertex2(gazeL[i].X * rx, gazeL[i].Y * ry);

                        // visualize the blackout points
                        // GL.PointSize(block);
                        // GL.Color4(1.0, 0.0, 0.0, alpha);
                        // GL.Begin(PrimitiveType.Points);
                        // GL.Vertex2(gazeL[i].X + (blockPoint.X * blockRadius) / sampleRadius, gazeL[i].Y + (blockPoint.Y * blockRadius) / sampleRadius);
                        //  GL.End();
                    }
                    GL.End();

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
            }
            else
            {
                if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
                //recalculate the video frame if the camera got a new one
                if (Video.NeedUpdate) Video.UpdateFrame(true);

                VideoPixel[,] px = Video.Pixels;
                rx = Video.ResX;
                ry = Video.ResY;
                vbit.FromVideo(Video);
                
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

                vbit.Draw(0.0, 0.0, rx, ry, 1.0);
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
                       
                        //create histogram from bins (alternative to openCV)
                        /*
                        double R = Video.Pixels[j, i].R;
                    
                        //rgb histogram
                        int range =256;
                        int binNum = 10;
                        int binWidth = range / binNum;

                        if (R < binWidth) RGB_bin[0] += 1;
                        else
                        { 
                            for (int k = 1; k < binNum - 2; k++)
                            {
                                RGB_bin[k] += 1;
                                binWidth++;
                            }
                        }*/
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
                //        GL.Vertex2(i * Width / rx, j * Height / ry);
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
                    for (int i = 0; i < nx; ++i)
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

