using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eyetrack
{
    public class CPlane
    {
        public CPlane()
        {
            o = new CVector(0.0, 0.0, 0.0);
            n = new CVector(0.0, 0.0, 1.0);
        }

        public CPlane(double x0, double y0, double z0, double nx, double ny, double nz)
        {
            o = new CVector(x0, y0, z0);
            n = new CVector(nx, ny, nz);
            n.Normalize();
        }

        public CPlane(CVector _o, CVector _n)
        {
            o = _o;
            n = _n;
        }

        public CPlane Clone()
        {
            return new CPlane(o, n);
        }

        public void  CloneTo(CPlane other)
        {
            other.o = o;
            other.n = n;
        }

        public CVector Origin
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

        public CVector Normal
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

        public CVector4 Equation
        {
            get
            {
                return new CVector4(n, -(n*o));
            }
        }

        public double DistanceSigned(CVector p)
        {
            return (p - o) * n;
        }

        public double Distance(CVector p)
        {
            return Math.Abs((p - o) * n);
        }

        public CVector ClosestPoint(CVector p)
        {
            CVector dp=p-o;
            return p-(dp*n)*n;
        }



        CVector o;
        CVector n;
    }
}
