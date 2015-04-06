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
    public class GLhelper
    {        
        public static int Unproject(ref Vector3d screenp, ref Vector3d p, ref Matrix4d InverseModelProj, double _w, double _h)
        {

            //Transformation of normalized coordinates between -1 and 1
            double nx = (2.0 * screenp.X / _w) - 1.0;
            double ny = (2.0 * screenp.Y / _h) - 1.0;
            double nz = 2.0 * screenp.Z - 1.0;
            double nw = 1.0;

            Vector4d np = new Vector4d(nx, ny, nz, nw);
            Vector4d ns = Vector4d.Transform(np, InverseModelProj);

            double ww = ns.W;
            p.X = ns.X;
            p.Y = ns.Y;
            p.Z = ns.Z;

            if (ww == 0.0)
                return 0;

            ww = 1.0 / ww;
            p.X *= ww;
            p.Y *= ww;
            p.Z *= ww;

            return 1;
        }

        static public void PickMatrix(double _mx, double _my, double _w, double _h, int[] viewport)
        {
            Matrix4d mm = Matrix4d.Identity;
            
            double sx, sy;
            double tx, ty;

            sx = (double)viewport[2] / _w;
            sy = (double)viewport[3] / _h;
            tx = ((double)viewport[2] + 2.0 * ((double)viewport[0] - _mx)) / _w;
            ty = ((double)viewport[3] + 2.0 * ((double)viewport[1] - _my)) / _h;

           

            mm.M11 = sx;
            mm.M41 = tx;
            mm.M22 = sy;
            mm.M42= ty;



          /*  GL.Translate(
                (viewport[2] - 2 * (_mx - viewport[0])) / _w,
                (viewport[3] - 2 * (_my - viewport[1])) / _h, 0);

            GL.Scale(viewport[2] / _w, viewport[3] / _h, 1.0);*/

            GL.MultMatrix(ref mm);
        }
    }
}
