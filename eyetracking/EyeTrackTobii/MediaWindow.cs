using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Tobii.Gaze.Core;

using System.IO;

//using Quobject.SocketIoClientDotNet.Client;
﻿//using Newtonsoft.Json.Linq;

namespace eyetrack
{
    public class MediaWindow
    {
        //...........................eye
       // Socket socket;
        EyeHelper EyeTracker = new EyeHelperTOBII();
        //...........................

        public MediaWindow()
        {

        }
        public int Width = 0;
        public int Height = 0;
        public double MouseX = 0.0;
        public double MouseY = 0.0;
        public GLutils.C_View Viewer = new GLutils.C_View();

        StreamWriter sw;

        public void Initialize(GLControl glcontrol)
        {
            //sw = new StreamWriter(@"C:\Users\anakano\Documents\Classes\GSD6432\Final_Project\eyetracking_data\gaze.csv");
            sw = new StreamWriter(@"eyetracking_data\gaze.csv");
            try
            {
           //     socket = IO.Socket("http://127.0.0.1:6789");
                EyeTracker.Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
 
        public void Close()
        {
            sw.Close();
            EyeTracker.ShutDown();
        }

        Color4 bg = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
        
        public void OnFrameUpdate()
        {
            Vector3d leye; 
            Vector3d reye;
            Vector3d lgaze;
            Vector3d rgaze;

            leye = Vector3d.TransformPosition(EyeTracker.EyeLeftSmooth.EyePosition, EyeTracker.EyeToScreen);
            reye = Vector3d.TransformPosition(EyeTracker.EyeRightSmooth.EyePosition, EyeTracker.EyeToScreen);

            lgaze = Vector3d.TransformPosition(EyeTracker.EyeLeftSmooth.GazePosition, EyeTracker.EyeToScreen);
            rgaze = Vector3d.TransformPosition(EyeTracker.EyeRightSmooth.GazePosition, EyeTracker.EyeToScreen);
          
            /*  dynamic json = new JObject();
            json.GazeL = new JArray(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.X, EyeTracker.EyeLeftSmooth.GazePositionScreenNorm.Y);
            json.GazeR = new JArray(EyeTracker.EyeRightSmooth.GazePositionScreenNorm.X, EyeTracker.EyeRightSmooth.GazePositionScreenNorm.Y);

            socket.Emit("eyeData", json);*/

            Vector3d e = (leye + reye) * 0.5;
            Vector3d g = (lgaze + rgaze) * 0.5;

            Vector3d deg = g - e;
            double ds = deg.Length;

            //  Console.WriteLine(ds);
            double fs = ds - 600.0;
            if (fs < 0.0) fs = 0.0;

            double sep = (rgaze - lgaze).Length * 0.001;

            CLine rline = new CLine(reye.X, reye.Y, reye.Z, rgaze.X, rgaze.Y, rgaze.Z);
            CLine lline = new CLine(leye.X, leye.Y, leye.Z, lgaze.X, lgaze.Y, lgaze.Z);

            CLine dline;

            Vector3d dpoint;
            if (CLine.DistanceSegment(rline, lline, out dline))
            {
                dpoint = new Vector3d(dline.Mid.x, dline.Mid.y, dline.Mid.z);
            }
            else
            {
                dpoint = (lgaze + rgaze) * 0.5;
            }

            //socket.Emit("eyeData", )

            GL.ClearColor(bg);     //set the background colour to white
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); //clear both the colour and depth buffers

            GL.UseProgram(0);
            GL.Color4(1.0, 0.0, 0.0, 1.0);
            GL.PointSize(10.0f);



            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.DepthTest);

            GL.MatrixMode(MatrixMode.Projection);//Start modification of the projection matrix
            GL.LoadIdentity();                  //reset the rojection matrix to the identity matrix 
            GL.Ortho(0.0, Width, 0.0, Height, -10.0, 100.0); //Set the projeciton matrix to an orthographic projection that matches the dimensions of the viewport
            GL.MatrixMode(MatrixMode.Modelview); //Start modification of the model matrix
            GL.LoadIdentity();


            GL.MatrixMode(MatrixMode.Projection);//Start modification of the projection matrix
            GL.LoadIdentity();                  //reset the rojection matrix to the identity matrix 
            GL.Ortho(0.0, 1.0, 1.0, 0.0, -10.0, 100.0); //Set the projeciton matrix to an orthographic projection that matches the dimensions of the viewport
            GL.MatrixMode(MatrixMode.Modelview); //Start modification of the model matrix
            GL.LoadIdentity();

            GL.Color4(0.0, 0.0, 0.0, 0.5);
            GL.PointSize(10.0f);
            GL.Begin(PrimitiveType.Points);
            GL.Color4(0.0, 0.0, 0.0, 0.5);
            GL.Vertex3(EyeTracker.EyeLeftSmooth.GazePositionScreenNorm);
            GL.Color4(1.0, 1.0, 1.0, 0.5);
            GL.Vertex3(EyeTracker.EyeRightSmooth.GazePositionScreenNorm);
            GL.End();


            GL.MatrixMode(MatrixMode.Projection);//Start modification of the projection matrix
            GL.LoadIdentity();                  //reset the rojection matrix to the identity matrix 
            GL.Ortho(-EyeTracker.ScreenW * 0.5, EyeTracker.ScreenW * 0.5, -EyeTracker.ScreenH * 0.5, EyeTracker.ScreenH * 0.5, -1000.0, 1000.0); //Set the projeciton matrix to an orthographic projection that matches the dimensions of the viewport
            GL.MatrixMode(MatrixMode.Modelview); //Start modification of the model matrix
            GL.LoadIdentity();


            GL.Color4(1.0, 0.0, 0.0, 0.5);
            GL.PointSize(20.0f);
            GL.Begin( PrimitiveType.Points);
            GL.Vertex3(leye);
            GL.Vertex3(reye);
            GL.End();


            GL.Color4(1.0, 1.0, 1.0, 0.5);
            GL.LineWidth(1.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(leye);
            GL.Vertex3(lgaze);

            GL.Vertex3(reye);
            GL.Vertex3(rgaze);
            GL.End();
        
            //heat map for gaze
            GL.Color4(0.0,1.0, 1.0, 0.4);
            GL.LineWidth(1.0f);
            GL.Begin(PrimitiveType.TriangleFan);

            int t = 0;
            for (t = 0; t < 100; t++) {
                GL.Vertex3(g.X, g.Y, 0.0);
                GL.Vertex3(g.X+.5, g.Y+.5, 0.0);
                GL.Vertex3(g.X-1, g.Y-1, 0.0);
                GL.Vertex3(g.X +2, g.Y +2, 0.0);

            }
            GL.End();
                /*GL.Color4(1.0, 1.0, 1.0, 0.4);
                GL.LineWidth(1.0f);
                GL.Begin(PrimitiveType.TriangleFan);
            
                int res = 80;   //number of steps to complete the circles
                double tim = 1.0;
                double r = tim * 1.0; //radius
                double fg = 200.0; 
                double dt = fg * (2.0 * Math.PI) / (double) res;
                double tb = 0.1;
                double phasespeed = 0.1;
                tb += phasespeed * fg;

                for (int i = 0; i <= res; ++i)
                {
                    tim += 0.02;
                    double rad = r; 
                     tb += phasespeed; 
                    GL.Vertex3(g.X + rad * Math.Cos(i * dt + tb), g.Y + rad * Math.Sin(i * dt + tb), 0.0);
                    //GL.Vertex3(p.X + Math.Cos(i * dt + tb), p.Y + Math.Sin(i * dt + tb), 0.0);
                }
           
                GL.End();
                */
                //stream writer
                sw.WriteLine(DateTime.Now + "," + g.X + "," + g.Y + "," + g.Z);
        
          
        }
    }
}
