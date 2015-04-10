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

namespace testmediasmall
{
    public class MediaWindow
    {
        ColorAnalysis RGBColor = new ColorAnalysis();

        public int Width = 0;       //width of the viewport in pixels
        public int Height = 0;      //height of the viewport in pixels
        public double MouseX = 0.0; //location of the mouse along X
        public double MouseY = 0.0; //location of the mouse along Y

        //C_View is a class defined in the C_geometry.cs file and is just a helper object for managing viewpoint / tragetpoint camera mechanics
        public GLutils.C_View Viewer = new GLutils.C_View();

        // An image object to hold the latest camera frame
        VBitmap Videoimage = new VBitmap(120, 120);
        //this object represents a single video input device [camera or video file]
        VideoIN Video = new VideoIN();

        StreamWriter sw;

        //initialization function. Everything you write here is executed once in the begining of the program
        public void Initialize()
        {
            //intialize Video capturing from primary camera [0] at a low resolution
            VideoIN.EnumCaptureDevices();
            Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            //  Video.StartVideoFile(@"C:\Users\pan\Desktop\out2.avi");
            Video.SetResolution(40, 30);
            sw = new StreamWriter(@"frame_info.csv");
            //histo_writer = new StreamWriter("C:/Users/anakano/Documents/Classes/GSD6432/Final_Project/quantitative_aesthetics/video_basics/histo/test2.csv"); 
        }
        
        public void Close()
        {
            sw.Close();
        }

        //animation function. This contains code executed 20 times per second.
        public void OnFrameUpdate()
        {
            GL.ClearColor(0.6f, 0.6f, 0.6f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (!Video.IsVideoCapturing) return; //make sure that there is a camera connected and running
            //recalculate the video frame if the camera got a new one
            if (Video.NeedUpdate) Video.UpdateFrame(true);

            VideoPixel[,] px = Video.Pixels;
            int rx = Video.ResX;
            int ry = Video.ResY;

            //.............................................................render video image 
            //update the video image and draw it [just for debugging now]
            Videoimage.FromVideo(Video);                    
            //Videoimage.Draw(-1.0, -1.0, 2.0, 2.0, 0.2);

           

            //GL.Color4(1.0, 1.0, 1.0, 1.0);

            /*for (int i = 0; i < Video.ResX; ++i)
            {
                GL.PointSize((float)(1.0+Video.Pixels[0,i].V*20.0));
                GL.Begin(BeginMode.Points);
                GL.Vertex2(i, 0.0);
                GL.End();
            }*/

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, rx, 0.0, ry, -1.0, 1.0);

            List<int> RGB_bin = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                RGB_bin.Add(0);
            }

            for (int j = 0; j < ry; ++j)
            {
                for (int i = 0; i < rx; ++i)
                {
                    //GL.PointSize((float)(1.0 + Video.Pixels[j, i].V * 20.0));
                    GL.PointSize((float)(rx));
                    GL.Color4(Video.Pixels[j, i].R,Video.Pixels[j, i].G, Video.Pixels[j, i].B, 1.0);
                    GL.Begin(PrimitiveType.Points);
                    GL.Vertex2(i, j);
                    GL.End();
                    //draw movement 
                    GL.LineWidth(1.0f);
                    //GL.Begin(PrimitiveType.LineStrip);
                  
                    GL.End();

                    //stream writer
                    sw.WriteLine(DateTime.Now + "," + j + "," + i
                                              + "," + Video.Pixels[j, i].R 
                                              + "," + Video.Pixels[j, i].G 
                                              + "," + Video.Pixels[j, i].B
                                              + "," + Video.Pixels[j, i].V
                                              );

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
            
            int nx = 4;
            int ny = 4;
            int sub_x = rx / nx;
            int sub_y = ry / ny;

            RGBColor.FrameUpdate(px, rx, ry);
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
            
        }
    }
}

