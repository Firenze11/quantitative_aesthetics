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
        public byte[] gazeColor = new byte[3];
        List<Vector3d> gazeL = new List<Vector3d>();
        double deviation = 0.0;
        public Vector3d projG = new Vector3d(); //projected dpointNorm
        public Vector3d projGM = new Vector3d();//projected gazemedium
        public Vector3d projF = new Vector3d();//projected focus
        int lastf_gazeMedium = 8;
        int lastf_motionPicture = 3;
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


        ///////////////////////////////////////////////////////////////Aiko
        int framecount = 0; //framecount before triggering zoom again
        double frameCountForNoTransition = 0;
        int numFramesInScene = 10;
        double gazeOptFlow = 0.0;
        Vector3d gazeVector = new Vector3d(0.0, 0.0, 0.0);
        double slowPlaybackRate = 0.4;

        int numMotionPicFrames = 20;
        double motion_alpha = 0.2;
        int motionPictureStartFrame = 55;
        public bool motionPicture = false;
        Vector3d gazeOptFlowVector = new Vector3d(0.0, 0.0, 0.0); 
        int motionFrameCounter = 0;
        ///////////////////////////////////////////////////////////////////


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
            id = _id; left = _left; bottom = _bottom; w = _w; h = _h;
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

            else if (left < gazeInput.X && (left + w > gazeInput.X) && (bottom < gazeInput.Y) && (bottom + h > gazeInput.Y))
            {
                for (int i = 0; i < MediaWindow.Screens.Count; i++)
                {
                    if (i != id) { MediaWindow.Screens[i].OnEyeOut(); }
                }
                return true;
            }
            else { return false; }
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
            ag.X = (projectedG.X * w / (double)MediaWindow.rx + left);
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
            if (gazeL.Count >= lastf_gazeMedium)
            {
                projGM = new Vector3d(0.0, 0.0, 0.0);
                deviation = 0;
                for (int i = 0; i < lastf_gazeMedium; i++) { projGM += gazeL[gazeL.Count - i - 1]; }
                projGM *= (1.0 / lastf_gazeMedium);
                for (int i = 0; i < lastf_gazeMedium; i++) { deviation += (gazeL[gazeL.Count - i - 1] - projGM).LengthSquared; } //"standard dev"
                deviation = Math.Sqrt(deviation);
                Console.WriteLine("deviation:  " + deviation);
            }

            //gaze color
            gazeColor[0] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].R * 255.0);
            gazeColor[1] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].G * 255.0);
            gazeColor[2] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].B * 255.0);

            ///////////////////////////////////////////////////////////////////////gaze motion, gaze etc... too
            //optical flow ///////////////////////////////////////////////////////////////////////////////////////Aiko
            if (gazeL.Count >= lastf_motionPicture)
            {
                for (int i = 0; i < lastf_motionPicture; i++)
                {
                    gazeVector = gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - i - 1];
                    double gaze_delta = (gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - i - 1]).Length;

                    //if (gaze_delta > Math.Sqrt((rx / 2) * (rx / 2) + (ry / 2) * (ry / 2))) motionPicture = true; //compare gaze distance travelled with diagonal screen size
                }
            }

            gazeOptFlow = MediaWindow.Vframe_repository[cframe].maskOpticalFlowMovement[num];
            gazeOptFlowVector = MediaWindow.Vframe_repository[cframe].maskOpticalFlowVector[num];
    
                        
        }

        private void TryZoom()
        {
            if (gazeL.Count < lastf_gazeMedium) { return; }
            if (iszooming) //post fixation period lasts for zoomduration frames
            {
                zoomcount++;
                if (isfading) { fadecount++; }
                if (zoomcount >= zoomduration)
                {
                    //here write the code that is executed during the transition period [zoom, cut etc....]
                    iszooming = false;
                    zoomcount = 0; //reset the zoomcount 
                    isfading = false;
                    framecount = 0; //reset the framecount so it does not immediately jump to another scene
                    frameCountForNoTransition = 0;
                    Console.WriteLine("zoom and fade stop: " + id);
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
            else if (framecount > numFramesInScene && deviation < 20 && deviation > 0.00000000001) //avoid zooming when there's no gaze data (dev = 0)
            {//deviation just dropped below threshold
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                MediaWindow.Screens[MediaWindow.other(id)].iszooming = true;
                MediaWindow.Screens[MediaWindow.other(id)].zoomcount = 0;

                MediaWindow.Screens[MediaWindow.other(id)].SetPframe(cframe); ///Frame reassignments

                //choose which scene to show (just remember it for now, show it later)
                //newFrame[other(sn)] = maskAvgRGBTransition(cframe[sn], num, gazeColor);
                MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue  
                MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.maskAvgRGBTransition(cframe, num, gazeColor, true);// CHOOSE BETWEEN!!!!!!!!!!!!!!!!!!!!!!!!!!!/////////////////////////////Aiko
                //MediaWindow.Screens[MediaWindow.other(id)].newFrame = maskOpticalFlowTransition(cframe, num, gazeOptFlow, gazeOptFlowVector, true);//add px optical flow later!!/////////////////////////////Aiko
                Console.WriteLine("is on: " + MediaWindow.other(id) + "cframe: " + cframe + "newframe: " + MediaWindow.Screens[MediaWindow.other(id)].cframe);
            }
            else////////////////////////////////////////////////////////////////Aiko
            { //write here the code that is executed during normal viewing
                frameCountForNoTransition += slowPlaybackRate;
                framecount = (int)Math.Floor(frameCountForNoTransition);
                //Console.WriteLine("framecount: " + framecount);
            }
        }

        /////////////////////////////////////////////////////////////////////////////Aiko
        private void TryMotion()
        {
            if (gazeL.Count < lastf_motionPicture) { return; }
            motionFrameCounter++;
            motionPicture = true;
        }
        /////////////////////////////////////////////////////////////////////////////////

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
            if (!ison) { return; }
            if (oncount > onduration) { ison = false; return; }

            FrameUpdate();
            bool islookedat = IsLookedAt(gazeInput);
            if (islookedat)
            {
                CalculateGazeProperty(gazeInput);
            }
            TryZoom();
            TryMotion();
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
                double s = 1.0 + zoomrate * zoomcount * zoomcount * zoomcount * zoomcount / 10000; //exponential zoomrate increase//Aiko
                x0 = Math.Min(ag.X * (1.0 - s) + left * s, left); // max and min are used to constrain the frame in view port (no pink!)
                y0 = Math.Min(ag.Y * (1.0 - s) + bottom * s, bottom);
                wd = Math.Max(w * s, w - x0);
                ht = Math.Max(h * s, h - y0);

                if (isfading) { a = Math.Min(1.0, Math.Max(0, 1.0 - (double)(0.25 * fadecount * fadecount) / ((double)fadeduration))); }//Aiko
                else { a = 1.0; }

                byte[, ,] pre_px = MediaWindow.Vframe_repository[pframe].frame_pix_data;
                Console.WriteLine(id + " is drawing pframe " + pframe);
                vbit.FromFrame(pre_px);
                vbit.Draw(x0, y0, wd, ht, a);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////Aiko
            if (motionPicture)
            {
                if (cframe > numMotionPicFrames)
                {
                    for (int i = 0; i < numMotionPicFrames; i += 10)   //draw every 5 frames so that motion is clearer
                    {
                        //motion_alpha = 1 - (i/5);
                        vbit.FromFrame(MediaWindow.Vframe_repository[motionPictureStartFrame - i - 1].frame_pix_data);
                        vbit.Draw(0, 0, MediaWindow.rx, MediaWindow.ry, motion_alpha);
                    }
                    motion_alpha *= 0.5;    //make it lighter as time goes on
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
