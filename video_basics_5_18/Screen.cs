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
        public double left, bottom, w, h, tx0, ty0, tx1, ty1;
        VBitmap vbit;
        public enum Mode { zoom, sequence, motion, pan}
        public Mode mode = Mode.pan;
        static double _rx = MediaWindow.rx;
        static double _ry = MediaWindow.ry;

        //gaze property
        List<Vector3d> gazeL = new List<Vector3d>();
        double deviation = 0.0;
        public Vector3d projG = new Vector3d(); //projected dpointNorm
        public Vector3d projGM = new Vector3d();//projected gazemedium
        public Vector3d projF = new Vector3d();//projected focus
        public byte[] gazeColor = new byte[3];
        Vector3d gazeOptFlowVector = new Vector3d(0.0, 0.0, 0.0);
        double gazeOptFlow = 0.0;
        Vector3d gazeVector;
        int lastf_gazeMedium = 8;
        int lastf_motionPicture = 3;
        public int num;  //get mask number of the gaze

        //frame control
        int cframe = 0;    //current frame
        int pframe = 0;  //frame number of previous clip during transition
        int newFrame = 0;

        //on/off control
        public bool ison = false;

        int framecount = 0; //framecount before triggering zoom again
        int transitionInterval = 10;

        //pan control
        public bool ispanning = false;
        int pancount = 0;
        static int panduration = 60;
        static double panrate = 0.01;

        //motion control
        public bool ismotion = false;
        int motioncount = 0;
        static int motionduration = 20;
        static int motionStartF = 55;
        static int motionInterval = 6;

        //zoom control
        public bool iszooming = false;
        int zoomcount = 0;
        static int zoomduration = 60;
        static double zoomrate = 0.01;

        //fade control
        public bool isfading = false;
        int fadecount = 0;
        static int fadeduration = 40;

        //sequence control
        bool issequencing = false;
        int sequencecount = 0;
        static int sequenceduration = 60;
        static int sequenceStartF = 25;
        static int sequenceInterval = 3;

        static int next(int n)
        {
            return (n + 1) % MediaWindow.screenCount;
        }
        static int prev(int n)
        {
            return (n + 4) % MediaWindow.screenCount;
        }
        public Screen(int _id, double _left, double _bottom, double _w, double _h)
        {
            id = _id; left = _left; bottom = _bottom; w = _w; h = _h;
            if (vbit == null) { vbit = new VBitmap(MediaWindow.rx, MediaWindow.ry); }
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
                gazeVector = new Vector3d(0.0, 0.0, 0.0);
                /////////////////////////////////////////////////////////////////reset gaze motion, gaze etc too
            }
        }
        private Vector3d ProjectedGaze(Vector3d gazeInput)
        {
            Vector3d pg = new Vector3d();
            //pg.X = Math.Min(Math.Max((gazeInput.X - left) * (double)MediaWindow.rx / w, 2.0), MediaWindow.rx - 3.0);
            //pg.Y = Math.Min(Math.Max((gazeInput.Y - bottom) * (double)MediaWindow.ry / h, 2.0), MediaWindow.ry - 3.0);
            pg.X = (((gazeInput.X - left) / w) * (tx1 - tx0) + tx0) * _rx;
            pg.Y = (((gazeInput.Y - bottom) / h) * (ty1 - ty0) + ty0) * _ry;
            pg.Z = 0.0;
            return pg;
        }
        private Vector3d ActualGaze(Vector3d projectedG)
        {
            Vector3d ag = new Vector3d();
            //ag.X = (projectedG.X * w / (double)MediaWindow.rx + left);
            //ag.Y = (projectedG.Y * h / (double)MediaWindow.ry + bottom);
            ag.X = ((projectedG.X / _rx) - tx0) * w / (tx1 - tx0) + left;
            ag.Y = ((projectedG.Y / _ry) - ty0) * h / (ty1 - ty0) + bottom;
            ag.Z = 0.0;
            return ag;
        }

        private void CalculateGazeProperty(Vector3d gazeInput)///need modify
        {
            //projected gaze
            projG = ProjectedGaze(gazeInput);

            gazeL.Add(projG);
            if (gazeL.Count > 20) { gazeL.RemoveAt(0); }

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

            if (gazeL.Count >1)
                gazeVector = gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - 2];
            else
                gazeVector = new Vector3d(0.0,0.0,0.0) ;
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

        private void DoZoom()
        {
            if (iszooming)
            {
                if (zoomcount >= zoomduration)
                {
                    //here write the code that is executed during the transition period [zoom, cut etc....]
                    iszooming = false;
                    zoomcount = 0;
                    framecount = 0;
                    isfading = false;

                    Console.WriteLine(id + " stops zooming, cf = " + cframe);
                    return;
                }
                if ((zoomcount >= zoomduration - fadeduration) && !isfading)
                {
                    isfading = true;
                    MediaWindow.Screens[next(id)].ison = true;
                    //pframe = cframe;
                    cframe = newFrame;
                    Console.WriteLine(id + " is fading, pf = " + pframe +", cf = " + cframe);

                    fadecount = 0;
                }
                zoomcount++;
                if (isfading) { fadecount++; }
            }
            else if (deviation < 20 && deviation > 0.00000000001 && framecount > transitionInterval ) //avoid zooming when there's no gaze data (dev = 0)
            {
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                MediaWindow.Screens[next(id)].ison = true;
                iszooming = true;
                zoomcount = 0;
                pframe = cframe; ///Frame reassignments

                Console.WriteLine(id + " is zooming, cf = "+cframe);

                //choose which scene to show (just remember it for now, show it later)
                //newFrame[other(sn)] = maskAvgRGBTransition(cframe[sn], num, gazeColor);
                newFrame = MediaWindow.domiHueTransition(cframe, true);  //while in zooming identify the next frame to show: from the repository pick the one with same domihue  
                //MediaWindow.Screens[MediaWindow.other(id)].newFrame = MediaWindow.maskAvgRGBTransition(cframe, num, gazeColor, true);// CHOOSE BETWEEN!!!!!!!!!!!!!!!!!!!!!!!!!!!/////////////////////////////Aiko
                //MediaWindow.Screens[MediaWindow.other(id)].newFrame = maskOpticalFlowTransition(cframe, num, gazeOptFlow, gazeOptFlowVector, true);//add px optical flow later!!//////////////////////////
            }
            else////////////////////////////////////////////////////////////////Aiko
            { //write here the code that is executed during normal viewing
                //frameCountForNoTransition += slowPlaybackRate;
                //framecount = (int)Math.Floor(frameCountForNoTransition);
            }
        }
        void DoPan()
        {
            double dir = Vector3d.Dot(new Vector3d(-1.0, 0.0, 0.0), gazeVector);
            if (ispanning) //post fixation period lasts for zoomduration frames
            {
                if (pancount > panduration)
                {
                    ispanning = false;
                    Console.WriteLine(id + " stops panning");
                    return;
                }
                pancount++;
                Console.WriteLine(id + " pancount = " + pancount);
            }
            if (dir > 0.0) //any condition that triggers motion mode, maybe gaze optical flow....
            {

                if ( !MediaWindow.Screens[prev(id)].ispanning)
                {
                    MediaWindow.Screens[prev(id)].ison = true;
                    MediaWindow.Screens[prev(id)].ispanning = true;
                    MediaWindow.Screens[prev(id)].pancount = 0;
                    MediaWindow.Screens[prev(id)].cframe = cframe;
                    Console.WriteLine(prev(id) + " is panning, cframe = " + cframe);
                }
            }
            else
            {
            }
        }
        private void DoMotion()
        {
            if (ismotion) //post fixation period lasts for zoomduration frames
            {
                if (motioncount >= motionduration)
                {
                    ismotion = false;
                    return;
                }
                motioncount++;
            }
            else if (cframe > motionStartF) //any condition that triggers motion mode, maybe gaze optical flow....
            {
                //projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process

                //MediaWindow.Screens[MediaWindow.other(id)].ison = true;
                ismotion = true;//when one screen itself is in motion mode, it can't trigger other screen's motin mode
                motioncount = 0;
                //MediaWindow.Screens[MediaWindow.other(id)].SetPframe(cframe); ///Frame reassignments
            }
            else
            {
            }
        }
        void DoSequence()
        {
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
                MediaWindow.Screens[next(id)].ison = true;
                MediaWindow.Screens[next(id)].issequencing = true;
                MediaWindow.Screens[next(id)].sequencecount = 0;
                MediaWindow.Screens[next(id)].cframe = cframe - sequenceInterval;
                Console.WriteLine(id + " is sequencing, cframe = " + cframe + ", " + next(id) + " cframe = " + (cframe - sequenceInterval));
            }
            else
            {
            }
        }

        private void FrameUpdate()
        {
            framecount++;
            cframe++;
            if (iszooming)
            {
                pframe++;
            }
            if (cframe >= MediaWindow.Vframe_repository.Count)
            {
                cframe = 0;
            }
            if (pframe >= MediaWindow.Vframe_repository.Count)
            {
                pframe = 0;
            }

            //Console.WriteLine(id + " cf = " + cframe);
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
            if (mode == Mode.zoom) DoZoom();
            if (mode == Mode.motion) DoMotion();
            if (mode == Mode.sequence) DoSequence();
            if (mode == Mode.pan) DoPan();
            //////////////////////////////////////////////////////////////////////////////////try other things too
        }

        public void DrawVbit()
        {
            // if (!ison) { return; }
            double _tx0, _tx1, _ty0, _ty1, a;

            if (ismotion)
            {
                a = 0.3;
                ///if (motioncount % motionInterval == 0)
                //{
                for (int i = motioncount; i < 1; i += motionInterval)
                {
                    vbit.FromFrame(MediaWindow.Vframe_repository[cframe - i].pix_data);
                    vbit.Draw(left, bottom, w, h, a);
                }
            }

            else if (!iszooming || (iszooming && isfading))
            {
                byte[, ,] px = MediaWindow.Vframe_repository[cframe].pix_data;
                vbit.FromFrame(px);
                vbit.Update();
                //vbit.Draw(x0, y0, wd, ht, 1.0);

                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, vbit.texid);
                GL.Color4(1.0, 1.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.Quads);

                GL.TexCoord2(tx0, ty0);
                GL.Vertex2(left, bottom);

                GL.TexCoord2(tx1, ty0);
                GL.Vertex2(left + w, bottom);

                GL.TexCoord2(tx1, ty1);
                GL.Vertex2(left + w, bottom + h);

                GL.TexCoord2(tx0, ty1);
                GL.Vertex2(left, bottom + h);

                GL.End();
                GL.Disable(EnableCap.Texture2D);
            }

            if (iszooming || ispanning)
            {
                if (iszooming)
                {
                    Vector3d ag = ActualGaze(projF);
                    double s = 1.0 + zoomrate * zoomcount * zoomcount * zoomcount * zoomcount / 10000;
                    _tx0 = ((s - 1.0) * projF.X / _rx + tx0) / s;
                    _tx1 = ((s - 1.0) * projF.X / _rx + tx1) / s;
                    _ty0 = ((s - 1.0) * projF.Y / _ry + ty0) / s;
                    _ty1 = ((s - 1.0) * projF.Y / _ry + ty1) / s;

                    byte[, ,] pre_px = MediaWindow.Vframe_repository[pframe].pix_data;
                    vbit.FromFrame(pre_px);
                }
                else
                {
                    double s = pancount / panduration ;
                    _tx0 = tx0 * (1.0 - s);
                    _tx1 = tx1 - s * tx0;
                    _ty0 = ty0;
                    _ty1 = ty1;

                    byte[, ,] pre_px = MediaWindow.Vframe_repository[cframe].pix_data;
                    vbit.FromFrame(pre_px);
                }
                vbit.Update();

                if (isfading) { a = Math.Min(1.0, Math.Max(0, 1.0 - (double)(0.25 * fadecount * fadecount) / ((double)fadeduration))); }//Aiko
                else { a = 1.0; }

                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, vbit.texid);
                GL.Color4(1.0, 1.0, 1.0, a);
                GL.Begin(PrimitiveType.Quads);

                GL.TexCoord2(_tx0, _ty0);
                GL.Vertex2(left, bottom);

                GL.TexCoord2(_tx1, _ty0);
                GL.Vertex2(left + w, bottom);

                GL.TexCoord2(_tx1, _ty1);
                GL.Vertex2(left + w, bottom + h);

                GL.TexCoord2(_tx0, _ty1);
                GL.Vertex2(left, bottom + h);

                GL.End();
                GL.Disable(EnableCap.Texture2D);

                //Console.WriteLine("__txy01: " + _tx0 + ", " + _tx1 + ", " + _ty0 + ", " + _ty1);
            }
        }
    }
}