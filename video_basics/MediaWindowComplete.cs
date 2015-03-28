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

namespace testmediasmall
{
    public class MediaWindowComplete
    {
        public int Width = 0;
        public int Height = 0;
        public double MouseX = 0.0;
        public double MouseY = 0.0;

        //C_View is a class defined in the C_geometry.cs file and is just a helper object for managing viewpoint / tragetpoint camera mechanics
        public GLutils.C_View Viewer = new GLutils.C_View();
        // An image object to hold the latest camera frame
        VBitmap Videoimage = new VBitmap(120, 120);

        VideoIN Video = new VideoIN();
        SoundSampleFreq sound;

        public void Initialize()
        {
            VideoIN.EnumCaptureDevices();
            Video.StartCamera(VideoIN.CaptureDevices[0], 160, 120);
            Video.SetResolution(80, 60);

            sound = SoundOUT.TheSoundOUT.AddEmptyFreqSample(0.3, 0.3);
            sound.Play(false);
        }

        public void OnFrameUpdate()
        {
            if (!Video.IsVideoCapturing) return;

            if (Video.NeedUpdate) Video.UpdateFrame(true);
            //Videoimage.FromVideo(Video);
            //Videoimage.Draw(0.0, 0.0, 15, 10, 0.5);
            //set the background colour to white
            GL.ClearColor(0.6f, 0.6f, 0.6f, 1.0f);
            //clear both the colour and depth buffers
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.DepthTest);
            //Start modification of the projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            //reset the rojection matrix to the identity matrix 
            GL.LoadIdentity();
            //Set the projeciton matrix to an orthographic projection that matches the dimensions of the video
            GL.Ortho(0.0, Video.ResX, 0.0, Video.ResY, -10.0, 100.0);
            //Start modification of the model matrix
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            for (int j = 0; j < Video.ResY; ++j)
            {
                for (int i = 0; i < Video.ResX; ++i)
                {
                    GL.PointSize((float)((Video.Pixels[j, i].V) * 20.0));
                    GL.Color4(1.0, 1.0, 1.0, 1.0);
                    GL.Begin(BeginMode.Points);
                    GL.Vertex2(i, j);
                    GL.End();
                }
            }
            for (int j = 0; j < Video.ResY; ++j)
            {
                for (int i = 0; i < Video.ResX; ++i)
                {
                    double diff = Math.Abs(Video.Pixels[j, i].V - Video.Pixels[j, i].V0);
                    GL.PointSize((float)(diff * 50.0));
                    GL.Color4(1.0, 0.0, 0.0, 0.5);
                    GL.Begin(BeginMode.Points);
                    GL.Vertex2(i, j);
                    GL.End();
                }
            }

            GL.Color4(0.0, 0.0, 0.0, 0.5);
            GL.LineWidth(1.0f);
            GL.Begin(BeginMode.Lines);
            for (int j = 0; j < Video.ResY; ++j)
            {
                for (int i = 0; i < Video.ResX; ++i)
                {
                    GL.Vertex2(i, j);
                    GL.Vertex2(i + Video.Pixels[j, i].mx * 50.0, j + Video.Pixels[j, i].my * 50.0);
                }
            }
            GL.End();


            double mx = 0.0;
            double my = 0.0;
            double mtotal = 0.0;
            double avgDX = 0.0;
            double avgDY = 0.0;

            for (int j = 0; j < Video.ResY; ++j)
            {
                for (int i = 0; i < Video.ResX; ++i)
                {
                    double mmag = Math.Sqrt(Video.Pixels[j, i].mx * Video.Pixels[j, i].mx + Video.Pixels[j, i].my * Video.Pixels[j, i].my);
                    mx += i * mmag;
                    my += j * mmag;

                    mtotal += mmag;

                    avgDX += Video.Pixels[j, i].mx * mmag;
                    avgDY += Video.Pixels[j, i].my * mmag;
                }
            }

            mx /= mtotal;
            my /= mtotal;

            avgDX /= mtotal;
            avgDY /= mtotal;

            GL.PointSize((float)(5.0 + mtotal * 0.5));
            GL.Color4(0.0, 0.0, 0.0, 0.5);

            GL.Begin(BeginMode.Points);
            GL.Vertex2(mx, my);
            GL.End();


            GL.Color4(0.0, 0.0, 0.0, 1.0);
            GL.LineWidth(5.0f);
            GL.Begin(BeginMode.Lines);
            GL.Vertex2(mx, my);
            GL.Vertex2(mx + avgDX * 60.0, my + avgDY * 60.0);
            GL.End();

            MotionXSmooth = MotionXSmooth * 0.9 + mx * 0.1;
            MotionYSmooth = MotionYSmooth * 0.9 + my * 0.1;



            GL.PointSize(10.0f);
            GL.Color4(0.0, 0.0, 0.0, 1.0);

            GL.Begin(BeginMode.Points);
            GL.Vertex2(MotionXSmooth, MotionYSmooth);
            GL.End();

            mpoints.AddLast(new Vector2d(MotionXSmooth, MotionYSmooth));
            if (mpoints.Count > 500) mpoints.RemoveFirst();

            GL.Color4(0.0, 0.0, 0.0, 1.0);
            GL.LineWidth(1.0f);
            GL.Begin(BeginMode.LineStrip);
            foreach (Vector2d v in mpoints)
            {
                GL.Vertex2(v);
            }
            GL.End();

            if (!sound.IsPlaying)
            {
                sound.SilenceAllFrequencies();

                double motmag = Math.Sqrt(avgDX * avgDX + avgDY * avgDY);
                avgDX /= motmag;
                avgDY /= motmag;

                double vol = motmag;
                if (vol > 0.3) vol = 0.3;

                sound.SetFreq(200.0 + (1.0 + avgDY) * 800.0, vol, rnd.NextDouble());
                // sound.SetFreq(100.0+(1.0 + avgDX) * 2000.0, vol, rnd.NextDouble());

                sound.BuildSoundSample();

                sound.Pan = 2.0 * ((mx / Video.ResX) - 0.5);
                sound.Play(false);
            }

        }

        Random rnd = new Random();
        double MotionXSmooth = 0.0;
        double MotionYSmooth = 0.0;
        LinkedList<Vector2d> mpoints = new LinkedList<Vector2d>();
    }
}
