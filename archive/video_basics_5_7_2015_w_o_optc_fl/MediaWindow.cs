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

//using Tobii.Gaze.Core;

namespace testmediasmall
{
    public class VFrame
    {
        public byte[, , ] frame_pix_data = null;
        //qualifiers
        public double avgr = 0.0;
        public double avgg = 0.0;
        public double avgb = 0.0;
        public double totalMovement = 0.0;
        public double domiHue = 0.0; //dominant Hue
        public RGBHisto HistoR;
        public RGBHisto HistoG;
        public RGBHisto HistoB;
    }

    public class MediaWindow
    {
        public int Width = 0;       //width of the viewport in pixels
        public int Height = 0;      //height of the viewport in pixels
        public double MouseX = 0.0; //location of the mouse along X
        public double MouseY = 0.0; //location of the mouse along Y

        //EyeHelper EyeTracker = new EyeHelperTOBII();/////////////////////////////////////////////////////////EYE TRACKER
        //List<Vector3d> gazeL = new List<Vector3d>();/////////////////////////////////////////////////////////EYE GAZE LIST
        
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
            /*
            try
            {
                //     socket = IO.Socket("http://127.0.0.1:6789");
                EyeTracker.Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
             * */
            ///////////////////////////////////////////////////////////////////////////////////END OF EYE TRACKER INIT
            //intialize Video capturing from primary camera [0] at a low resolution
            VideoIN.EnumCaptureDevices();
            
            //...using webcam inputs
            //Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            //...use video
            Video.StartVideoFile(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\quantitative_aesthetics\video_basics_5_7_2015_w_o_optc_fl\_testVideo.avi");
            Video.SetResolution(120, 80);

            sw = new StreamWriter(@"frame_111.csv");
        }
        
        public void Close()
        {
            sw.Close();
            //EyeTracker.ShutDown();/////////////////////////////////////////////////////////////////////// EYE TRACKER SHUT DOWN
        }

        public bool playbackmode=false;
        public int maxframes = 200;
        public int cframe=0;    //current frame

        public int rx = 0;
        public int ry = 0;

        //public bool optFlow = false;

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

