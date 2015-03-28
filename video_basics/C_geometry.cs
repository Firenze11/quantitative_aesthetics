/*
* Copyright (c) 2012 Panagiotis Michalatos [www.sawapan.eu]
*
* This software is provided 'as-is', without any express or implied
* warranty. In no event will the authors be held liable for any damages
* arising from the use of this software.
*/

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

namespace GLutils
{
    public class C_imagetexture
    {
        public C_imagetexture()
        {
        }

        public C_imagetexture(string _filename)
        {
            Build(_filename);
        }

        public C_imagetexture(Bitmap _bmp)
        {
            Build(_bmp);
           
        }

        public C_imagetexture(int _rx, int _ry)
        {
            Build(_rx, _ry);
        }

        public void Build(string _filename)
        {
            if (_filename == null) return;
            Bitmap bmp = Bitmap.FromFile(_filename) as Bitmap;
            if (bmp == null) return;
            Build(bmp);
        }

        public void Build(Bitmap _bmp)
        {
            Build(_bmp.Width, _bmp.Height);
            if (bitmap == null) return;
            g.DrawImage(_bmp, 0, 0, _bmp.Width, _bmp.Height);
            needupdate = true;
        }

        public void Build(int _rx, int _ry)
        {
            if (bitmap == null || bitmap.Width!=_rx || bitmap.Height!=_ry)
            {
                bitmap = new Bitmap(_rx, _ry, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                g = Graphics.FromImage(bitmap);
                ResetGraphicsTransform();

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                rc.X = 0;
                rc.Y = 0;
                rc.Width = _rx;
                rc.Height = _ry;
            }

            
            Clear();
            needupdate = true;
        }

        public void ResetGraphicsTransform()
        {
            if (g == null || bitmap == null) return;
            g.ResetTransform();
            g.ScaleTransform(1.0f, -1.0f);
            g.TranslateTransform(0.0f, -(float)bitmap.Height);
        }

        virtual public void Clear()
        {
            if (g == null) return;
            g.Clear(Clearcolor);
        }

        
        public void Bind()
        {
            if (bitmap == null) return;

            if (texid<0) {
                GL.GenTextures(1, out texid);
                GL.BindTexture(TextureTarget.Texture2D, texid);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                needupdate = true;
            }
            else GL.BindTexture(TextureTarget.Texture2D, texid);

            if (needupdate || bitmap.Width != texw || bitmap.Height != texh)
            {
               // bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                if (bitmap.Width != texw || bitmap.Height != texh)
                {
                    System.Drawing.Imaging.BitmapData data = 
                        bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                       );

                   
                    GL.TexImage2D(TextureTarget.Texture2D, 
                        0, 
                        PixelInternalFormat.Rgba, 
                        data.Width, data.Height, 
                        0, 
                        PixelFormat.Bgra, 
                        PixelType.UnsignedByte, 
                        data.Scan0);


                    bitmap.UnlockBits(data);

                    texw = bitmap.Width;
                    texh = bitmap.Height;
                }
                else
                {
                    System.Drawing.Imaging.BitmapData data =
                        bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                       );

                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, data.Width, data.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
         
                    bitmap.UnlockBits(data);
                }
                needupdate = false;
            }                
        }

        public void DrawQuadCentred()
        {
            if (displist < 0)
            {
                displist = GL.GenLists(1);
                GL.NewList(displist, ListMode.Compile);

                GL.Begin(BeginMode.Quads);
                GL.Normal3(0.0, 0.0, 1.0);

                GL.TexCoord2(0.0, 0.0);
                GL.Vertex2(-1.0, -1.0);

                GL.TexCoord2(1.0, 0.0);
                GL.Vertex2(1.0, -1.0);

                GL.TexCoord2(1.0, 1.0);
                GL.Vertex2(1.0, 1.0);

                GL.TexCoord2(0.0, 1.0);
                GL.Vertex2(-1.0, 1.0);

                GL.End();

                GL.EndList();
            }

            GL.CallList(displist);
        }

