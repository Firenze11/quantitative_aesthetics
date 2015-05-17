using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C_sawapan_media;

namespace testmediasmall
{
    public class Screen
    {
        //canvas property
        public int id;
        public double left, bottom, w, h;
        VBitmap vbit;

        //gaze property
        public byte[] gazeColor = new byte[3];//////////////////////////////////////////////////////////
        List<Vector3d> gazeL = new List<Vector3d>();///////////////////////////////////////////////////EYE GAZE LIST
        double deviation = 0.0;
        public Vector3d projG = new Vector3d(); //projected dpointNorm
        public Vector3d projGM = new Vector3d();//projected gazemedium
        public Vector3d projF = new Vector3d();//projected focus
        int lastf = 8;
        public int num;  //get mask number of the gaze

        //frame control
        int cframe = 0;    //current frame
        double cframeSlowPlayback = 0.0;  //reduce the frame rate for playback
        int pframe = 0;  //frame number of previous clip during transition
        double pframeSlowPlayback = 0.0;
        int newFrame = 0;

        //on/off control
        public bool ison = false;
        int oncount = 0; //loop count AFTER A ZOOM IS FINISHED!
        int onduration = 50000;

        //zoom control
        public bool iszooming = false;
        int zoomcount = 0;
        int zoomduration = 60;
        double zoomrate = 0.01;

        //fade control
        public bool isfading = false;
        int fadecount = 0;
        int fadeduration = 40;

        public Screen(int _id, double _left, double _bottom, double _w, double _h)
        {
            id = _id;  left = _left; bottom = _bottom; w = _w; h = _h;
            if (vbit == null) { vbit = new VBitmap(MediaWindow.Video.ResX, MediaWindow.Video.ResY); }
        }

        public void SetCframe(int _frame)
        {
            cframe = _frame;
            cframeSlowPlayback = _frame;
        }

        public void SetPframe(int _frame)
        {
            pframe = _frame;
            pframeSlowPlayback = _frame;
        }

        public bool IsLookedAt(Vector3d gazeInput) //gazeInout must bee un-unitized (x *=rx, y *= ry)
        {
            if (!ison) { return false; }
               
            else if (left < gazeInput.X && (left+w > gazeInput.X) && (bottom < gazeInput.Y) && (bottom+h > gazeInput.Y))
            {
                for (int i = 0; i < MediaWindow.Screens.Count ; i++)
                {
                    if (i != id) { MediaWindow.Screens[i].OnEyeOut(); }
                } 
                return true;
            }
            else {  return false; }
        }

        public void OnEyeOut()///need modify
        {
            if (!ison) { return; }
            else
            {
                projG = new Vector3d();
                gazeL.Clear();
                deviation = 0.0;
                gazeColor[0] = 255; gazeColor[1] = 255; gazeColor[2] = 255;
                /////////////////////////////////////////////////////////////////reset gaze motion, gaze etc too
            }
        }

        private Vector3d ProjectedGaze(Vector3d gazeInput)
        {
            Vector3d pg = new Vector3d();
            pg.X = Math.Min(Math.Max((gazeInput.X - left) * (double)MediaWindow.rx / w, 2.0), MediaWindow.rx - 3.0);
            pg.Y = Math.Min(Math.Max((gazeInput.Y - bottom) * (double)MediaWindow.ry / h, 2.0), MediaWindow.ry - 3.0);
            pg.Z = 0.0;
            return pg;
        }
        private Vector3d ActualGaze(Vector3d projectedG)
        {
            Vector3d ag = new Vector3d();
            ag.X = (projectedG.X * w / (double)MediaWindow.rx + left) ;
            ag.Y = (projectedG.Y * h / (double)MediaWindow.ry + bottom);
            ag.Z = 0.0;
            return ag;
        }