        //animation function. This contains code executed 20 times per second.
        public void OnFrameUpdate()
        {
            //////////////////////////////////////////////////////////////////////////////////MATCH GAZE WITH MASK///
            //create mask:
            /* 
            |  --     3    --  |
            |   1  |  0  |  2  |
            |  --     4    --  |
            ** mask 5 is the whole screen
            */
            /*
            Vector3d lgaze;
            Vector3d rgaze;
            Vector3d lnorm;
            Vector3d rnorm;

            lgaze = Vector3d.TransformPosition(EyeTracker.EyeLeftSmooth.GazePosition, EyeTracker.EyeToScreen);
            rgaze = Vector3d.TransformPosition(EyeTracker.EyeRightSmooth.GazePosition, EyeTracker.EyeToScreen);
            
            lnorm = new Vector3d(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X,
                                  EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y, 0.0);
            rnorm = new Vector3d(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X,
                                  EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y, 0.0);

            Vector3d dpoint;
            Vector3d dpointNorm;
            dpointNorm = (lnorm + rnorm) * 0.5;
            //.WriteLine(dpointNorm.X);
            dpoint = (lgaze + rgaze) * 0.5;

            gazeL.Add(dpoint);

            int num;
            if (dpointNorm.Y > 0.75) { num = 4; }
            else if (dpointNorm.Y < 0.25) { num = 3; }
            else if (dpointNorm.X < 0.25) { num = 1; }
            else if (dpointNorm.X > 0.75) { num = 2; }
            else { num = 0; }
            Console.WriteLine(num);

            sw.WriteLine( num + "," + dpointNorm.X + "," + dpointNorm.Y+",");
            /////////////////////////////////////////////////////////////////////////////////END OF MATCH GAZE WITH MASK///
            */


            if (Vframe_repository.Count >= maxframes && !playbackmode)
            {
                playbackmode = true;
                Vframe_repository.Sort(sorter);
            }

            if (playbackmode)
            {
                GL.ClearColor(1.0f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                cframe++;
                if (cframe >= Vframe_repository.Count) cframe = 0;

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////interaction
                //match eye position with THIS frame's quality and switch to other frames accrording to that

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

                //if dpoint has stayed in a certain location for an amount of time
                //then draw pixels with a zoom from that location 

                GL.RasterPos2(0.0, 0.0);
                GL.PixelZoom((float)Width / rx * (frameNumber-190) * 0.1f, (float)Height / ry * (frameNumber-190) * 0.1f);
                GL.DrawPixels(rx, ry, PixelFormat.Bgr, PixelType.UnsignedByte, Vframe_repository[cframe].frame_pix_data);
                //no alpha here! 


                ///////////////////////////////////////////////////////////////////////////////////////////////////////////end of interaction
            }
            else
            {
                if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
                //recalculate the video frame if the camera got a new one
                if (Video.NeedUpdate) Video.UpdateFrame(true);

                VideoPixel[,] px = Video.Pixels;
                rx = Video.ResX;
                ry = Video.ResY;


                //............................................................index each frames

                
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

                for (int j = 0; j < ry; ++j)
                {
                    for (int i = 0; i < rx; ++i)
                    {
                        //GL.PointSize((float)(1.0 + Video.Pixels[j, i].V * 20.0));
                        GL.PointSize((float)(rx));
                        GL.Color4(px[j, i].R, px[j, i].G, px[j, i].B, 1.0);
                        GL.Begin(PrimitiveType.Points);
                        GL.Vertex2(i, j);
                        GL.End();
                        //draw movement 
                        GL.LineWidth(1.0f);
                        //GL.Begin(PrimitiveType.LineStrip);

                        GL.End();
                        //GL.DrawPixels(2*rx, 2*ry, PixelFormat.Rgba, PixelType.Byte, );

                        //stream writer
                        /*sw.Write(frameNumber
                            + "," + j
                            + "," + i
                            + "," + px[j, i].R
                            + "," + px[j, i].G
                            + "," + px[j, i].B
                            + "," + px[j, i].V
                            + ","
                            //==================================also get alpha!!
                            );
                         * */

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
                //Console.Write(frameNumber + ", ");
                frameNumber++;

                RGBColor.FrameUpdate(px, rx, ry);

                VFrame vf = new VFrame();
                //vf.frame_pix_data = (byte[, ,])RGBColor.imgdataBGR.Clone();
                int j2;
                vf.frame_pix_data = new byte[ry,rx ,3];
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
                vf.HistoR = RGBColor.HistoR;
                vf.HistoG = RGBColor.HistoG;
                vf.HistoB = RGBColor.HistoB;

                double cBlue = CvInvoke.cvCompareHist(vf.HistoR.Histogram, vf.HistoB.Histogram,Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
                //Console.WriteLine(cBlue);
                //////////////////////////////////////////////////////////////////////////////color palette code
                ColorQuant ColorQuantizer = new ColorQuant();
                Colormap initialCMap = ColorQuantizer.MedianCutQuantGeneral(vf, rx, ry, 3);
                Colormap DiffColorMap = ColorQuantizer.SortByDifference(initialCMap);
                Colormap HueColorMap = ColorQuantizer.SortByHue(initialCMap);
                var a = ColorQuantizer.TranslateHSV(initialCMap[0]);
                vf.domiHue = a[0];
                //Console.WriteLine(vf.domiHue);
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
                //        GL.Vertex2(i, j);
                //        GL.End();

                //        //angle 
                //        double optFlowAngle = Math.Atan2(px[j, i].mx, px[j, i].my);
                //       // optFlowAngle +=  

                //        totalMovement += diff;
                //    }
                //}
                //vf.totalMovement = totalMovement;
                /////////////////////////////////////////////////////////////end of optical flow
                

                Vframe_repository.Add(vf);
                //Console.WriteLine(RGBColor.avgb);

                //checking past 20 frames
                GL.PixelZoom(0.5f, 0.5f);
                for (int k = 0; k < 20 && k < Vframe_repository.Count; ++k)
                {
                    VFrame f = Vframe_repository[Vframe_repository.Count - 1 - k];
                    GL.RasterPos2(k * 0.5, 10.0);
                    GL.DrawPixels(rx, ry, PixelFormat.Bgr, PixelType.UnsignedByte, f.frame_pix_data);
                }

                
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

