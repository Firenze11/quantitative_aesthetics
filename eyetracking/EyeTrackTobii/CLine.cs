using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eyetrack
{
    public class CLine
    {
        public CLine(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            mP0 = new CVector(x0, y0, z0);
            mP1 = new CVector(x1, y1, z1);
            Update();
        }

        public CLine(CVector _v0, CVector _v1)
        {
            mP0 = _v0;
            mP1 = _v1;
            Update();
        }

        public CVector PointAtNorm(double t)
        {
            return mP0 + PointDiff * t;
        }

        public CVector PointAtLength(double l)
        {
            return mP0 + TangentNorm * l;
        }

        public double X0
        {
            get { return mP0.x; }
            set {mP0.x=value; Update();}
        }
        public double Y0
        {
            get { return mP0.y; }
            set {mP0.y=value; Update();}
        }
        public double Z0
        {
            get { return mP0.z; }
            set {mP0.z=value; Update();}
        }

        public double X1
        {
            get { return mP1.x; }
            set { mP1.x = value; Update(); }
        }
        public double Y1
        {
            get { return mP1.y; }
            set { mP1.y = value; Update(); }
        }
        public double Z1
        {
            get { return mP1.z; }
            set { mP1.z = value; Update(); }
        }

        public CVector P0
        {
            get
            {
                return mP0;
            }
            set
            {
                mP0 = value;
                Update();
            }
        }

        public CVector V1
        {
            get
            {
                return mP1;
            }
            set
            {
                mP1 = value;
                Update();
            }
        }

        public CVector PointDiff
        {
            get
            {
                return mTangentNorm*mLength;
            }
            set
            {
                mP1 = mP0+value;
                mLength = PointDiff.Length;
                mTangentNorm = PointDiff;
                mTangentNorm.Normalize();
            }
        }

        public CVector TangentNorm
        {
            get
            {
                return mTangentNorm;
            }
            set
            {
                mTangentNorm = value;
                mP1 = mP0 + mTangentNorm*mLength;
            }
        }

        public double Length
        {
            get
            {
                return mLength;
            }
            set
            {
                mLength = Math.Abs(value);
                mP1 = mP0 + mTangentNorm * mLength;
            }
        }

        public CVector Mid
        {
            get
            {
                return (mP0+mP1)*0.5;
            }
        }

        public void Update()
        {
            mTangentNorm = mP1 - mP0;
            mLength = mTangentNorm.Length;
            if (mLength!=0.0) mTangentNorm*=(1.0/mLength);
        }

        public void ClosestPointInfinite(CVector _p, out CVector _cp, out double _t)
        { 
            _t = (_p - mP0) * mTangentNorm;

            _cp = mTangentNorm * _t+mP0;

            if (mLength != 0.0) _t = _t / Length;
            else _t = 0.0;
        }

        public double ClosestPoint(CVector _p, out CVector _cp, out double _t)
        {

            CVector dvec, pvec;
            double d;
            dvec = _p - mP0;
            d = dvec* mTangentNorm;


            if (d > 0.0 && d < Length)
            {
                _cp = mTangentNorm * d;
                pvec = dvec - _cp;
                _cp += mP0;

                if (mLength != 0.0) _t = d / Length;
                else _t = 0.0;
                return pvec.Length;
            }

            if (d <= 0.0)
            {
                _cp = mP0;
                _t = 0.0;
                return dvec.Length;
            }


            dvec = _p - mP1;
            _cp = mP1;
            _t = 1.0;
            return dvec.Length;
        }

        public bool Intersect(CPlane _pl, out CVector xp)
        {
            xp=_pl.Origin;

            double den = mTangentNorm*_pl.Normal;
            if (den == 0.0) return false;

            den = ((_pl.Origin-mP0)*_pl.Normal)/den;

            xp = mP0 + mTangentNorm * den;

            return true;
        }

        static public bool DistanceSegment(CLine P, CLine Q, out CLine lw)
        {
            lw = new CLine(0.0, 0.0, 0.0, 1.0, 0.0, 0.0);

            CVector P0 = P.P0;
            CVector Q0 = Q.P0;

            CVector u = P.PointDiff;
            CVector v = Q.PointDiff;

            CVector w0 = P0 - Q0;

            double a = u * u;
            double b = u * v;
            double c = v * v;
            double d = u * w0;
            double e = v * w0;

            double den = a * c - b * b;

            if (den == 0.0) return false;

            double sc = (b * e - c * d) / den;
            double tc = (a * e - b * d) / den;

            CVector Pc = P0 + u * sc;
            CVector Qc = Q0 + v * tc;

            lw = new CLine(Pc, Qc);

            return true;
        }

        CVector mP0;
        CVector mP1;
        CVector mTangentNorm;
        double mLength;
    }
}
