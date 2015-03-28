using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using C_sawapan_media;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace testmediasmall
{
    public class VPixel {
        public VPixel(VBitmap vbitmap) {
            bm=vbitmap;
            NPixels[0] = this;
            NPixels[1] = this;
            NPixels[2] = this;
            NPixels[3] = this;
        }

        public Vector3d Gradient
        {
            get
            {
                return new Vector3d(Right.Grey-Left.Grey, Up.Grey-Down.Grey, 0.0);
            }
        }

        public Vector3d Contour
        {
            get
            {
                return new Vector3d(Up.Grey - Down.Grey, -Right.Grey + Left.Grey, 0.0);
            }
        }

        public VPixel Left
        {
            get
            {
                return NPixels[0];
            }
        }

        public VPixel Right
        {
            get
            {
                return NPixels[1];
            }
        }

        public VPixel Up
        {
            get
            {
                return NPixels[3];
            }
        }

        public VPixel Down
        {
            get
            {
                return NPixels[2];
            }
        }

        public double Grey
        {
            get
            {
                return (r + g + b) / 3.0;
            }
            set
            {
                r = value;
                g = value;
                b = value;
                bm.NeedUpdate = true;
            }
        }

        public double R{
            get{
                return r;
            }
            set{
                r=value;
                bm.NeedUpdate = true;
            }
        }

        public double G
        {
            get
            {
                return g;
            }
            set
            {
                g = value;
                bm.NeedUpdate = true;
            }
        }


        public double B
        {
            get
            {
                return b;
            }
            set
            {
                b = value;
                bm.NeedUpdate = true;
            }
        }


        public double A
        {
            get
            {
                return a;
            }
            set
            {
                a = value;
                bm.NeedUpdate = true;
            }
        }


        public VPixel [] NPixels=new VPixel[4];

        VBitmap bm;
        double r=0.0;
        double g=0.0;
        double b=0.0;
        double a=1.0;
    }

    public class VBitmap
    {
        public VBitmap(int rx, int ry)
        {
            SetDimensions(rx, ry);
        }

        public VBitmap(VBitmap b)
        {
            CopyFrom(b);
        }

        public void LoadBitmap(string _fname)
        {
            if (_fname == null) return;
            Bitmap bmp = Bitmap.FromFile(_fname) as Bitmap;
            if (bmp == null) return;
            Bitmap abmp = new Bitmap(bmp, ResX, ResY);

            double id=1.0/255.0;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Color cc = abmp.GetPixel(i, ResY-j-1);
                    Pixels[j, i].R = cc.R * id;
                    Pixels[j, i].G = cc.G * id;
                    Pixels[j, i].B = cc.B * id;
                    Pixels[j, i].A = cc.A * id;
                }
            }
        }

        public void Normalize()
        {
            double minR = double.MaxValue;
            double maxR = double.MinValue;
            double minG = double.MaxValue;
            double maxG = double.MinValue;
            double minB = double.MaxValue;
            double maxB = double.MinValue;

            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    if (minR > Pixels[j, i].R) minR = Pixels[j, i].R;
                    if (minG > Pixels[j, i].G) minG = Pixels[j, i].G;
                    if (minB > Pixels[j, i].B) minB = Pixels[j, i].B;

                    if (maxR < Pixels[j, i].R) maxR = Pixels[j, i].R;
                    if (maxG < Pixels[j, i].G) maxG = Pixels[j, i].G;
                    if (maxB < Pixels[j, i].B) maxB = Pixels[j, i].B;
             
                }
            }

            double dR = maxR - minR;
            double dG = maxG - minG;
            double dB = maxB - minB;

            if (dR == 0.0) dR = -1.0;
            else dR = 1.0 / dR;

            if (dG == 0.0) dG = -1.0;
            else dG = 1.0 / dG;

            if (dB == 0.0) dB = -1.0;
            else dB = 1.0 / dB;

            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    if (dR > 0.0) Pixels[j, i].R = (Pixels[j, i].R - minR) * dR;
                    if (dG > 0.0) Pixels[j, i].G = (Pixels[j, i].G - minG) * dG;
                    if (dB > 0.0) Pixels[j, i].B = (Pixels[j, i].B - minB) * dB;

                }
            }

            NeedUpdate = true;
        }

        public void FromVideo(VideoIN vi) {
            if (vi.Pixels == null) return;
            SetDimensions(vi.ResX, vi.ResY);
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R = vi.Pixels[j, i].R;
                    Pixels[j, i].G = vi.Pixels[j, i].G;
                    Pixels[j, i].B = vi.Pixels[j, i].B;
                    Pixels[j, i].A = 1.0;
                }
            }
        }

        public void CopyFrom(VBitmap b)
        {
            SetDimensions(b.ResX, b.ResY);
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R = b.Pixels[j, i].R;
                    Pixels[j, i].G = b.Pixels[j, i].G;
                    Pixels[j, i].B = b.Pixels[j, i].B;
                    Pixels[j, i].A = b.Pixels[j, i].A;
                }
            }
        }

        public void SetDimensions(int Rx, int Ry)
        {
            if (ResX == Rx && ResY == Ry) return;
            Pixels = new VPixel[Ry, Rx];
            for (int j = 0; j < Ry; ++j)
            {
                for (int i = 0; i < Rx; ++i)
                {
                    Pixels[j, i] = new VPixel(this);
                }
            }

            for (int j = 0; j < Ry; ++j)
            {
                for (int i = 0; i < Rx; ++i)
                {
                    if (i > 0) Pixels[j, i].NPixels[0] = Pixels[j, i - 1];
                    if (i < Rx-1) Pixels[j, i].NPixels[1] = Pixels[j, i + 1];
                    if (j > 0) Pixels[j, i].NPixels[2] = Pixels[j-1, i];
                    if (j > Ry-1) Pixels[j, i].NPixels[3] = Pixels[j+1, i];
                }
            }

            NeedUpdate=true;

            texdata = new byte[Rx * Ry * 4];
            if (texid != 0) GL.DeleteTexture(texid);
            texid = 0;
        }

        byte[] texdata;

        public int ResX
        {
            get
            {
                if (Pixels == null) return 0;
                return Pixels.GetLength(1);
            }
        }

        public int ResY
        {
            get
            {
                if (Pixels == null) return 0;
                return Pixels.GetLength(0);
            }
        }

        

        public Vector3d Location=Vector3d.Zero;
        public double ViewWidth=120.0;
        public double ViewHeight=60.0;
        public VPixel[,] Pixels;

        int texid = 0;

        public bool NeedUpdate=false;

        byte D2B(double c)
        {
            if (c <= 0.0) return 0;
            if (c >= 1.0) return 255;
            return (byte)(c * 255.0);
        }

        public void MultiplyRGB(double d)
        {
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R*= d;
                    Pixels[j, i].G*= d;
                    Pixels[j, i].B*= d;
                }
            }
        }

        public void AddRGB(double r, double g, double b)
        {
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R += r;
                    Pixels[j, i].G += g;
                    Pixels[j, i].B += b;
                }
            }
        }

        public void MultiplyRGB(VBitmap b2)
        {
            if (b2.ResX != ResX || b2.ResY != ResY) return;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R *= b2.Pixels[j, i].R;
                    Pixels[j, i].G *= b2.Pixels[j, i].G;
                    Pixels[j, i].B *= b2.Pixels[j, i].B;
                }
            }
        }

        public void AddRGB(VBitmap b2)
        {
            if (b2.ResX != ResX || b2.ResY != ResY) return;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R += b2.Pixels[j, i].R;
                    Pixels[j, i].G += b2.Pixels[j, i].G;
                    Pixels[j, i].B += b2.Pixels[j, i].B;
                }
            }
        }

        public void AddMultipliedRGB(VBitmap b2, double a)
        {
            if (b2.ResX != ResX || b2.ResY != ResY) return;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R += b2.Pixels[j, i].R*a;
                    Pixels[j, i].G += b2.Pixels[j, i].G*a;
                    Pixels[j, i].B += b2.Pixels[j, i].B*a;
                }
            }
        }

        public double DotProductRGB(VBitmap b2)
        {
            if (b2.ResX != ResX || b2.ResY != ResY) return 0.0;
            double dt = 0.0;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    dt += Pixels[j, i].R * b2.Pixels[j, i].R;
                    dt += Pixels[j, i].G * b2.Pixels[j, i].G;
                    dt += Pixels[j, i].B * b2.Pixels[j, i].B;
                }
            }

            return dt;
        }

        public void EdgeDetect()
        {
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].A = Math.Abs(-Pixels[j, i].Grey * 4.0 + (Pixels[j, i].Left.Grey + Pixels[j, i].Right.Grey + Pixels[j, i].Up.Grey + Pixels[j, i].Down.Grey));
                }
            }

            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].Grey = Pixels[j, i].A;
                    Pixels[j, i].A = 1.0;
                }
            }

            NeedUpdate = true;
        }

        public void Grey()
        {
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].Grey = Pixels[j, i].Grey;
                }
            }


            NeedUpdate = true;
        }

        public void Abs()
        {
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    Pixels[j, i].R = Math.Abs(Pixels[j, i].R);
                    Pixels[j, i].G = Math.Abs(Pixels[j, i].G);
                    Pixels[j, i].B = Math.Abs(Pixels[j, i].B);
                    Pixels[j, i].A = Math.Abs(Pixels[j, i].A);
                }
            }


            NeedUpdate = true;
        }

        public double DotProductGrey(VBitmap b2)
        {
            if (b2.ResX != ResX || b2.ResY != ResY) return 0.0;
            double dt = 0.0;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {
                    dt += Pixels[j, i].Grey * b2.Pixels[j, i].Grey;
                }
            }

            return dt;
        }

        public void Update()
        {
            int k = 0;
            for (int j = 0; j < ResY; ++j)
            {
                for (int i = 0; i < ResX; ++i)
                {

                    texdata[k++] = D2B(Pixels[j, i].R);
                    texdata[k++] = D2B(Pixels[j, i].G);
                    texdata[k++] = D2B(Pixels[j, i].B);
                    texdata[k++] = D2B(Pixels[j, i].A);
                }
            }

            if (texid == 0)
            {
                texid = GL.GenTexture();

                GL.BindTexture(TextureTarget.Texture2D, texid);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexImage2D<byte>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, ResX, ResY, 0, PixelFormat.Rgba, PixelType.UnsignedByte, texdata);
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, texid);
                GL.TexSubImage2D<byte>(TextureTarget.Texture2D, 0, 0, 0, ResX, ResY, PixelFormat.Rgba, PixelType.UnsignedByte, texdata);
            }

            NeedUpdate = false;
        }

        public void Draw(double Alpha) {
            if (NeedUpdate) Update();

            GL.Enable(EnableCap.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, texid);
            GL.Color4(1.0, 1.0, 1.0, Alpha);
            DrawTexturedQuad(Location.X, Location.Y, ViewWidth, ViewHeight);

            GL.Disable(EnableCap.Texture2D);
        }

        public void Draw(double _x, double _y, double _w, double _h, double Alpha)
        {
            Location.X = _x;
            Location.Y = _y;
            ViewHeight = _h;
            ViewWidth = _w;

            Draw(Alpha);
        }

        static public void DrawTexturedQuad(double x0, double y0, double w, double h)
        {
            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0, 0.0);
            GL.Vertex2(x0, y0);

            GL.TexCoord2(1.0, 0.0);
            GL.Vertex2(x0 + w, y0);

            GL.TexCoord2(1.0, 1.0);
            GL.Vertex2(x0 + w, y0 + h);

            GL.TexCoord2(0.0, 1.0);
            GL.Vertex2(x0, y0 + h);
            GL.End();
        }
    }
}
