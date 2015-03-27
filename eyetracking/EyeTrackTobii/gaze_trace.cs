using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace eyetrack
{
    class gaze_trace
    {
        public List<Point3d> trace = new List<Point3d>();
        int MaxTraceCount = 10;

        public void AddTrace(List<Point3d> trace)
        {
            foreach (Point3d pt in trace)
            {
                trace.Add(pt);
            }
            if (trace.Count > MaxTraceCount)
                trace.RemoveAt(0);
        }

    }
}
