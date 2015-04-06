using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testmediasmall
{
    class HSV
    {
        public HSV()
        {
        }
        public static double[] hsv(double r, double g, double b)
        {
            double cmax;
            double cmin;
            double h;
            double s;
            double v;
            double[] hsv = new double [3];

            if (r > g & r > b) { cmax = r; }
            else if (g > b) { cmax = g; }
            else { cmax = b; }

            if (r < g & r < b) { cmin = r; }      //h
            else if (g < b) { cmin = g; }
            else { cmin = b; }

            double delta = cmax = cmin;

            if (delta == 0) { h= 0; }
            else if (cmax == r)
            {
                h= (((g - b) / delta) % 6) / 6;
            }
            else if (cmax == g)
            {
                h= (((b - r) / delta) + 2) / 6;
            }
            else //if (cmax == b)
            {
                h= (((r - g) / delta) + 4) / 6;
            }
            if (h < 0) { h += 1; }

            if (delta == 0) { s= 0; }        //s
            else { s= (delta / cmax); }

            v = cmax;                        //v

            hsv[0] = h;
            hsv[1] = s;
            hsv[2] = v;
            return hsv;
        }
    }
}
