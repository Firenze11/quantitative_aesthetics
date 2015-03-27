/*
* Copyright (c) 2012 Panagiotis Michalatos [www.sawapan.eu]
*
* This software is provided 'as-is', without any express or implied
* warranty. In no event will the authors be held liable for any damages
* arising from the use of this software.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

using System.Runtime.InteropServices;

namespace eyetrack
{

    public interface IVectorField<T>
    {
        int Count{get;}
        double Component(int i);

        void Zero();

        void Add(T v);
        void Add(T v, double scale);
        void Sum(T v1, T v2);
        void Sum(T v1, double m1, T v2, double m2);
        void Sum(T v1, T v2, double m2);
        void Difference(T v1, T v2);
        void Subtract(T v);
        void Scale(double m);
        void Reverse();
        void ReverseOf(T v);
        bool IsEqual(T v, double tol = 0.0001);

        void TrilinearInterpolation(T v000, T v100, T v010, T v110, T v001, T v101, T v011, T v111, double di, double dj, double dk);
    }

    public class CCoordSystem
    {
        public CCoordSystem()
        {
            O = CVector.Origin;
            Ex = CVector.Xaxis;
            Ey = CVector.Yaxis;
            Ez = CVector.Zaxis;
        }

        public CCoordSystem(CCoordSystem cs)
        {
            O = cs.O;
            Ex = cs.Ex;
            Ey = cs.Ey;
            Ez = cs.Ez;
        }

        public CCoordSystem(CVector _o, CVector _ex, CVector _ey, CVector _ez)
        {
            O = _o;
            Ex = _ex;
            Ey = _ey;
            Ez = _ez;
        }

        public void Set(CVector _o, CVector _ex, CVector _ey, CVector _ez)
        {
            O = _o;
            Ex = _ex;
            Ey = _ey;
            Ez = _ez;
        }


        public void Orthonormal(CVector _o, CVector _ex, CVector _ey)
        {
            O = _o;
            Ex = _ex;
            Ex.Normalize();

            Ez.Cross(Ex, _ey);
            Ez.Normalize();

            Ey.Cross(Ez, Ex);
            Ey.Normalize();
        }

        public void Orthonormal3P(CVector _p0, CVector _p1, CVector _p2)
        {
            Orthonormal(_p0, _p1 - _p0, _p2 - _p0);
        }

        public static CCoordSystem NewOrthonormalSystem(CVector _o, CVector _ex, CVector _ey)
        {
            CCoordSystem cc = new CCoordSystem();
            cc.Orthonormal(_o, _ex, _ey);
            return cc;
        }

        public static CCoordSystem NewOrthonormalSystem3P(CVector _p0, CVector _p1, CVector _p2)
        {
            CCoordSystem cc = new CCoordSystem();
            cc.Orthonormal3P(_p0, _p1, _p2);
            return cc;
        }

        public CVector Global2LocalPoint(CVector p)
        {
            CVector c1 = CVector.CrossProduct(Ey, Ez);
            CVector c2 = CVector.CrossProduct(Ez, Ex);
            CVector c3 = CVector.CrossProduct(Ex, Ey);

            double det = c3.Dot(Ez);
            if (det == 0.0)
            {
                return CVector.Origin;
            }

            det = 1.0 / det;

            CVector dv = new CVector();

            dv.Difference(p, O);

            CVector proj = new CVector();

            proj.x = dv.Dot(c1) * det;
            proj.y = dv.Dot(c2) * det;
            proj.z = dv.Dot(c3) * det;

            return proj;
        }

        public double Determinant()
        {
            CVector c3 = CVector.CrossProduct(Ex, Ey);
            return c3.Dot(Ez);
        }

        public CVector Global2LocalPointOrtho(CVector p)
        {
            CVector dv = p - O;
            return new CVector(dv * Ex, dv * Ey, dv * Ez);
        }

        public CVector Global2LocalVectorOrtho(CVector dv)
        {
            return new CVector(dv * Ex, dv * Ey, dv * Ez);
        }


        public CVector LocalToGlobalPoint(CVector p)
        {
            return new CVector(O.x + p.x * Ex.x + p.y * Ey.x + p.z * Ez.x,
                                O.y + p.x * Ex.y + p.y * Ey.y + p.z * Ez.y,
                                O.z + p.x * Ex.z + p.y * Ey.z + p.z * Ez.z
                );
        }

        public CVector LocalToGlobalVector(CVector v)
        {
            return new CVector(v.x * Ex.x + v.y * Ey.x + v.z * Ez.x,
                                v.x * Ex.y + v.y * Ey.y + v.z * Ez.y,
                                v.x * Ex.z + v.y * Ey.z + v.z * Ez.z
                );
        }


        public CVector O;
        public CVector Ex;
        public CVector Ey;
        public CVector Ez;
    }


    [StructLayout(LayoutKind.Explicit)]
    public struct CVector2:IVectorField<CVector2>
    {
        public CVector2(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public CVector2(CVector _p)
        {
            x = _p.x;
            y = _p.y;
        }

        public CVector ToVector3(double _z)
        {
            return new CVector(x, y, _z);
        }

        public int Count { get { return 2; } }

        public double Component(int i)
        {
            if (i == 0) return x;
            return y;
        }

        

        public void TrilinearInterpolation(CVector2 v000, CVector2 v100, CVector2 v010, CVector2 v110, CVector2 v001, CVector2 v101, CVector2 v011, CVector2 v111, double di, double dj, double dk)
        {
            x = CVector.TrilinearInterpolation(v000.x, v100.x, v010.x, v110.x, v001.x, v101.x, v011.x, v111.x, di, dj, dk);
            y = CVector.TrilinearInterpolation(v000.y, v100.y, v010.y, v110.y, v001.y, v101.y, v011.y, v111.y, di, dj, dk);
        }

        [FieldOffset(0)]
        public double x;
        [FieldOffset(8)]
        public double y;


        public static CVector2 Origin = new CVector2();
        public static CVector2 Xaxis = new CVector2(1.0, 0.0);
        public static CVector2 Yaxis = new CVector2(0.0, 1.0);

        private const double ONETHIRD = 1.0 / 3.0;

        public double this[int i]
        {
            get
            {
                if (i == 0) return x;
                return y;
            }
            set
            {
                if (i == 0) x = value;
                else y = value;
            }
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(x * x + y * y);
            }
            set
            {
                double l = Math.Sqrt(x * x + y * y);
                if (l == 0.0) return;
                l = value / l;
                x *= l;
                y *= l;
            }
        }

        public double LengthSquared
        {
            get
            {
                return (x * x + y * y);
            }
        }

        public void Zero()
        {
            x = 0.0;
            y = 0.0;
        }

        public void eX()
        {
            x = 1.0;
            y = 0.0;
        }
        public void eY()
        {
            x = 0.0;
            y = 1.0;
        }

        public void FindMin(CVector2 v)
        {
            if (x > v.x) x = v.x;
            if (y > v.y) y = v.y;
        }

        public void FindMax(CVector2 v)
        {
            if (x < v.x) x = v.x;
            if (y < v.y) y = v.y;
        }

        public void FindMin(double _x, double _y)
        {
            if (x > _x) x = _x;
            if (y > _y) y = _y;
        }

        public void FindMax(double _x, double _y)
        {
            if (x < _x) x = _x;
            if (y < _y) y = _y;
        }

        public void Set(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public void Set(CVector2 v, double scale)
        {
            x = v.x * scale;
            y = v.y * scale;
        }

        public void Set(CVector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public void Add(CVector2 v)
        {
            x += v.x;
            y += v.y;
        }

        public void Add(double _x, double _y)
        {
            x += _x;
            y += _y;
        }

        public void Add(CVector2 v, double scale)
        {
            x += v.x * scale;
            y += v.y * scale;
        }

        public void Sum(CVector2 v1, CVector2 v2)
        {
            x = v1.x + v2.x;
            y = v1.y + v2.y;
        }

        public void Sum(CVector2 v1, double m1, CVector2 v2, double m2)
        {
            x = v1.x * m1 + v2.x * m2;
            y = v1.y * m1 + v2.y * m2;
        }

        public void Sum(CVector2 v1, CVector2 v2, double m2)
        {
            x = v1.x + v2.x * m2;
            y = v1.y + v2.y * m2;
        }

        public void Difference(CVector2 v1, CVector2 v2)
        {
            x = v1.x - v2.x;
            y = v1.y - v2.y;
        }

        public void Subtract(CVector2 v)
        {
            x -= v.x;
            y -= v.y;
        }

           public static double CrossProduct(CVector2 v1, CVector2 v2)
        {
            return  v1.x * v2.y - v1.y * v2.x;
        }

        public double Dot(CVector2 v)
        {
            return x * v.x + y * v.y;
        }

        static public double operator *(CVector2 v1, CVector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        static public CVector2 operator *(CVector2 v, double m)
        {
            return new CVector2(v.x * m, v.y * m);
        }

        static public CVector2 operator *(double m, CVector2 v)
        {
            return new CVector2(v.x * m, v.y * m);
        }

        static public CVector2 operator +(CVector2 v1, CVector2 v2)
        {
            return new CVector2(v1.x + v2.x, v1.y + v2.y);
        }

        static public CVector2 operator -(CVector2 v1, CVector2 v2)
        {
            return new CVector2(v1.x - v2.x, v1.y - v2.y);
        }


        static public double operator %(CVector2 v1, CVector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static explicit operator CVector(CVector2 p)
        {
            return new CVector(p.x, p.y, 0.0);
        }

        public void NormalTo(CVector2 v)
        {
            x = v.y;
            y = -v.x;
        }

        public void Scale(double scalefactor)
        {
            x *= scalefactor;
            y *= scalefactor;
        }

        public void Multiply(CVector2 v)
        {
            x *= v.x;
            y *= v.y;

        }
        public void Reverse()
        {
            x = -x;
            y = -y;
        }

        public void ReverseOf(CVector2 v)
        {
            x = -v.x;
            y = -v.y;
        }

        public void Restrict(CVector2 _min, CVector2 _max)
        {
            if (x < _min.x) x = _min.x;
            else if (x > _max.x) x = _max.x;

            if (y < _min.y) y = _min.y;
            else if (y > _max.y) y = _max.y;
        }

        public double Normalize()
        {
            double len = Length;
            if (len == 0.0) return 0.0;

            double ilen = 1.0 / len;
            x *= ilen;
            y *= ilen;

            return len;
        }

        public double Normalize(double scale)
        {
            double len = Length;
            if (len == 0.0) return 0.0;

            double ilen = scale / len;
            x *= ilen;
            y *= ilen;

            return len;
        }

        public void Abs()
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
        }

        static public double TriangleArea(CVector2 p0, CVector2 p1, CVector2 p2)
        {
            return Math.Abs(CVector2.CrossProduct(p1 - p0, p2 - p0) * 0.5);
        }

        public double DistanceTo(CVector2 v)
        {
            CVector2 dvv = new CVector2(x - v.x, y - v.y);
            return dvv.Length;
        }

        public double DistanceToSqr(CVector2 v)
        {
            CVector2 dvv = new CVector2(x - v.x, y - v.y);
            return dvv.LengthSquared;
        }

        public bool IsEqual(CVector2 v, double tol = 0.0001)
        {
            return (Math.Abs(x - v.x) < tol && Math.Abs(y - v.y) < tol);
        }

                
        public CVector2 Mid(CVector2 v)
        {
            return new CVector2((x + v.x) * 0.5, (y + v.y) * 0.5);
        }

        public void Mid(CVector2 va, CVector2 vb)
        {
            x = (va.x + vb.x) * 0.5;
            y = (va.y + vb.y) * 0.5;
        }

        static public CVector2 MidPoint(CVector2 va, CVector2 vb)
        {
            return new CVector2((va.x + vb.x) * 0.5, (va.y + vb.y) * 0.5);
        }


        public static CVector2 Centroid(CVector2 va, CVector2 vb, CVector2 vc)
        {
            return new CVector2((vc.x + va.x + vb.x) * ONETHIRD, (vc.y + va.y + vb.y) * ONETHIRD);
        }

        void MakeCentroid(CVector2 va, CVector2 vb, CVector2 vc)
        {
            x = (vc.x + va.x + vb.x) * ONETHIRD;
            y = (vc.y + va.y + vb.y) * ONETHIRD;
        }


        public CVector2 ProjectionPoint(CVector2 va, CVector2 vb)
        {

            CVector2 result = vb - va;
            result.Normalize();

            result *= ((this - va) * result);
            result += va;

            return result;
        }

       /* void Interpolation(CVector2 iv0, CVector2 iv1, double s0, double s1)
        {
            x = iv0.x * s0 + iv1.x * s1;
            y = iv0.y * s0 + iv1.y * s1;
        }*/

        void AddBestDirection(CVector2 _v0, CVector2 _v1)
        {

            double dt0 = Dot(_v0);
            double dt1 = Dot(_v1);

            if (Math.Abs(dt0) > Math.Abs(dt1))
            {
                if (dt0 > 0.0)
                {
                    Add(_v0);
                }
                else
                {
                    Subtract(_v0);
                }
            }
            else
            {
                if (dt1 > 0.0)
                {
                    Add(_v1);
                }
                else
                {
                    Subtract(_v1);
                }
            }
        }

        void SetToBestDirection(CVector2 _v0, CVector2 _v1)
        {
            double dt0 = Dot(_v0);
            double dt1 = Dot(_v1);

            if (Math.Abs(dt0) > Math.Abs(dt1))
            {
                if (dt0 > 0.0)
                {
                    Set(_v0.x, _v0.y);
                }
                else
                {
                    Set(-_v0.x, -_v0.y);
                }
            }
            else
            {
                if (dt1 > 0.0)
                {
                    Set(_v1.x, _v1.y);
                }
                else
                {
                    Set(-_v1.x, -_v1.y);
                }
            }
        }

        void Reflect(CVector2 _axis)
        {
            CVector2 dummy = this;
            double dotu;
            dotu = dummy * _axis;

            x = 2.0 * dotu * _axis.x - dummy.x;
            y = 2.0 * dotu * _axis.y - dummy.y;
        }

        public static implicit operator string(CVector2 v)
        {
            return "[" + v.x.ToString() + "," + v.y.ToString() +  "]";
        }

        public override string ToString()
        {
            return "[" + x.ToString() + "," + y.ToString() +  "]";
        }

        public string ToString(int _round)
        {
            return "[" + Math.Round(x, _round).ToString() + "," + Math.Round(y, _round).ToString() +  "]";
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CVector4 : IVectorField<CVector4>
    {
        public CVector4(double _x, double _y, double _z, double _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public CVector4(CVector _p, double _w)
        {
            x = _p.x;
            y = _p.y;
            z = _p.z;
            w = _w;
        }

        public CVector ToVector3()
        {
            CVector r = new CVector(x, y, z);
            if (w == 0.0) return r;
            r.Scale(1.0 / w);
            return r;
        }

        public int Count { get { return 4; } }

        public double Component(int i)
        {
            if (i == 0) return x;
            if (i == 1) return y;
            if (i == 2) return z;
            return w;
        }

        public void Set(double _x, double _y, double _z, double _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        [FieldOffset(0)]
        public double x;
        [FieldOffset(8)]
        public double y;
        [FieldOffset(16)]
        public double z;
        [FieldOffset(24)]
        public double w;

        public void TrilinearInterpolation(CVector4 v000, CVector4 v100, CVector4 v010, CVector4 v110, CVector4 v001, CVector4 v101, CVector4 v011, CVector4 v111, double di, double dj, double dk)
        {
            x = CVector.TrilinearInterpolation(v000.x, v100.x, v010.x, v110.x, v001.x, v101.x, v011.x, v111.x, di, dj, dk);
            y = CVector.TrilinearInterpolation(v000.y, v100.y, v010.y, v110.y, v001.y, v101.y, v011.y, v111.y, di, dj, dk);
            z = CVector.TrilinearInterpolation(v000.z, v100.z, v010.z, v110.z, v001.z, v101.z, v011.z, v111.z, di, dj, dk);
            w = CVector.TrilinearInterpolation(v000.w, v100.w, v010.w, v110.w, v001.w, v101.w, v011.w, v111.w, di, dj, dk);
        }

        public void Zero()
        {
            x = 0.0;
            y = 0.0;
            z = 0.0;
            w = 0.0;
        }

        public void Add(CVector4 v)
        {
            x += v.x;
            y += v.y;
            z += v.z;
            w += v.w;
        }
        
        public void Add(CVector4 v, double scale)
        {
            x += v.x * scale;
            y += v.y * scale;
            z += v.z * scale;
            w += v.w * scale;
        }

        public void Sum(CVector4 v1, CVector4 v2)
        {
            x = v1.x + v2.x;
            y = v1.y + v2.y;
            z = v1.z + v2.z;
            w = v1.w + v2.w;
        }

        public void Sum(CVector4 v1, double m1, CVector4 v2, double m2)
        {
            x = v1.x * m1 + v2.x * m2;
            y = v1.y * m1 + v2.y * m2;
            z = v1.z * m1 + v2.z * m2;
            w = v1.w * m1 + v2.w * m2;
        }

        public void Sum(CVector4 v1, CVector4 v2, double m2)
        {
            x = v1.x + v2.x * m2;
            y = v1.y + v2.y * m2;
            z = v1.z + v2.z * m2;
            w = v1.w + v2.w * m2;
        }

        public void Difference(CVector4 v1, CVector4 v2)
        {
            x = v1.x - v2.x;
            y = v1.y - v2.y;
            z = v1.z - v2.z;
            w = v1.w - v2.w;
        }

        public void Subtract(CVector4 v)
        {
            x -= v.x;
            y -= v.y;
            z -= v.z;
            w -= v.w;
        }

   
        public void Scale(double m)
        {
            x *= m;
            y *= m;
            z *= m;
            w *= m;
        }
        public void Reverse()
        {
            x =-x;
            y =-y;
            z =-z;
            w =-w;
        }
        public void ReverseOf(CVector4 v)
        {
            x =-v.x;
            y =-v.y;
            z =-v.z;
            w =-v.w;
        }
        public bool IsEqual(CVector4 v, double tol = 0.0001)
        {
            return (Math.Abs(x - v.x) < tol && Math.Abs(y - v.y) < tol && Math.Abs(z - v.z) < tol && Math.Abs(w - v.w) < tol);
        }     
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CVector : IVectorField<CVector>
    {

        public CVector(double _x, double _y, double _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        [FieldOffset(0)]
        public double x;
        [FieldOffset(8)]
        public double y;
        [FieldOffset(16)]
        public double z;

        public static CVector Origin = new CVector();
        public static CVector Xaxis = new CVector(1.0, 0.0, 0.0);
        public static CVector Yaxis = new CVector(0.0, 1.0, 0.0);
        public static CVector Zaxis = new CVector(0.0, 0.0, 1.0);

        private const double ONETHIRD = 1.0 / 3.0;

        public int Count { get { return 3; } }

        public double Component(int i)
        {
            if (i == 0) return x;
            if (i == 1) return y;
            return z;
        }

        public void TrilinearInterpolation(CVector v000, CVector v100, CVector v010, CVector v110, CVector v001, CVector v101, CVector v011, CVector v111, double di, double dj, double dk)
        {
            x = CVector.TrilinearInterpolation(v000.x, v100.x, v010.x, v110.x, v001.x, v101.x, v011.x, v111.x, di, dj, dk);
            y = CVector.TrilinearInterpolation(v000.y, v100.y, v010.y, v110.y, v001.y, v101.y, v011.y, v111.y, di, dj, dk);
            z = CVector.TrilinearInterpolation(v000.z, v100.z, v010.z, v110.z, v001.z, v101.z, v011.z, v111.z, di, dj, dk);
        }

        public CVector2 XY
        {
            get
            {
                return new CVector2(x, y);
            }
            set
            {
                x = value.x;
                y = value.y;
            }
        }

        public double this[int i]
        {
            get
            {
                if (i == 0) return x;
                if (i == 1) return y;
                return z;
            }
            set
            {
                if (i == 0) x = value;
                else if (i == 1) y = value;
                else z = value;
            }
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(x * x + y * y + z * z);
            }
            set
            {
                double l = Math.Sqrt(x * x + y * y + z * z);
                if (l == 0.0) return;
                l = value / l;
                x *= l;
                y *= l;
                z *= l;
            }
        }

        public double LengthSquared
        {
            get
            {
                return (x * x + y * y + z * z);
            }
        }

        public void Zero()
        {
            x = 0.0;
            y = 0.0;
            z = 0.0;
        }

        public void eX()
        {
            x = 1.0;
            y = 0.0;
            z = 0.0;
        }
        public void eY()
        {
            x = 0.0;
            y = 1.0;
            z = 0.0;
        }
        public void eZ()
        {
            x = 0.0;
            y = 0.0;
            z = 1.0;
        }

        public CVector Normalized
        {
            get
            {
                CVector res = new CVector(x, y, z);
                res.Normalize();
                return res;
            }
        }


        public void FindMin(CVector v)
        {
            if (x > v.x) x = v.x;
            if (y > v.y) y = v.y;
            if (z > v.z) z = v.z;
        }

        public void FindMax(CVector v)
        {
            if (x < v.x) x = v.x;
            if (y < v.y) y = v.y;
            if (z < v.z) z = v.z;
        }

        public void FindMin(double _x, double _y, double _z)
        {
            if (x > _x) x = _x;
            if (y > _y) y = _y;
            if (z > _z) z = _z;
        }

        public void FindMax(double _x, double _y, double _z)
        {
            if (x < _x) x = _x;
            if (y < _y) y = _y;
            if (z < _z) z = _z;
        }

        public void Set(double _x, double _y, double _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public void Set(CVector v, double scale)
        {
            x = v.x * scale;
            y = v.y * scale;
            z = v.z * scale;
        }

        public void Set(CVector v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public void Add(CVector v)
        {
            x += v.x;
            y += v.y;
            z += v.z;
        }

        public void Add(double _x, double _y, double _z)
        {
            x += _x;
            y += _y;
            z += _z;
        }

        public void Add(CVector v, double scale)
        {
            x += v.x * scale;
            y += v.y * scale;
            z += v.z * scale;
        }

        public void Sum(CVector v1, CVector v2)
        {
            x = v1.x + v2.x;
            y = v1.y + v2.y;
            z = v1.z + v2.z;
        }

        public void Sum(CVector v1, double m1, CVector v2, double m2)
        {
            x = v1.x * m1 + v2.x * m2;
            y = v1.y * m1 + v2.y * m2;
            z = v1.z * m1 + v2.z * m2;
        }

        public void Sum(CVector v1, CVector v2, double m2)
        {
            x = v1.x + v2.x * m2;
            y = v1.y + v2.y * m2;
            z = v1.z + v2.z * m2;
        }

        public void Difference(CVector v1, CVector v2)
        {
            x = v1.x - v2.x;
            y = v1.y - v2.y;
            z = v1.z - v2.z;
        }

        public void Subtract(CVector v)
        {
            x -= v.x;
            y -= v.y;
            z -= v.z;
        }

        public void Cross(CVector v1, CVector v2)
        {
            x = v1.y * v2.z - v1.z * v2.y;
            y = v1.z * v2.x - v1.x * v2.z;
            z = v1.x * v2.y - v1.y * v2.x;
        }

        public static CVector CrossProduct(CVector v1, CVector v2)
        {
            return new CVector(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }

        public double Dot(CVector v)
        {
            return x * v.x + y * v.y + z * v.z;
        }

        static public double operator *(CVector v1, CVector v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        static public CVector operator *(CVector v, double m)
        {
            return new CVector(v.x * m, v.y * m, v.z * m);
        }

        static public CVector operator *(double m, CVector v)
        {
            return new CVector(v.x * m, v.y * m, v.z * m);
        }

        static public CVector operator +(CVector v1, CVector v2)
        {
            return new CVector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        static public CVector operator -(CVector v1, CVector v2)
        {
            return new CVector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }


        static public CVector operator %(CVector v1, CVector v2)
        {
            return new CVector(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }


        public void Scale(double scalefactor)
        {
            x *= scalefactor;
            y *= scalefactor;
            z *= scalefactor;
        }

        public void Multiply(CVector v)
        {
            x *= v.x;
            y *= v.y;
            z *= v.z;

        }
        public void Reverse()
        {
            x = -x;
            y = -y;
            z = -z;
        }

        public void ReverseOf(CVector v)
        {
            x = -v.x;
            y = -v.y;
            z = -v.z;
        }

        public void Restrict(CVector _min, CVector _max)
        {
            if (x < _min.x) x = _min.x;
            else if (x > _max.x) x = _max.x;

            if (y < _min.y) y = _min.y;
            else if (y > _max.y) y = _max.y;

            if (z < _min.z) z = _min.z;
            else if (z > _max.z) z = _max.z;
        }

        public double Normalize()
        {
            double len = Length;
            if (len == 0.0) return 0.0;

            double ilen = 1.0 / len;
            x *= ilen;
            y *= ilen;
            z *= ilen;

            return len;
        }

        public double Normalize(double scale)
        {
            double len = Length;
            if (len == 0.0) return 0.0;

            double ilen = scale / len;
            x *= ilen;
            y *= ilen;
            z *= ilen;

            return len;
        }

        public void Abs()
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            z = Math.Abs(z);
        }

        static public double TriangleArea(CVector p0, CVector p1, CVector p2)
        {
            return CVector.CrossProduct(p1 - p0, p2 - p0).Length * 0.5;
        }

        public double DistanceTo(CVector v)
        {
            CVector dvv = new CVector(x - v.x, y - v.y, z - v.z);
            return dvv.Length;
        }

        public double DistanceToXY(CVector v)
        {
            return Math.Sqrt((x - v.x) * (x - v.x) + (y - v.y) * (y - v.y));
        }

        public double DistanceToSqr(CVector v)
        {
            CVector dvv = new CVector(x - v.x, y - v.y, z - v.z);
            return dvv.LengthSquared;
        }

        public bool IsEqual(CVector v, double tol = 0.0001)
        {
            return (Math.Abs(x - v.x) < tol && Math.Abs(y - v.y) < tol && Math.Abs(z - v.z) < tol);
        }


        public void MatrixTransform(double[] ma, out CVector res)
        {
            res = new CVector();
            res.x = x * ma[0] + y * ma[4] + z * ma[8] + ma[12];
            res.y = x * ma[1] + y * ma[5] + z * ma[9] + ma[13];
            res.z = x * ma[2] + y * ma[6] + z * ma[10] + ma[14];
        }

        public void MatrixTransform2D(double[] ma, ref CVector res)
        {
            res.x = x * ma[0] + y * ma[4] + z * ma[8] + ma[12];
            res.y = x * ma[1] + y * ma[5] + z * ma[9] + ma[13];
        }

        public void MatrixTransformP(double[] ma, out CVector res)
        {
            res = new CVector();
            double w = 1.0 / ma[15];

            res.x = (x * ma[0] + y * ma[4] + z * ma[8] + ma[12]) * w;
            res.y = (x * ma[1] + y * ma[5] + z * ma[9] + ma[13]) * w;
            res.z = (x * ma[2] + y * ma[6] + z * ma[10] + ma[14]) * w;
        }

        public CVector Mid(CVector v)
        {
            return new CVector((x + v.x) * 0.5, (y + v.y) * 0.5, (z + v.z) * 0.5);
        }

        public void Mid(CVector va, CVector vb)
        {
            x = (va.x + vb.x) * 0.5;
            y = (va.y + vb.y) * 0.5;
            z = (va.z + vb.z) * 0.5;
        }

        static public CVector MidPoint(CVector va, CVector vb)
        {
            return new CVector((va.x + vb.x) * 0.5, (va.y + vb.y) * 0.5, (va.z + vb.z) * 0.5);
        }

        public void Normal3P(CVector va, CVector vb, CVector vc)
        {
            Cross(vb - va, vc - va);
        }

        public bool IsInsideOrderedPair(CVector va, CVector vb)
        {
            return (x >= va.x && x <= vb.x && y >= va.y && y <= vb.y && z >= va.z && z <= vb.z);
        }


        public static CVector Centroid(CVector va, CVector vb, CVector vc)
        {
            return new CVector((vc.x + va.x + vb.x) * ONETHIRD, (vc.y + va.y + vb.y) * ONETHIRD, (vc.z + va.z + vb.z) * ONETHIRD);
        }

        void MakeCentroid(CVector va, CVector vb, CVector vc)
        {
            x = (vc.x + va.x + vb.x) * ONETHIRD;
            y = (vc.y + va.y + vb.y) * ONETHIRD;
            z = (vc.z + va.z + vb.z) * ONETHIRD;
        }


        public CVector ProjectionPoint(CVector va, CVector vb)
        {

            CVector result = vb - va;
            result.Normalize();

            result *= ((this - va) * result);
            result += va;

            return result;
        }

        void Interpolation(CVector iv0, CVector iv1, double s0, double s1)
        {
            x = iv0.x * s0 + iv1.x * s1;
            y = iv0.y * s0 + iv1.y * s1;
            z = iv0.z * s0 + iv1.z * s1;
        }

        void AddBestDirection(CVector _v0, CVector _v1)
        {

            double dt0 = Dot(_v0);
            double dt1 = Dot(_v1);

            if (Math.Abs(dt0) > Math.Abs(dt1))
            {
                if (dt0 > 0.0)
                {
                    Add(_v0);
                }
                else
                {
                    Subtract(_v0);
                }
            }
            else
            {
                if (dt1 > 0.0)
                {
                    Add(_v1);
                }
                else
                {
                    Subtract(_v1);
                }
            }
        }

        void SetToBestDirection(CVector _v0, CVector _v1)
        {
            double dt0 = Dot(_v0);
            double dt1 = Dot(_v1);

            if (Math.Abs(dt0) > Math.Abs(dt1))
            {
                if (dt0 > 0.0)
                {
                    Set(_v0.x, _v0.y, _v0.z);
                }
                else
                {
                    Set(-_v0.x, -_v0.y, -_v0.z);
                }
            }
            else
            {
                if (dt1 > 0.0)
                {
                    Set(_v1.x, _v1.y, _v1.z);
                }
                else
                {
                    Set(-_v1.x, -_v1.y, -_v1.z);
                }
            }
        }

        void Reflect(CVector _axis)
        {
            CVector dummy = this;
            double dotu;
            dotu = dummy * _axis;

            x = 2.0 * dotu * _axis.x - dummy.x;
            y = 2.0 * dotu * _axis.y - dummy.y;
            z = 2.0 * dotu * _axis.z - dummy.z;

        }

        public static implicit operator string(CVector v)
        {
            return "[" + v.x.ToString() + "," + v.y.ToString() + "," + v.z.ToString() + "]";
        }

        public override string ToString()
        {
            return "[" + x.ToString() + "," + y.ToString() + "," + z.ToString() + "]";
        }

        public  string ToString(int _round)
        {
            return "[" + Math.Round(x, _round).ToString() + "," + Math.Round(y, _round).ToString() + "," + Math.Round(z, _round).ToString() + "]";
        }


        public static double TrilinearInterpolation(double v000, double v100, double v010, double v110, double v001, double v101, double v011, double v111, double di, double dj, double dk)
        {
            /*  double dii = 1.0 - di;
              double dji = 1.0 - dj;
          
              double d0, d1, d2, d3;

              d0 = v000 * dii + v100 * di;
              d1 = v010 * dii + v110 * di;
              d2 = v001 * dii + v101 * di;
              d3 = v011 * dii + v111 * di;

              d0 = d0 * dji + d1 * dj;
              d2 = d2 * dji + d3 * dj;

              return d0 * (1.0-dk) + d2 * dk;*/


            //double d0, d1, d2, d3;

            double dii = 1.0 - di;
            double dji = 1.0 - dj;
            return ((v000 * dii + v100 * di) * dji + (v010 * dii + v110 * di) * dj) * (1.0 - dk) + ((v001 * dii + v101 * di) * dji + (v011 * dii + v111 * di) * dj) * dk;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TensorSym3D : IVectorField<TensorSym3D>
    {

        [FieldOffset(0)]
        public double XX;
        [FieldOffset(8)]
        public double YY;
        [FieldOffset(16)]
        public double ZZ;
        [FieldOffset(24)]
        public double XY;
        [FieldOffset(32)]
        public double YZ;
        [FieldOffset(40)]
        public double ZX;

        //   [FieldOffset(0)]
        //  fixed double fixedBuffer[6];

        public int Count { get { return 6; } }
        public double Component(int i)
        {
            if (i == 0) return XX;
            if (i == 1) return YY;
            if (i == 2) return ZZ;
            if (i == 3) return XY;
            if (i == 4) return YZ;
            return ZX;
        }

        public void Zero()
        {
            XX = 0.0;
            YY = 0.0;
            ZZ = 0.0;
            XY = 0.0;
            YZ = 0.0;
            ZX = 0.0;
        }

        public void Add(TensorSym3D sm)
        {
            XX += sm.XX;
            YY += sm.YY;
            ZZ += sm.ZZ;
            XY += sm.XY;
            YZ += sm.YZ;
            ZX += sm.ZX;
        }

        public void Add(TensorSym3D sm, double m)
        {
            XX += sm.XX * m;
            YY += sm.YY * m;
            ZZ += sm.ZZ * m;
            XY += sm.XY * m;
            YZ += sm.YZ * m;
            ZX += sm.ZX * m;
        }

        public void Sum(TensorSym3D t1, TensorSym3D t2)
        {
            XX = t1.XX + t2.XX;
            YY = t1.YY + t2.YY;
            ZZ = t1.ZZ + t2.ZZ;
            XY = t1.XY + t2.XY;
            YZ = t1.YZ + t2.YZ;
            ZX = t1.ZX + t2.ZX;
        }

        public void Sum(TensorSym3D t1, double m1, TensorSym3D t2, double m2)
        {
            XX = t1.XX * m1 + t2.XX * m2;
            YY = t1.YY * m1 + t2.YY * m2;
            ZZ = t1.ZZ * m1 + t2.ZZ * m2;
            XY = t1.XY * m1 + t2.XY * m2;
            YZ = t1.YZ * m1 + t2.YZ * m2;
            ZX = t1.ZX * m1 + t2.ZX * m2;
        }

        public void Sum(TensorSym3D t1, TensorSym3D t2, double m2)
        {
            XX = t1.XX + t2.XX * m2;
            YY = t1.YY + t2.YY * m2;
            ZZ = t1.ZZ + t2.ZZ * m2;
            XY = t1.XY + t2.XY * m2;
            YZ = t1.YZ + t2.YZ * m2;
            ZX = t1.ZX + t2.ZX * m2;
        }

        public void Difference(TensorSym3D t1, TensorSym3D t2)
        {
            XX = t1.XX - t2.XX;
            YY = t1.YY - t2.YY;
            ZZ = t1.ZZ - t2.ZZ;
            XY = t1.XY - t2.XY;
            YZ = t1.YZ - t2.YZ;
            ZX = t1.ZX - t2.ZX;
        }

        public void Subtract(TensorSym3D sm)
        {
            XX -= sm.XX;
            YY -= sm.YY;
            ZZ -= sm.ZZ;
            XY -= sm.XY;
            YZ -= sm.YZ;
            ZX -= sm.ZX;
        }

        public void Scale(double m)
        {
            XX *= m;
            YY *= m;
            ZZ *= m;
            XY *= m;
            YZ *= m;
            ZX *= m;
        }

        public void Reverse()
        {
            XX = -XX;
            YY = -YY;
            ZZ = -ZZ;
            XY = -XY;
            YZ = -YZ;
            ZX = -ZX;
        }

        public void ReverseOf(TensorSym3D v)
        {
            XX = -v.XX;
            YY = -v.YY;
            ZZ = -v.ZZ;
            XY = -v.XY;
            YZ = -v.YZ;
            ZX = -v.ZX;
        }

        public bool IsEqual(TensorSym3D v, double tol = 0.0001)
        {
            return (Math.Abs(XX - v.XX) < tol &&
                    Math.Abs(YY - v.YY) < tol &&
                    Math.Abs(ZZ - v.ZZ) < tol &&
                    Math.Abs(XY - v.XY) < tol &&
                    Math.Abs(YZ - v.YZ) < tol &&
                    Math.Abs(ZX - v.ZX) < tol
                    );
        }

        public void TrilinearInterpolation(TensorSym3D v000, TensorSym3D v100, TensorSym3D v010, TensorSym3D v110, TensorSym3D v001, TensorSym3D v101, TensorSym3D v011, TensorSym3D v111, double di, double dj, double dk)
        {
            XX = CVector.TrilinearInterpolation(v000.XX, v100.XX, v010.XX, v110.XX, v001.XX, v101.XX, v011.XX, v111.XX, di, dj, dk);
            YY = CVector.TrilinearInterpolation(v000.YY, v100.YY, v010.YY, v110.YY, v001.YY, v101.YY, v011.YY, v111.YY, di, dj, dk);
            ZZ = CVector.TrilinearInterpolation(v000.ZZ, v100.ZZ, v010.ZZ, v110.ZZ, v001.ZZ, v101.ZZ, v011.ZZ, v111.ZZ, di, dj, dk);
            XY = CVector.TrilinearInterpolation(v000.XY, v100.XY, v010.XY, v110.XY, v001.XY, v101.XY, v011.XY, v111.XY, di, dj, dk);
            YZ = CVector.TrilinearInterpolation(v000.YZ, v100.YZ, v010.YZ, v110.YZ, v001.YZ, v101.YZ, v011.YZ, v111.YZ, di, dj, dk);
            ZX = CVector.TrilinearInterpolation(v000.ZX, v100.ZX, v010.ZX, v110.ZX, v001.ZX, v101.ZX, v011.ZX, v111.ZX, di, dj, dk);
        }

        public double Trace
        {
            get
            {
                return XX + YY + ZZ;
            }
        }


    }
}