        private void CalculateGazeProperty(Vector3d gazeInput)///need modify
        {
            //projected gaze
            projG = ProjectedGaze(gazeInput);

            gazeL.Add(projG);
            if (gazeL.Count > 150) { gazeL.RemoveAt(0); }

            //mask num
            /* mask
            |  --     3    --  |
            |   1  |  0  |  2  |
            |  --     4    --  |
            */
            if (projG.Y > 0.75 * h) { num = 4; }
            else if (projG.Y < 0.25 * h) { num = 3; }
            else if (projG.X < 0.25 * w) { num = 1; }
            else if (projG.X > 0.75 * w) { num = 2; }
            else { num = 0; }

            //gaze medium
            if (gazeL.Count >= lastf)
            {
                projGM = new Vector3d(0.0, 0.0, 0.0);
                deviation = 0;
                for (int i = 0; i < lastf; i++) { projGM += gazeL[gazeL.Count - i - 1]; }
                projGM *= (1.0 / lastf);
                for (int i = 0; i < lastf; i++) { deviation += (gazeL[gazeL.Count - i - 1] - projGM).LengthSquared; } //"standard dev"
                deviation = Math.Sqrt(deviation);
                Console.WriteLine("deviation:  "+deviation);
            }

            //gaze color
            gazeColor[0] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].R * 255.0);
            gazeColor[1] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].G * 255.0);
            gazeColor[2] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].B * 255.0);

            ///////////////////////////////////////////////////////////////////////gaze motion, gaze etc... too
        }

        private void TryZoom()
        {
            if (gazeL.Count < lastf) { return; }
            if (iszooming) //post fixation period lasts for zoomduration frames
            {
                zoomcount++;
                if (isfading) { fadecount++; }
                if (zoomcount >= zoomduration)
                {
                    //here write the code that is executed during the transition period [zoom, cut etc....]
                    iszooming = false;
                    isfading = false;
                    Console.WriteLine("zoom and fade stop: "+ id);
                }
                if ((zoomcount >= zoomduration - fadeduration) && !isfading)
                {
                    isfading = true;
                    Console.WriteLine("fade start: ");
                    MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                    Console.WriteLine(MediaWindow.other(id) + " is on");
                    SetCframe(newFrame);
                    fadecount = 0;
                }
            }
            else if (deviation < 20 && deviation > 0.00000000001) //avoid zooming when there's no gaze data (dev = 0)
            {//deviation just dropped below threshold
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                MediaWindow.Screens[MediaWindow.other(id)].iszooming = true;
                MediaWindow.Screens[MediaWindow.other(id)].zoomcount = 0;

                MediaWindow.Screens[MediaWindow.other(id)].SetPframe(cframe); ///Frame reassignments

                //choose which scene to show (just remember it for now, show it later)
                //newFrame[other(sn)] = maskAvgRGBTransition(cframe[sn], num, gazeColor);
                MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue  
                Console.WriteLine("is on: " + MediaWindow.other(id) + "cframe: " + cframe + "newframe: " + MediaWindow.Screens[MediaWindow.other(id)].cframe);
            }
            else//normal viewing period 
            { //write here the code that is executed during normal viewing
            }
        }

        private void FrameUpdate()
        {
            oncount++;
            cframeSlowPlayback += 0.2;
            cframe = (int)Math.Floor(cframeSlowPlayback);
            if (isfading)
            {
                pframeSlowPlayback += 0.2;
                pframe = (int)Math.Floor(pframeSlowPlayback);
            }
            if (cframe >= MediaWindow.Vframe_repository.Count)
            {
                SetCframe(0);
                oncount = 0;
            }
            if (pframe >= MediaWindow.Vframe_repository.Count)
            {
                SetPframe(0);
            }
        }

        public void OnTimeLapse(Vector3d gazeInput)
        {
            if (!ison) {  return; }
            if (oncount > onduration) { ison = false; return; }

            FrameUpdate();
            bool islookedat = IsLookedAt(gazeInput);
            if (islookedat)
            {
                CalculateGazeProperty(gazeInput);
            }
            TryZoom();
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////try other things too
        }

        public void DrawVbit()
        {
            if (!ison) { return; }
            double x0, y0, wd, ht, a;
            if (!iszooming || (iszooming && isfading))
            {
                x0 = left;
                y0 = bottom;
                wd = w;
                ht = h;
                byte[, ,] px = MediaWindow.Vframe_repository[cframe].frame_pix_data;
                vbit.FromFrame(px);
                vbit.Draw(x0, y0, wd, ht, 1.0);
            }

            if (iszooming)
            {
                Vector3d ag = ActualGaze(projF);
                double s = 1.0 + zoomrate * zoomcount;
                x0 = Math.Min(ag.X * (1.0 - s) + left * s, left); // max and min are used to constrain the frame in view port (no pink!)
                y0 = Math.Min(ag.Y * (1.0 - s) + bottom * s, bottom);
                wd = Math.Max(w * s , w - x0);
                ht = Math.Max(h * s , h - y0);
                if (isfading)
                    a = 1.0 - ((double)fadecount) / ((double)fadeduration);
                else
                    a = 1.0;
                byte[, ,] pre_px = MediaWindow.Vframe_repository[pframe].frame_pix_data;
                Console.WriteLine(id + " is drawing pframe " + pframe);
                vbit.FromFrame(pre_px);
                vbit.Draw(x0, y0, wd, ht, a);
            }
        }
    }
}
