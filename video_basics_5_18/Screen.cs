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

        public enum Mode { zoom, sequence, motion, pan, color}
        public Mode mode = Mode.zoom;

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
        public int cframe = 0;    //current frame
        int pframe = 0;  //frame number of previous clip during transition
        public double cframeSmooth = 0.0;
        int newFrame = 0;

        //on/off control
        public bool ison = false;

        public static int framecount = 0; //framecount before triggering zoom again
        static int transitionInterval = 10;

        //pan control
        public bool ispanning = false;
        int pancount = 0;
        static int panduration = 30;
        public int pandir = 0;

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

        //color control
        public bool iscolor = false;
        public int threshold = 0;

        //fade control
        public bool isfading = false;
        int fadecount = 0;
        static int fadeduration = 40;

        //sequence control
        static bool issequencing = false;
        static double sequenceDurationEnlarge = 9.0;
        static double sequenceDuration;
        static int sequenceStartF;
        static double sequenceTriggerDist = 40;
        static string sequenceDir;
        static int squenceExtension = 0;

        static int next(int n)
        {
            return (n + 1) % MediaWindow.screenCount;
        }
        static int prev(int n)
        {
            return (n + 2) % MediaWindow.screenCount;
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
                Console.WriteLine("deviation: " + deviation);
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
                    threshold = 0;

                    Console.WriteLine(id + " stops zooming, cf = " + cframe);
                    return;
                }
                if ((zoomcount >= zoomduration - fadeduration) && !isfading)
                {
                    isfading = true;
                    MediaWindow.Screens[next(id)].ison = true;
                    //pframe = cframe;
                    cframe = newFrame;
                    cframeSmooth = newFrame;
                    Console.WriteLine(id + " is fading, pf = " + pframe +", cf = " + cframe);

                    fadecount = 0;
                }
                zoomcount++;
                if (isfading) { fadecount++; }
            }
            //else if (deviation < 100)
            //{
            //    //draw the color change 
            //    byte[,,] limitedPalette = RecreateColor(MediaWindow.Vframe_repository[cframe], 5.0);
            //    //limitedPalette;

            //}
            else if (deviation < 20 && deviation > 0.00000000001 && framecount > transitionInterval) //avoid zooming when there's no gaze data (dev = 0)
            {
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process
                MediaWindow.Screens[next(id)].ison = true;

                iszooming = true;
                zoomcount = 0;
                pframe = cframe; ///Frame reassignments

                Console.WriteLine(id + " is zooming, cf = "+cframe);

                //choose which scene to show (just remember it for now, show it later)
                newFrame = MediaWindow.domiHueTransition(cframe, true); 
            }
            else
            {
            }
        }
        void DoPan()
        {
            if (ispanning) //post fixation period lasts for zoomduration frames
            {
                if (pancount > panduration)
                {
                    ispanning = false;
                    framecount = 0;
                    Console.WriteLine(id + " stops panning");
                }
                else
                {
                    pancount++;
                }
            }
            else
            {
                if (Vector3d.Dot(new Vector3d(-1.0, 0.0, 0.0), gazeVector) > 0.0 && framecount > transitionInterval ) 
                {
                    //MediaWindow.Screens[prev(id)].ison = true;
                    ispanning = true;
                    pandir = -1;
                    pancount = 0;
                    //MediaWindow.Screens[prev(id)].cframe = cframe;
                    Console.WriteLine(id + " is panning, cframe = " + cframe + " pandir = " + pandir);
                }
                else if (Vector3d.Dot(new Vector3d(1.0, 0.0, 0.0), gazeVector) > 0.0 && framecount > transitionInterval) //any condition that triggers motion mode, maybe gaze optical flow....
                {
                    //MediaWindow.Screens[prev(id)].ison = true;
                    ispanning = true;
                    pandir = 1;
                    pancount = 0;
                    Console.WriteLine(id + " is panning, cframe = " + cframe + " pandir = " + pandir);
                }
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
                ismotion = true;//when one screen itself is in motion mode, it can't trigger other screen's motin mode
                motioncount = 0;
            }
        }
        void DoSequence()
        {
            if (MediaWindow.gazeL.Count < 1) return;
            Vector3d p1 = MediaWindow.gazeL[MediaWindow.gazeL.Count - 1];
            Vector3d p0 = MediaWindow.gazeL[MediaWindow.gazeL.Count - 2];
            if (!issequencing)
            {
                if (Math.Abs(p1.X - p0.X) > sequenceTriggerDist && framecount > transitionInterval)
                {
                    issequencing = true;
                    sequenceDuration = sequenceDurationEnlarge * MediaWindow.rx / Math.Abs(p1.X - p0.X);
                    sequenceStartF = cframe;
                    if (p1.X - p0.X > 0) { sequenceDir = "right"; } else { sequenceDir = "left"; }
                    Console.Write("sequence starts, cf = ");
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        Console.Write(MediaWindow.Screens[i].cframe + ", ");
                    }
                    Console.WriteLine();
                }
                else { return; }
            }
            if (issequencing && cframeSmooth > sequenceStartF + 1.33333 * sequenceDuration) 
            {
                if (sequenceDir == "right")
                {
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        MediaWindow.Screens[i].cframeSmooth = (double)MediaWindow.Screens[2].cframe; //in case 3 screens are not syncronized after sequencing period
                    }
                }
                else //if (sequenceDir == "left")
                {
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        MediaWindow.Screens[i].cframeSmooth = (double)MediaWindow.Screens[0].cframe;
                    }
                }
                issequencing = false;
                framecount = 0;
                Console.Write("sequence end, cf = ");
                for (int i = 0; i < MediaWindow.screenCount; i++)
                {
                    Console.Write((int)MediaWindow.Screens[i].cframeSmooth + ", ");
                }
                Console.WriteLine();
                return;
            }
            if (issequencing)
            {
                if (sequenceDir == "left")
                {
                    if (id == 2)
                    {
                        if (cframeSmooth <= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.33333); //cframe will increase at only one third of nomal rate; one third because screenCount = 3
                        }
                        else if (cframeSmooth >= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth += 2.0; //cframe will increase at three times of nomal rate
                            Console.WriteLine(id +" is increasing at 3 times, cf = " + cframeSmooth);
                        }
                    }
                    if (id == 1)
                    {
                        if (cframeSmooth >= sequenceStartF + 0.33333 * sequenceDuration && cframeSmooth <= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.66667); //cframe will increase at two thirds of nomal rate
                        }
                        else if (cframeSmooth >= sequenceStartF + sequenceDuration )
                        {
                            cframeSmooth += 1.0; //cframe will increase at two times of nomal rate
                            Console.WriteLine(id + " is increasing at 2 times, cf = " + cframeSmooth);
                        }
                    }
                }
                else if (sequenceDir == "right")
                {
                    if (id == 0)
                    {
                        if (cframeSmooth <= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.33333); 
                        }
                        else if (cframeSmooth >= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth += 2.0; 
                            Console.WriteLine(id + " is increasing at 3 times, cf = " + cframeSmooth);
                        }
                    }
                    if (id == 1 && cframe >= sequenceStartF + 0.33333 * sequenceDuration)
                    {
                        if (cframeSmooth >= sequenceStartF + 0.33333 * sequenceDuration && cframeSmooth <= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.66667); 
                        }
                        else if (cframeSmooth >= sequenceStartF + sequenceDuration)
                        {
                            cframeSmooth += 1.0;
                            Console.WriteLine(id + " is increasing at 2 times, cf = " + cframeSmooth);
                        }
                    }
                }
            }

            //double x = MediaWindow.Vframe_repository[cframe].mDirSmth.X;
            //double sequenceRate = 0.6;
            //if (x >= 0.5)
            //{
            //    //MediaWindow.Screens[0].cframeSmooth -= 2.0 * sequenceRate * x;
            //    //cframeSmooth -= sequenceRate * x;
            //    MediaWindow.Screens[0].cframeSmooth -= 1.0;
            //    cframeSmooth -= 0.5;
            //}
            //else if (x <= -0.5)
            //{
            //    MediaWindow.Screens[0].cframeSmooth += 1.0;
            //    cframeSmooth += 0.5;
            //}
            //else
            //{
            //    MediaWindow.Screens[0].cframeSmooth = cframe;
            //    MediaWindow.Screens[2].cframeSmooth = cframe;
            //}

            //if (id == 1 && cframe > 0)
            //{
            //    if (Math.Abs(MediaWindow.Vframe_repository[cframe].mDirSmth.X - MediaWindow.Vframe_repository[cframe-1].mDirSmth.X) > 0.4)
            //    {
            //        Console.WriteLine(cframe + " !!!direction changed!!!");
            //    }
            //}
        }

        void DoColor() {
            if (deviation > 0 && deviation < 40)    //in the beginning deviation = 0 so must have deviation > very small number
            {
                iscolor = true;
            }
        }

        static double ColorDist(byte[] px, RGBA_Quad quad)
        {
            return Math.Sqrt((quad.R - px[2]) * (quad.R - px[2])
                             + (quad.G - px[1]) * (quad.G - px[1])
                             + (quad.B - px[0]) * (quad.B - px[0]));
        }

        byte[, ,] RecreateColor(VFrame vf, double threshold, bool distinctColor)
        {
            byte[, ,] recreate_pix_data = new byte[MediaWindow.ry, MediaWindow.rx, 3];

            for (int j = 0; j < MediaWindow.ry; j++)
            {
                for (int i = 0; i < MediaWindow.rx; i++)
                {
                    double min = 442.0;
                    int minId = 0;
                    if (distinctColor)
                    {
                        for (int k = 0; k < vf.DiffColorMap.Count; k++)
                        {
                            byte[] px_d = { vf.pix_data[j, i, 0], vf.pix_data[j, i, 1], vf.pix_data[j, i, 2] };
                            double dist = ColorDist(px_d, vf.DiffColorMap[k]);
                            if (dist < threshold)
                            {
                                minId = k;
                                break;
                            }
                            else
                            {
                                if (dist < min)
                                {
                                    min = dist;
                                    minId = k;
                                }
                            }
                        }
                        recreate_pix_data[j, i, 2] = vf.DiffColorMap[minId].R;
                        recreate_pix_data[j, i, 1] = vf.DiffColorMap[minId].G;
                        recreate_pix_data[j, i, 0] = vf.DiffColorMap[minId].B;
                    }
                    else
                    {
                        for (int k = 0; k < vf.initialCMap.Count; k++)
                        {
                            byte[] px_d = { vf.pix_data[j, i, 0], vf.pix_data[j, i, 1], vf.pix_data[j, i, 2] };
                            double dist = ColorDist(px_d, vf.initialCMap[k]);
                            if (dist < threshold)
                            {
                                minId = k;
                                break;
                            }
                            else
                            {
                                if (dist < min)
                                {
                                    min = dist;
                                    minId = k;
                                }
                            }
                        }
                        recreate_pix_data[j, i, 2] = vf.initialCMap[minId].R;
                        recreate_pix_data[j, i, 1] = vf.initialCMap[minId].G;
                        recreate_pix_data[j, i, 0] = vf.initialCMap[minId].B;
                    }
                }
            }
            return recreate_pix_data;
        }

        public void FrameUpdate()
        {
            if (!ison) { return; }
            cframeSmooth += 1.0;
            cframe = (int) cframeSmooth; 

            if (iszooming)
            {
                pframe++;
            }
            if (cframe >= MediaWindow.Vframe_repository.Count)
            {
                cframe = 0;
                cframeSmooth = 0.0;
            }
            if (pframe >= MediaWindow.Vframe_repository.Count)
            {
                pframe = 0;
            }
        }

        public void OnTimeLapse(Vector3d gazeInput)
        {
            // if (!ison) { return; }
            // if (oncount > onduration) { ison = false; return; }
            bool islookedat = IsLookedAt(gazeInput);
            if (islookedat) {CalculateGazeProperty(gazeInput);}
            if (mode == Mode.zoom) DoZoom();
            if (mode == Mode.motion) DoMotion();
            if (mode == Mode.sequence) DoSequence();
            if (mode == Mode.pan) DoPan();
            if (mode == Mode.color) DoColor();
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
                //byte[, ,] px = MediaWindow.Vframe_repository[cframe].pix_data;
                byte[, ,] px;
                if (iscolor)    //in the beginning deviation = 0 so must have deviation > very small number
                {
                    //draw the color change 
                    px = RecreateColor(MediaWindow.Vframe_repository[cframe], threshold, false);
                    if (threshold <= 150)
                    {
                        threshold += 10;
                    }
                    //Colormap domiHueList = MediaWindow.Vframe_repository[cframe].DiffColorMap;

                    //limitedPalette;

                }
                else { px = MediaWindow.Vframe_repository[cframe].pix_data; }
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

            //else if (normal) { 

            //}

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
                    double s = (double)pancount / (double)panduration;
                    if (pandir == -1)
                    {
                        _tx0 = tx0 * (1.0 - s);
                        _tx1 = tx1 - s * tx0;
                    }
                    else
                    {
                        _tx0 = (1.0 - tx1) * s + tx0;
                        _tx1 = s + (1.0 - s) * tx1;
                    }
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