        public void DrawSpriteCentred()
        {
            Bind();
            DrawQuadCentred();            
        }

        public void DrawQuad()
        {
            if (displist0 < 0)
            {
                displist0 = GL.GenLists(1);
                GL.NewList(displist0, ListMode.Compile);
                GL.Begin(BeginMode.Quads);
                GL.Normal3(0.0, 0.0, 1.0);
                GL.TexCoord2(0.0, 0.0);
                GL.Vertex2(0.0, 0.0);
                GL.TexCoord2(1.0, 0.0);
                GL.Vertex2(1.0, 0.0);
                GL.TexCoord2(1.0, 1.0);
                GL.Vertex2(1.0, 1.0);
                GL.TexCoord2(0.0, 1.0);
                GL.Vertex2(0.0, 1.0);

                GL.End();
                GL.EndList();
            }

            GL.CallList(displist0);
        }

        public void DrawSprite()
        {
            Bind();
            DrawQuad();
        }

        public void glDraw()
        {
            System.Drawing.Imaging.BitmapData data =
                        bitmap.LockBits(
                            new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            bitmap.PixelFormat
                            //System.Drawing.Imaging.PixelFormat.Format32bppArgb
                        );

            GL.DrawPixels(data.Width, data.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, data.Width, data.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);
        }

        public void glDraw(Rectangle _r)
        {
            if (_r.Width == 0 || _r.Height == 0) return;
            System.Drawing.Imaging.BitmapData data =
                        bitmap.LockBits(
                            _r,
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            bitmap.PixelFormat
                        );

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, bitmap.Width);//data.Stride);

