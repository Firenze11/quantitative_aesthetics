using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C_sawapan_media;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace testmediasmall
{
    public class Screen
    {
        //canvas property
        public int id;
        public double left, bottom, w, h;
        VBitmap vbit;

        //gaze property
        List<Vector3d> gazeL = new List<Vector3d>();
        double deviation = 0.0;
        public Vector3d projG = new Vector3d(); //projected dpointNorm
        public Vector3d projGM = new Vector3d();//projected gazemedium
        public Vector3d projF = new Vector3d();//projected focus
        public byte[] gazeColor = new byte[3];
        Vector3d gazeOptFlowVector = new Vector3d(0.0, 0.0, 0.0);
        int lastf_gazeMedium = 8;
        int lastf_motionPicture = 3;
        public int num;  //get mask number of the gaze

        //frame control
        int cframe = 0;    //current frame
        double cframeSlowPlayback = 0.0;  //reduce the frame rate for playback
        int pframe = 0;  //frame number of previous clip during transition
        double pframeSlowPlayback = 0.0;
        int newFrame = 0;

        public double tx0, ty0, tx1, ty1;
        //on/off control
        public bool ison = true;
        int oncount = 0; //loop count AFTER A ZOOM IS FINISHED!
        int onduration = 50000;


        ///////////////////////////////////////////////////////////////Aiko
        int framecount = 0; //framecount before triggering zoom again
        double frameCountForNoTransition = 0;
        int numFramesInScene = 10;
        double gazeOptFlow = 0.0;
        Vector3d gazeVector = new Vector3d(0.0, 0.0, 0.0);
        double slowPlaybackRate = 1.0;

        //motion control
        public bool motionMode = false;
        public bool ismotion = false;
        int motioncount = 0;
        double motioncountSlow;
        static int motionduration = 20;
        static int motionStartF = 55;
        static int motionInterval = 6;

        //zoom control
        public bool zoomMode = true;
        public bool iszooming = false;
        int zoomcount = 0;
        static int zoomduration = 60;
        static double zoomrate = 0.01;

        //fade control
        public bool isfading = false;
        int fadecount = 0;
        static int fadeduration = 40;

        //sequence control
        public bool sequenceMode = false;
        bool issequencing = false;
        int sequencecount = 0;
        static int sequenceduration = 60;
        static int sequenceStartF = 25;
        static int sequenceInterval = 3;

        public Screen(int _id, double _left, double _bottom, double _w, double _h)
        {
            id = _id; left = _left; bottom = _bottom; w = _w; h = _h;
            if (vbit == null) { vbit = new VBitmap(MediaWindow.rx, MediaWindow.ry); }
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
            //pg.X = Math.Min(Math.Max((gazeInput.X - left) * (double)MediaWindow.rx / w, 2.0), MediaWindow.rx - 3.0);
            //pg.Y = Math.Min(Math.Max((gazeInput.Y - bottom) * (double)MediaWindow.ry / h, 2.0), MediaWindow.ry - 3.0);
            pg.X = (((gazeInput.X - left) / w) *(tx1 - tx0) + tx0) * MediaWindow.rx;
            pg.Y = (((gazeInput.Y - bottom) / h) * (ty1 - ty0) + ty0) * MediaWindow.ry;
            pg.Z = 0.0;
            return pg;
        }
        private Vector3d ActualGaze(Vector3d projectedG)
        {
            Vector3d ag = new Vector3d();
            //ag.X = (projectedG.X * w / (double)MediaWindow.rx + left);
            //ag.Y = (projectedG.Y * h / (double)MediaWindow.ry + bottom);
            ag.X = ((projectedG.X / MediaWindow.rx) - tx0) * w / (tx1 - tx0) + left;
            ag.Y = ((projectedG.Y / MediaWindow.ry) - ty0) * h / (ty1 - ty0) + bottom;
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
            }

            //gaze color/////////////////////////////////NEED TO CONFIRM VBIT HAS CORRECT CONTENT!!
            gazeColor[0] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].R * 255.0);
            gazeColor[1] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].G * 255.0);
            gazeColor[2] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].B * 255.0);

            ///////////////////////////////////////////////////////////////////////gaze motion, gaze etc... too
            //gaze vector
            if (gazeL.Count >= lastf_motionPicture)////NOT FINISHED
            {
                for (int i = 0; i < lastf_motionPicture; i++)
                {
                    gazeVector = gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - i - 1];
                    double gaze_delta = (gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - i - 1]).Length;
                }
            }
            //gaze optical flow////NOT FINISHED
            gazeOptFlow = MediaWindow.Vframe_repository[cframe].maskOpticalFlowMovement[num];
            gazeOptFlowVector = MediaWindow.Vframe_repository[cframe].maskOpticalFlowVector[num];
        }

        private void TryZoom()
        {
            if (gazeL.Count < lastf_gazeMedium || !zoomMode) { return; }
            if (iszooming)
            {
                if (zoomcount >= zoomduration)
                {
                    //here write the code that is executed during the transition period [zoom, cut etc....]
                    iszooming = false;
                    zoomcount = 0;
                    isfading = false;
                    framecount = 0;
                    frameCountForNoTransition = 0;
                    return;
                }
                if ((zoomcount >= zoomduration - fadeduration) && !isfading)
                {
                    isfading = true;
                    MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                    SetCframe(newFrame);
                    fadecount = 0;
                }
                zoomcount++;
                if (isfading) { fadecount++; }
            }
            else if (framecount > numFramesInScene && deviation < 20 && deviation > 0.00000000001) //avoid zooming when there's no gaze data (dev = 0)
            {
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                MediaWindow.Screens[MediaWindow.other(id)].iszooming = true;
                MediaWindow.Screens[MediaWindow.other(id)].zoomcount = 0;
                MediaWindow.Screens[MediaWindow.other(id)].SetPframe(cframe); ///Frame reassignments

                //choose which scene to show (just remember it for now, show it later)
                //newFrame[other(sn)] = maskAvgRGBTransition(cframe[sn], num, gazeColor);
                MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue  
                MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.maskAvgRGBTransition(cframe, num, gazeColor, true);// CHOOSE BETWEEN!!!!!!!!!!!!!!!!!!!!!!!!!!!/////////////////////////////Aiko
                //MediaWindow.Screens[MediaWindow.other(id)].newFrame = maskOpticalFlowTransition(cframe, num, gazeOptFlow, gazeOptFlowVector, true);//add px optical flow later!!//////////////////////////
            }
            else////////////////////////////////////////////////////////////////Aiko
            { //write here the code that is executed during normal viewing
                //frameCountForNoTransition += slowPlaybackRate;
                //framecount = (int)Math.Floor(frameCountForNoTransition);
            }
        }

        /////////////////////////////////////////////////////////////////////////////Aiko
        private void TryMotion()
        {
            if (gazeL.Count < lastf_motionPicture || !motionMode) { return; }
            if (ismotion) //post fixation period lasts for zoomduration frames
            {
                if (motioncount >= motionduration)
                {
                    ismotion = false;
                    return;
                }
                motioncountSlow += slowPlaybackRate;  //independent counter from the beginning of film
                motioncount = (int)Math.Floor(motioncountSlow);
            }
            else if (cframe > motionStartF) //any condition that triggers motion mode, maybe gaze optical flow....
            {
                //projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                //MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                ismotion = true;//when one screen itself is in motion mode, it can't trigger other screen's motion mode
                motioncount = 0;
                motioncountSlow = 0;
                //MediaWindow.Screens[MediaWindow.other(id)].SetPframe(cframe); ///Frame reassignments
            }
            else
            {
            }
        }

        private void TrySequence()
        {
            if (gazeL.Count < lastf_motionPicture || !sequenceMode) { return; }
            if (issequencing) //post fixation period lasts for zoomduration frames
            {
                if (sequencecount > sequenceduration)
                {
                    issequencing = false;
                    Console.WriteLine(id + " stops sequencing");
                    return;
                }
                sequencecount++;
            }
            else if (cframe > sequenceStartF) //any condition that triggers motion mode, maybe gaze optical flow....
            {
                //projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                //MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                issequencing = true;//when one screen itself is in motion mode, it can't trigger other screen's motin mode
                sequencecount = 0;
                MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                MediaWindow.Screens[MediaWindow.other(id)].issequencing = true;
                MediaWindow.Screens[MediaWindow.other(id)].sequencecount = 0;
                MediaWindow.Screens[MediaWindow.other(id)].SetCframe(cframe - sequenceInterval);
                Console.WriteLine(id + " is sequencing, cframe = " + cframe + ", " + MediaWindow.other(id) + " cframe = " + (cframe - sequenceInterval));
            }
            else
            {
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
            // if (!ison) { return; }
            // if (oncount > onduration) { ison = false; return; }

            FrameUpdate();
            bool islookedat = IsLookedAt(gazeInput);
            if (islookedat)
            {
                CalculateGazeProperty(gazeInput);
            }
            TryZoom();
            TryMotion();
            TrySequence();
            //////////////////////////////////////////////////////////////////////////////////try other things too
        }

        public void DrawVbit()
        {
            // if (!ison) { return; }
            double x0, y0, wd, ht, a;

            if (ismotion)
            {
                x0 = left;
                y0 = bottom;
                wd = w;
                ht = h;
                a = 0.3;
                ///if (motioncount % motionInterval == 0)
                //{
                for (int i = motioncount; i < 1; i += motionInterval)
                {
                    vbit.FromFrame(MediaWindow.Vframe_repository[cframe - i].pix_data);
                    vbit.Draw(x0, y0, wd, ht, a);
                }

            }

            else if (!iszooming || (iszooming && isfading))
            {
                x0 = left;
                y0 = bottom;
                wd = w;
                ht = h;
                byte[, ,] px = MediaWindow.Vframe_repository[cframe].pix_data;
                vbit.FromFrame(px);
                vbit.Update();
                //vbit.Draw(x0, y0, wd, ht, 1.0);

                GL.Enable(EnableCap.Texture2D);


                GL.BindTexture(TextureTarget.Texture2D, vbit.texid);
                GL.Color4(1.0, 1.0, 1.0, 1.0);

                //GL.Begin(PrimitiveType.Quads);
                //GL.TexCoord2(tx0, ty0);
                //GL.Vertex2(x0, y0);

                //GL.TexCoord2(tx1, ty0);
                //GL.Vertex2(x0 + w, y0);

                //GL.TexCoord2(tx1, ty1);
                //GL.Vertex2(x0 + w, y0 + h);

                //GL.TexCoord2(tx0, ty1);
                //GL.Vertex2(x0, y0 + h);

                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(tx0, ty0);
                GL.Vertex2(x0, y0);

                GL.TexCoord2(tx1, ty0);
                GL.Vertex2(x0 + w, y0);

                GL.TexCoord2(tx1, ty1);
                GL.Vertex2(x0 + w, y0 + h);

                GL.TexCoord2(tx0, ty1);
                GL.Vertex2(x0, y0 + h);
                GL.End();

                GL.Disable(EnableCap.Texture2D);
            }

            if (iszooming)
            {
                Vector3d ag = ActualGaze(projF);
                double s = 1.0 + zoomrate * zoomcount * zoomcount * zoomcount * zoomcount / 10000;
                x0 = Math.Min(ag.X * (1.0 - s) + left * s, left);
                y0 = Math.Min(ag.Y * (1.0 - s) + bottom * s, bottom);
                wd = Math.Max(w * s, w - x0);
                ht = Math.Max(h * s, h - y0);

                if (isfading) { a = Math.Min(1.0, Math.Max(0, 1.0 - (double)(0.25 * fadecount * fadecount) / ((double)fadeduration))); }//Aiko
                else { a = 1.0; }

                byte[, ,] pre_px = MediaWindow.Vframe_repository[pframe].pix_data;
                vbit.FromFrame(pre_px);
                vbit.Update();
                //vbit.Draw(x0, y0, wd, ht, a);

                GL.Enable(EnableCap.Texture2D);


                GL.BindTexture(TextureTarget.Texture2D, vbit.texid);
                GL.Color4(1.0, 1.0, 1.0, 1.0);

                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(tx0, ty0);
                GL.Vertex2(x0, y0);

                GL.TexCoord2(tx1, ty0);
                GL.Vertex2(x0 + w, y0);

                GL.TexCoord2(tx1, ty1);
                GL.Vertex2(x0 + w, y0 + h);

                GL.TexCoord2(tx0, ty1);
                GL.Vertex2(x0, y0 + h);
                GL.End();

                GL.Disable(EnableCap.Texture2D);
            }
        }
    }
}