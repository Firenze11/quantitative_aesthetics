using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using C_sawapan_media;

namespace testmediasmall
{
    public partial class MainWindowForm : Form
    {
        public MainWindowForm()
        {
            Resize += new EventHandler(MainWindowForm_Resize);
            InitializeComponent();            
        }

        MediaWindow mediawin = new MediaWindow();
        bool loaded = false;
        Timer timer = new Timer();

        void MainWindowForm_Resize(object sender, EventArgs e)
        {            
          /*  int flayoutsize=150;
            flowLayoutPanel1.Location = new Point(ClientSize.Width - flayoutsize, 0);
            flowLayoutPanel1.Size = new System.Drawing.Size(flayoutsize, ClientSize.Height);

            GLviewport.Location = new Point(0, 0);
            GLviewport.Size = new System.Drawing.Size(ClientSize.Width-flayoutsize, ClientSize.Height);*/
        }

        void UpdateFrame()
        {
            if (!loaded) return;
            mediawin.OnFrameUpdate();
            GLviewport.SwapBuffers();
        }

        private void GLviewport_Load(object sender, EventArgs e)
        {
            MediaIO.Initialize(this.Handle.ToInt32());
            mediawin.Initialize();

            loaded = true;
            timer.Interval = 35;
            timer.Enabled = true;
            timer.Start();
            timer.Tick += new EventHandler(timer_Tick);

            mediawin.Viewer.SetViewportSize(GLviewport.Width, GLviewport.Height);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.PointSmooth);
            GL.Enable(EnableCap.LineSmooth);

            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.Diffuse);
            GL.Enable(EnableCap.ColorMaterial);
            GL.Enable(EnableCap.Normalize);


            GL.LightModel(LightModelParameter.LightModelTwoSide, 1);
            GL.LightModel(LightModelParameter.LightModelLocalViewer, 1);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (!loaded) return;
            UpdateFrame();
        }

        private void GLviewport_Resize(object sender, EventArgs e)
        {
            mediawin.Width = GLviewport.Width;
            mediawin.Height = GLviewport.Height;

            mediawin.Viewer.SetViewportSize(GLviewport.Width, GLviewport.Height);

            if (!loaded) return;
            
            GL.Viewport(0, 0, mediawin.Width, mediawin.Height); // Use all of the glControl painting area

            UpdateFrame();
        }

        private void GLviewport_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            UpdateFrame();
        }

        private double mx0 = 0;
        private double my0 = 0;

        private void GLviewport_MouseMove(object sender, MouseEventArgs e)
        {
            mx0=mediawin.MouseX;
            my0=mediawin.MouseY;

            mediawin.MouseX = e.X;
            mediawin.MouseY = mediawin.Height - e.Y;

            if (e.Button == MouseButtons.Left)
            {
                mediawin.Viewer.AngleXYrad -= (mediawin.MouseX - mx0) * 0.03;
                mediawin.Viewer.AngleZrad -= (mediawin.MouseY - my0) * 0.03;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                mediawin.Viewer.Distance *= (1.0-(mediawin.MouseY - my0) * 0.1);
                if (mediawin.Viewer.Distance < mediawin.Viewer.NearPlane) mediawin.Viewer.Distance = mediawin.Viewer.NearPlane;

               // mediawin.viewer.AngleZrad += (mediawin.MouseY - my0) * 0.1;
            }
            else if (e.Button == MouseButtons.Right)
            {
            
                mediawin.Viewer.Tpoint -= mediawin.Viewer.VX*(mediawin.MouseX - mx0) * 0.02;
                mediawin.Viewer.Tpoint -= mediawin.Viewer.VY * (mediawin.MouseY - my0) * 0.02;
                // mediawin.viewer.AngleZrad += (mediawin.MouseY - my0) * 0.1;
            }
            
            
        }

        private void MainWindowForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void GLviewport_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void GLviewport_MouseUp(object sender, MouseEventArgs e)
        {

        }


        private void button1_loadfile_MouseClick(object sender, MouseEventArgs e)
        {
 

        }

        private void MainWindowForm_Load(object sender, EventArgs e)
        {

        }


    }
}