            GL.DrawPixels(data.Width, data.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);//data.Stride);
        }

        static int displist = -1;
        static int displist0 = -1;

        public bool needupdate = true;
        public int texw = 0;
        public int texh = 0;
        public Bitmap bitmap;
        public Graphics g;
        public Color Clearcolor = Color.FromArgb(0, 255, 255, 255);

        public RectangleF rc = new RectangleF();

        public int texid = -1;
    }


    public class C_imagetextureText : C_imagetexture
    {
        public C_imagetextureText()
        {
            //sformat.FormatFlags
        }

        public C_imagetextureText(string _filename):base(_filename)
        {
            BitmapRectangle.X = 0;
            BitmapRectangle.Y = 0;
            BitmapRectangle.Width = bitmap.Width;
            BitmapRectangle.Height = bitmap.Height;
        }

        public C_imagetextureText(Bitmap _bmp):base(_bmp)
        {
            BitmapRectangle.X = 0;
            BitmapRectangle.Y = 0;
            BitmapRectangle.Width = bitmap.Width;
            BitmapRectangle.Height = bitmap.Height;
        }

        public C_imagetextureText(int _rx, int _ry):base(_rx, _ry)
        {
            BitmapRectangle.X = 0;
            BitmapRectangle.Y = 0;
            BitmapRectangle.Width = bitmap.Width;
            BitmapRectangle.Height = bitmap.Height;
        }


        public Color TColor
        {
            get
            {
                return tcolor;
            }
            set
            {
                tcolor = value;
                tbrush = new SolidBrush(tcolor);
                Clearcolor = Color.FromArgb(0, tcolor.R, tcolor.G, tcolor.B);
                backbrush = new SolidBrush(Clearcolor);
                
            }
        }

        public Color BGColor
        {
            get
            {
                return Clearcolor;
            }
            set
            {
                Clearcolor = value;
                backbrush = new SolidBrush(Clearcolor);
            }
        }

        Color tcolor = Color.White;


        public StringFormat sformat = new StringFormat();
        public Rectangle DrawnTextRectangle = new Rectangle();
        public RectangleF textrectF = new RectangleF();

        public RectangleF DrawnTextRectangleF = new RectangleF();

        public Rectangle BitmapRectangle = new Rectangle();

        //int backalpha=0; 
        SolidBrush tbrush = new SolidBrush(Color.White);
        SolidBrush backbrush = new SolidBrush(Color.FromArgb(0, 255, 255, 255));
        PointF originpoint = new PointF(0.0f, 0.0f);

        public void DrawTextWrapped(Font fn, string s, double x, double y, int _w)
        {
            int w = _w;
            if (w > bitmap.Width) w = bitmap.Width;

            SizeF sz=g.MeasureString(s, fn, w, sformat);
            DrawnTextRectangle.X = 0;
            DrawnTextRectangle.Y = 0;
            DrawnTextRectangle.Width = (int)sz.Width;
            DrawnTextRectangle.Height = (int)sz.Height;

            DrawnTextRectangleF.X = 0.0f;
            DrawnTextRectangleF.Y = 0.0f;
            DrawnTextRectangleF.Width = sz.Width;
            DrawnTextRectangleF.Height = sz.Height;

            textrectF.X = 0.0f;
            textrectF.Y = 0.0f;
            textrectF.Width = w;
            textrectF.Height = bitmap.Height;


            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.FillRectangle(backbrush, DrawnTextRectangle);

            g.DrawString(s, fn, tbrush, textrectF, sformat);
            needupdate = true;

            GL.RasterPos2(x, y);
            DrawnTextRectangle.Y = bitmap.Height - (int)sz.Height;

            DrawnTextRectangle.Intersect(BitmapRectangle);

            glDraw(DrawnTextRectangle);
            //glDraw();

        }

        public void DrawText(Font fn, string s, double x, double y, double z)
        {
           /* Clear();
            sformat.LineAlignment = StringAlignment.Far;
            g.DrawString(s, fn, tbrush, rc, sformat);
            needupdate = true;

            // GL.Enable(EnableCap.Texture2D);
            // textoverlay.Bind();
            GL.RasterPos3(x, y, z);
            glDraw();*/


            SizeF sz = g.MeasureString(s, fn, originpoint, sformat);
            DrawnTextRectangle.X = 0;
            DrawnTextRectangle.Y = 0;
            DrawnTextRectangle.Width = (int)sz.Width;
            DrawnTextRectangle.Height = (int)sz.Height;

            DrawnTextRectangleF.X = 0.0f;
            DrawnTextRectangleF.Y = 0.0f;
            DrawnTextRectangleF.Width = sz.Width;
            DrawnTextRectangleF.Height = sz.Height;



            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.FillRectangle(backbrush, DrawnTextRectangle);

            g.DrawString(s, fn, tbrush, originpoint, sformat);
            needupdate = true;

            GL.RasterPos3(x, y, z);
            DrawnTextRectangle.Y = bitmap.Height - (int)sz.Height;

            DrawnTextRectangle.Intersect(BitmapRectangle);
          //  if (DrawnTextRectangle.Right >= bitmap.Width) DrawnTextRectangle.Width += DrawnTextRectangle.Right-bitmap.Width - 1;
            glDraw(DrawnTextRectangle);

        }

        float multiliney = 0.0f;

        public void BeginMultilineText()
        {
            Clear();
            multiliney = 0.0f;
            DrawnTextRectangle = new Rectangle(0, 0, 0, 0);
            DrawnTextRectangleF = new RectangleF(0.0f, 0.0f, 0.0f, 0.0f);
            needupdate = true;
        }

        public bool WriteLine(Font fn, string s) {
            if (multiliney >= bitmap.Height) return false;

            sformat.FormatFlags = StringFormatFlags.NoWrap;

            SizeF sz=g.MeasureString(s, fn, new PointF(0.0f, 0.0f),sformat);

            g.DrawString(s, fn, tbrush, new PointF(0.0f, multiliney), sformat);
            multiliney += sz.Height;// *1.05f;

            if (sz.Width > DrawnTextRectangleF.Width) DrawnTextRectangleF.Width = sz.Width;
            if (multiliney < bitmap.Height) DrawnTextRectangleF.Height = multiliney;
            else DrawnTextRectangleF.Height = bitmap.Height;


            needupdate = true;
            sformat.FormatFlags = 0;

            if (multiliney > bitmap.Height) return false;


            return true;
        }

        public bool WriteLineWrapped(Font fn, string s, int _w)
        {
            if (multiliney >= bitmap.Height) return false;

            int w = _w;
            if (w > bitmap.Width) w = bitmap.Width;

            sformat.FormatFlags = 0;

            SizeF sz = g.MeasureString(s, fn, w, sformat);

            RectangleF rf = new RectangleF(0.0f, multiliney, (float)w, sz.Height);

            g.DrawString(s, fn, tbrush, rf, sformat);

            multiliney += sz.Height;// *1.05f;

            if (w > DrawnTextRectangleF.Width) DrawnTextRectangleF.Width = w;
            if (multiliney < bitmap.Height) DrawnTextRectangleF.Height = multiliney;
            else DrawnTextRectangleF.Height = bitmap.Height;

            needupdate = true;

            if (multiliney > bitmap.Height) return false;

            return true;
        }

        public void DrawMultilineText(double _x, double _y) {
            GL.RasterPos2(_x, _y);
            
            DrawnTextRectangle.X = 0;
            DrawnTextRectangle.Y = bitmap.Height - (int)DrawnTextRectangleF.Height;
            DrawnTextRectangle.Width = (int)DrawnTextRectangleF.Width;
            DrawnTextRectangle.Height = (int)DrawnTextRectangleF.Height;

            DrawnTextRectangle.Intersect(BitmapRectangle);
            glDraw(DrawnTextRectangle);
        }

        public void DrawMultilineTextFromBottom(double _x0, double _y0, double _x1)
        {
            GL.Enable(EnableCap.Texture2D);
            //GL.Color4(1.0, 1.0, 1.0, 1.0);
            Bind();

            DrawnTextRectangle.X = 0;
            DrawnTextRectangle.Y = bitmap.Height - (int)DrawnTextRectangleF.Height;
            DrawnTextRectangle.Width = (int)DrawnTextRectangleF.Width;
            DrawnTextRectangle.Height = (int)DrawnTextRectangleF.Height;

            double _y1 = _y0 + (_x1 - _x0) * (DrawnTextRectangleF.Height / DrawnTextRectangleF.Width);

            double ty0 = (bitmap.Height - DrawnTextRectangleF.Height) / (double)bitmap.Height;
            double tx1 = DrawnTextRectangleF.Width / (double)bitmap.Width;

            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0, ty0);
            GL.Vertex2(_x0, _y0);

            GL.TexCoord2(tx1, ty0);
            GL.Vertex2(_x1, _y0);

            GL.TexCoord2(tx1, 1.0);
            GL.Vertex2(_x1, _y1);

            GL.TexCoord2(0.0, 1.0);
            GL.Vertex2(_x0, _y1);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }


        public void DrawMultilineTextFromTop(double _x0, double _y1, double _x1)
        {
            GL.Enable(EnableCap.Texture2D);
            //GL.Color4(1.0, 1.0, 1.0, 1.0);
            Bind();

            DrawnTextRectangle.X = 0;
            DrawnTextRectangle.Y = bitmap.Height - (int)DrawnTextRectangleF.Height;
            DrawnTextRectangle.Width = (int)DrawnTextRectangleF.Width;
            DrawnTextRectangle.Height = (int)DrawnTextRectangleF.Height;

            double _y0 = _y1 - (_x1 - _x0) * (DrawnTextRectangleF.Height / DrawnTextRectangleF.Width);

            double ty0 = (bitmap.Height - DrawnTextRectangleF.Height) / (double)bitmap.Height;
            double tx1 = DrawnTextRectangleF.Width / (double)bitmap.Width;

            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0, ty0);
            GL.Vertex2(_x0, _y0);

            GL.TexCoord2(tx1, ty0);
            GL.Vertex2(_x1, _y0);

            GL.TexCoord2(tx1, 1.0);
            GL.Vertex2(_x1, _y1);

            GL.TexCoord2(0.0, 1.0);
            GL.Vertex2(_x0, _y1);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }

       /* public enum TEXTPLANE { XY, YX, XZ, ZX, YZ, ZY };
        public void DrawMultilineText(TEXTPLANE _plane, double _x, double _y, double _z, double _dpi, b)
        {
            GL.Enable(EnableCap.Texture2D);
            //GL.Color4(1.0, 1.0, 1.0, 1.0);
            Bind();

            double L = DrawnTextRectangleF.Width / _dpi;

           // DrawnTextRectangle.X = 0;
           // DrawnTextRectangle.Y = bitmap.Height - (int)DrawnTextRectangleF.Height;
           // DrawnTextRectangle.Width = (int)DrawnTextRectangleF.Width;
           // DrawnTextRectangle.Height = (int)DrawnTextRectangleF.Height;

            double _y0 = _y1 - (_x1 - _x0) * (DrawnTextRectangleF.Height / DrawnTextRectangleF.Width);

            double ty0 = (bitmap.Height - DrawnTextRectangleF.Height) / (double)bitmap.Height;
            double tx1 = DrawnTextRectangleF.Width / (double)bitmap.Width;

            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0, ty0);
            GL.Vertex2(_x0, _y0);

            GL.TexCoord2(tx1, ty0);
            GL.Vertex2(_x1, _y0);

            GL.TexCoord2(tx1, 1.0);
            GL.Vertex2(_x1, _y1);

            GL.TexCoord2(0.0, 1.0);
            GL.Vertex2(_x0, _y1);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }*/

        public void DrawMultilineText(Vector3d _p0, Vector3d _p1, Vector3d _p2, double _dpi)
        {
            GL.Enable(EnableCap.Texture2D);
            Bind();


            Vector3d dv1 = _p1 - _p0;
            Vector3d dv2 = _p2 - _p0;
            Vector3d dv3 =Vector3d.Cross(dv1, dv2);
            dv2 = Vector3d.Cross(dv3, dv1);

            dv1.Normalize();
            dv1 *= DrawnTextRectangleF.Width / _dpi;
            dv2.Normalize();
            dv2 *= DrawnTextRectangleF.Height / _dpi;

            
            double ty0 = (bitmap.Height - DrawnTextRectangleF.Height) / (double)bitmap.Height;
            double tx1 = DrawnTextRectangleF.Width / (double)bitmap.Width;

            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0, 1.0);
            GL.Vertex3(_p0);

            GL.TexCoord2(tx1, 1.0);
            GL.Vertex3(_p0+dv1);

            GL.TexCoord2(tx1, ty0);
            GL.Vertex3(_p0 + dv1+dv2);

            GL.TexCoord2(0.0, ty0);
            GL.Vertex3(_p0 + dv2);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }
      /*  public void DrawText(Font fn, string s, double x, double y, double z)
        {
            Clear();
            sformat.LineAlignment = StringAlignment.Far;
            g.DrawString(s, fn, tbrush, rc, sformat);
            needupdate = true;

            // GL.Enable(EnableCap.Texture2D);
            // textoverlay.Bind();
            GL.RasterPos3(x, y, z);
            glDraw();

        }*/

        
    }

    public class C_View
    {
       
        public void Apply(double mousex, double mousey)
        {
            double ip = m_interpolator;
            double iip = 1.0 - m_interpolator;

            m_targetpoint = m_targetpoint * ip + m_targetpointtarget*iip;
            m_upvector = m_upvector * ip + m_upvectortarget * iip;
            m_upvector.Normalize();
            m_fov = m_fov * ip + m_fovtarget * iip;
            m_viewangleXYr = m_viewangleXYr * ip + m_viewangleXYrtarget * iip;
            m_viewangleZr = m_viewangleZr * ip + m_viewangleZrtarget * iip;
            m_viewdistance = m_viewdistance * ip + m_viewdistancetarget * iip;


            UpdateFromPolar();
            BuildProjectionMatrix();
            UpdateMatrices();

           

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projmatrix);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref viewmatrix);


            mouse2d = new Vector3d(mousex, mousey, 0.0);
            UnProject(ref mouse2d, ref mousenear);

            mouse2d.Z = 1.0;
            UnProject(ref mouse2d, ref mousefar);

            mouseray = new C_Line(mousenear, mousefar);
        }


        public void UnProject(ref Vector3d screenpoint, ref Vector3d worldpoint)
        {
            GLhelper.Unproject(ref screenpoint, ref worldpoint, ref matMPi, Width, Height);
        }

        public double AngleXYrad
        {
            get
            {
                return m_viewangleXYrtarget;
            }
            set
            {
                m_viewangleXYrtarget = value;
            }
        }

        public double AngleZrad
        {
            get
            {
                return m_viewangleZrtarget;
            }
            set
            {
                m_viewangleZrtarget = value;
            }
        }

        public double Distance
        {
            get
            {
                return m_viewdistancetarget;
            }
            set
            {
                m_viewdistancetarget = value;
            }
        }

        public double Fov
        {
            get
            {
                return m_fovtarget;
            }
            set
            {
                m_fovtarget = value;       
            }
        }

        public void SetViewportSize(int _w, int _h)
        {
            m_width = _w;
            m_height = _h;
        }

        public Vector3d Vpoint
        {
            get
            {
                return m_viewpoint;
            }
        }

        public Vector3d Tpoint
        {
            get
            {
                return m_targetpointtarget;
            }
            set
            {
                m_targetpointtarget=value;
            }
        }

        public double NearPlane
        {
            get
            {
                return m_nearplane;
            }
            set
            {
                m_nearplane = value;
            }
        }

        public double FarPlane
        {
            get
            {
                return m_farplane;
            }
            set
            {
                m_farplane = value;
            }
        }

        public Vector3d Upvector
        {
            get
            {
                return m_upvectortarget;
            }
            set
            {
                m_upvectortarget = value;
                m_upvectortarget.Normalize();
            }
        }

        void UpdateFromPolar()
        {
            VZ.X = - Math.Cos(m_viewangleXYr) * Math.Cos(m_viewangleZr);
            VZ.Y = - Math.Sin(m_viewangleXYr) * Math.Cos(m_viewangleZr);
            VZ.Z = - Math.Sin(m_viewangleZr);

            m_viewdirection = VZ * m_viewdistance;
            m_viewpoint = m_targetpoint - m_viewdirection;
     
            VX=Vector3d.Cross(VZ, m_upvector);
            VX.Normalize();

            VY = Vector3d.Cross(VX, VZ);
            VY.Normalize();


            BuildModelMatrix();
        }

        void BuildModelMatrix()
        {
           //Matrix4d modmat=  Matrix4d.LookAt(m_viewpoint, m_targetpoint, m_upvector);
            viewmatrix = Matrix4d.Identity;

            viewmatrix.M11 = VX.X;
            viewmatrix.M21 = VX.Y;
            viewmatrix.M31 = VX.Z;

            viewmatrix.M12 = VY.X;
            viewmatrix.M22 = VY.Y;
            viewmatrix.M32 = VY.Z;

            viewmatrix.M13 = -VZ.X;
            viewmatrix.M23 = -VZ.Y;
            viewmatrix.M33 = -VZ.Z;


            viewmatrix.M41 = -Vector3d.Dot(m_viewpoint, VX);
            viewmatrix.M42 = -Vector3d.Dot(m_viewpoint, VY);
            viewmatrix.M43 = Vector3d.Dot(m_viewpoint, VZ);


            viewmatrixf = Matrix4.Identity;

            viewmatrixf.M11 = (float)viewmatrix.M11;
            viewmatrixf.M21 = (float)viewmatrix.M21;
            viewmatrixf.M31 = (float)viewmatrix.M31;
            viewmatrixf.M12 = (float)viewmatrix.M12;
            viewmatrixf.M22 = (float)viewmatrix.M22;
            viewmatrixf.M32 = (float)viewmatrix.M32;
            viewmatrixf.M13 = (float)viewmatrix.M13;
            viewmatrixf.M23 = (float)viewmatrix.M23;
            viewmatrixf.M33 = (float)viewmatrix.M33;
            viewmatrixf.M41 = (float)viewmatrix.M41;
            viewmatrixf.M42 = (float)viewmatrix.M42;
            viewmatrixf.M43 = (float)viewmatrix.M43;

        }

        void BuildProjectionMatrix()
        {
            //Matrix4d prmat = Matrix4d.Perspective(m_fov, m_width / m_height, m_nearplane, m_farplane);
            projmatrix = Matrix4d.Identity;
            double aspect = m_width / m_height;

            
            double f_mul = 1.0 / Math.Tan((0.5 * m_fov * Math.PI) / 180.0);
            projmatrix.M11 = f_mul / aspect;
            projmatrix.M22 = f_mul;
            projmatrix.M33 = (m_farplane + m_nearplane) / (m_nearplane - m_farplane);
            projmatrix.M34 = -1.0;
            projmatrix.M43 = (2.0 * m_farplane * m_nearplane) / (m_nearplane - m_farplane);
            projmatrix.M44 = 0.0;

            projmatrixf = Matrix4.Identity;
            projmatrixf.M11 = (float)projmatrix.M11;
            projmatrixf.M22 = (float)projmatrix.M22;
            projmatrixf.M33 = (float)projmatrix.M33;
            projmatrixf.M34 = (float)projmatrix.M34;
            projmatrixf.M43 = (float)projmatrix.M43;
            projmatrixf.M44 = (float)projmatrix.M44;
        }

        void UpdateMatrices()
        {
            Matrix4d.Mult(ref viewmatrix, ref projmatrix, out matMP);
            matMPi = Matrix4d.Invert(matMP);

            matBillBoard = Matrix4d.Invert(viewmatrix);
            matBillBoard.M41 = 0.0;
            matBillBoard.M42 = 0.0;
            matBillBoard.M43 = 0.0;
        }

        public double Width
        {
            get
            {
                return m_width;
            }
            set
            {
                if (m_width != value)
                {
                    m_width = value;
                }
            }
        }

        public double Height
        {
            get
            {
                return m_height;
            }
            set
            {
                if (m_height != value)
                {
                    m_height = value;
                }
            }
        }

        Vector3d m_targetpointtarget = Vector3d.Zero;
        Vector3d m_upvectortarget = Vector3d.UnitZ;
        double m_fovtarget = 60.0;
        double m_viewangleXYrtarget = 0.0;
        double m_viewangleZrtarget = 0.0;
        double m_viewdistancetarget = 1.0;

        double m_interpolator = 0.9;

        Vector3d m_viewpoint = Vector3d.UnitY;
        Vector3d m_viewdirection = Vector3d.UnitY;
        Vector3d m_targetpoint = Vector3d.Zero;
        Vector3d m_upvector = Vector3d.UnitZ;
        double m_fov = 60.0;
        double m_viewangleXYr = 0.0;
        double m_viewangleZr = 0.0;
        double m_viewdistance = 1.0;

        public Matrix4d matMP = new Matrix4d();
        public Matrix4d matMPi = new Matrix4d();
        public Matrix4d matBillBoard = Matrix4d.Identity;
        double m_width = 0;
        double m_height = 0;

        public Matrix4d projmatrix;
        public Matrix4d viewmatrix;

        public Matrix4 projmatrixf;
        public Matrix4 viewmatrixf;

        public Vector3d mouse2d;
        public Vector3d mousefar;
        public Vector3d mousenear;
        public C_Line mouseray;

        double m_nearplane=1.0;
        double m_farplane=200.0;

        public Vector3d VX;
        public Vector3d VY;
        public Vector3d VZ;

    }

    public class C_Plane
    {
        public C_Plane(double x0, double y0, double z0, double nx, double ny, double nz)
        {
            o = new Vector3d(x0, y0, z0);
            n = new Vector3d(nx, ny, nz);
            n.Normalize();
        }

        public C_Plane(Vector3d _o, Vector3d _n)
        {
            o = _o;
            n = _n;
        }

        public Vector3d Origin
        {
            get
            {
                return o;
            }
            set
            {
                o = value;
            }
        }

        public Vector3d Normal
        {
            get
            {
                return n;
            }
            set
            {
                n = value;
                n.Normalize();
            }
        }

        Vector3d o;
        Vector3d n;
    }

    public class C_Line
    {
        public C_Line(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            v0 = new Vector3d(x0, y0, z0);
            v1 = new Vector3d(x1, y1, z1);
            Update();
        }

        public C_Line(Vector3d _v0, Vector3d _v1)
        {
            v0 = _v0;
            v1 = _v1;
            Update();
        }

        public Vector3d V0
        {
            get
            {
                return v0;
            }
            set
            {
                v0 = value;
                Update();
            }
        }

        public Vector3d V1
        {
            get
            {
                return v1;
            }
            set
            {
                v1 = value;
                Update();
            }
        }

        public Vector3d Dv
        {
            get
            {
                return dvn*length;
            }
            set
            {
                v1 = v0+value;
                length = Dv.Length;
                dvn = Dv;
                dvn.Normalize();
            }
        }

        public Vector3d Dvn
        {
            get
            {
                return dvn;
            }
            set
            {
                dvn = value;
                v1 = v0 + dvn*length;
            }
        }

        public double Length
        {
            get
            {
                return length;
            }
            set
            {
                length = Math.Abs(value);
                v1 = v0 + dvn * length;
            }
        }

        public Vector3d Mid
        {
            get
            {
                return (v0+v1)*0.5;
            }
        }

        void Update()
        {
            dvn = v1 - v0;
            length = dvn.Length;
            if (length!=0.0) dvn*=(1.0/length);
        }

        public double ClosestPoint(Vector3d _p, out Vector3d _cp, out double _t)
        {

            Vector3d dvec, pvec;
            double d;
            dvec = _p - v0;
            d = Vector3d.Dot(dvec, dvn);


            if (d >= 0.0 && d <= Length)
            {
                _cp = dvn * d;
                pvec = dvec - _cp;
                _cp += v0;

                if (length != 0.0) _t = d / Length;
                else _t = 0.0;
                return pvec.Length;
            }

            if (d < 0.0)
            {
                _cp = v0;
                _t = 0.0;
                return dvec.Length;
            }


            dvec = _p - v1;
            _cp = v1;
            _t = 1.0;
            return dvec.Length;

        }

        public bool Intersect(C_Plane _pl, out Vector3d xp)
        {
            xp=_pl.Origin;

            double den = Vector3d.Dot(dvn, _pl.Normal);
            if (den == 0.0) return false;

            den = Vector3d.Dot(_pl.Origin-v0, _pl.Normal)/den;

            xp = v0 + dvn * den;

            return true;
        }

        Vector3d v0;
        Vector3d v1;
        Vector3d dvn;
        double length;
    }

    class C_geometry
    {
 
    }
}
