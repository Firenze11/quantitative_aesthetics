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

        public enum Mode { zoom, sequence, pan, color, none}
        public Mode mode = Mode.color;

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
        Vector3d gazeVector;
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

        //zoom control
        public bool iszooming = false;
        int zoomcount = 0;
        static int zoomduration = 60;
        static double zoomrate = 0.01;

        //color control
        public bool iscolor = false;
        int colorcount = 0;
        static int colorduration = 10; 
        public int threshold = 0;
        double gazeRadius = 0.0;
        public bool ischoosingframe = false;

        //fade control
        public bool isfading = false;
        int fadecount = 0;
        static int fadeduration = 35;

        //sequence control
        static bool issequencing = false;
        public static double sequenceDurationEnlarge = 1.8;
        static double sequenceDuration;
        static double sequenceDurExtention = 0.6;
        static int sequenceStartF;
        static double sequenceTriggerDist = 40;
        static string sequenceDir;

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

        private void ChangeMode()
        {
            Array values = Enum.GetValues(typeof(Mode));
            Random random = new Random();
            //mode = (Mode)values.GetValue(random.Next(values.Length));
            mode = Mode.color;
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

            projGM = ProjectedGaze(MediaWindow.gazeMedium);
            deviation = MediaWindow.deviation;

            if (gazeL.Count >1)
                gazeVector = gazeL[gazeL.Count - 1] - gazeL[gazeL.Count - 2];
            else
                gazeVector = new Vector3d(0.0,0.0,0.0) ;
            //gaze color/////////////////////////////////NEED TO CONFIRM VBIT HAS CORRECT CONTENT!!
            gazeColor[0] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].R * 255.0);
            gazeColor[1] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].G * 255.0);
            gazeColor[2] = (byte)(vbit.Pixels[(int)projG.Y, (int)projG.X].B * 255.0);
            ///////////////////////////////////////////////////////////////////////gaze motion, gaze etc... too
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
                    iscolor = false;
                    //gazeRadius = 0.0;
                    ChangeMode();
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
                    iscolor = false;
                    fadecount = 0;
                }
                zoomcount++;
                if (isfading) { fadecount++; }
            }
            else if (deviation < 20 && deviation > 0.00000000001 && framecount > transitionInterval) //avoid zooming when there's no gaze data (dev = 0)
            {
                projF = projGM; //projected focus. unlike projG, projF remains stable during zooming process
                MediaWindow.Screens[next(id)].ison = true;
                iszooming = true;
                zoomcount = 0;
                pframe = cframe; ///Frame reassignments
                //DoColor();
                
                newFrame = MediaWindow.domiHueTransition(pframe, true);
                //newFrame = MediaWindow.maskAvgRGBTransition(cframe, num,gazeColor, true);
                Console.WriteLine(id + " cf = " + cframe + ", newframe = " + newFrame);
            }
            else
            {
                //DoColor();
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
                    ChangeMode();
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
        void DoSequence()
        {
            if (MediaWindow.gazeL.Count < 2) return;
            Vector3d p1 = MediaWindow.gazeL[MediaWindow.gazeL.Count - 1];
            Vector3d p0 = MediaWindow.gazeL[MediaWindow.gazeL.Count - 2];
            if (!issequencing)
            {
                if (Math.Abs(p1.X - p0.X) > sequenceTriggerDist && framecount > transitionInterval && (p1.X > 0.6 * MediaWindow.rx ||p1.X < 0.4 * MediaWindow.rx))
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
                    Console.WriteLine("sequenceDuration = " + sequenceDuration);
                }
                else { return; }
            }
            if (issequencing && cframeSmooth > sequenceStartF + (2.0 + sequenceDurExtention) * sequenceDuration || cframeSmooth > MediaWindow.Vframe_repository.Count - 2.0) 
            {
                if (sequenceDir == "right")
                {
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        MediaWindow.Screens[i].cframeSmooth = (double)MediaWindow.Screens[2].cframe; //in case 3 screens are not syncronized after sequencing period
                    }
                }
                else if (sequenceDir == "left")
                {
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        MediaWindow.Screens[i].cframeSmooth = (double)MediaWindow.Screens[0].cframe;
                    }
                }
                issequencing = false;
                framecount = 0;
                ChangeMode();
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
                        if (cframeSmooth <= sequenceStartF + 0.33333 * sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.33333); //cframe will increase at only one third of nomal rate; one third because screenCount = 3
                        }
                        else if (cframeSmooth <= sequenceStartF + (0.33333 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667;//cframe will increase at 1/3 times of nomal rate
                        }
                        else //if (cframeSmooth >= sequenceStartF + 0.33333 * sequenceDuration)
                        {
                            cframeSmooth += 0.66667; //cframe will increase at 5/3 times of nomal rate
                        }
                    }
                    if (id == 1)
                    {
                        if (cframeSmooth <= sequenceStartF + 0.66667 * sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.66667); //cframe will increase at two thirds of nomal rate
                        }
                        else if (cframeSmooth <= sequenceStartF + (0.66667 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667; 
                        }
                        else if (cframeSmooth >= sequenceStartF + (1.0 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth += 0.33333; //cframe will increase at 4/3 times of nomal rate
                        }
                    }
                    if (id == 0)
                    {
                        if (cframeSmooth >= sequenceStartF + sequenceDuration && cframeSmooth <= sequenceStartF + (1.0 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667; 
                        }
                    }
                }
                else if (sequenceDir == "right")
                {
                    if (id == 0)
                    {
                        if (cframeSmooth <= sequenceStartF + 0.33333 * sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.33333); 
                        }
                        else if (cframeSmooth <= sequenceStartF + (0.33333 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667;
                            Console.WriteLine(id + "middle freeze");
                        }
                        else //if (cframeSmooth >= sequenceStartF + 0.33333 * sequenceDuration)
                        {
                            cframeSmooth += 0.66667; 
                            Console.WriteLine(id + " is increasing at 5/3 times, cf = " + cframeSmooth);
                        }
                    }
                    if (id == 1 )
                    {
                        if (cframeSmooth <= sequenceStartF + 0.66667 * sequenceDuration)
                        {
                            cframeSmooth -= (1.0 - 0.66667); 
                        }
                        else if (cframeSmooth <= sequenceStartF + (0.66667 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667;
                            Console.WriteLine(id + "middle freeze");
                        }
                        else if (cframeSmooth >= (sequenceStartF + sequenceDurExtention) + sequenceDuration)
                        {
                            cframeSmooth += 0.33333;
                            Console.WriteLine(id + " is increasing at 4/3 times, cf = " + cframeSmooth);
                        }
                    }
                    if (id == 2)
                    {
                        if (cframeSmooth >= sequenceStartF + sequenceDuration && cframeSmooth <= sequenceStartF + (1.0 + sequenceDurExtention) * sequenceDuration)
                        {
                            cframeSmooth -= 0.66667;
                        }
                    }
                }
            }
        }

        void DoColor() 
        {
            if (iscolor)
            {
                //if (colorcount >= colorduration && threshold > 300)
                if (colorcount >= colorduration)
                {
                    for (int i = 0; i < MediaWindow.screenCount; i++)
                    {
                        MediaWindow.Screens[i].left = (double)i * (double)MediaWindow.rx / (double)MediaWindow.screenCount;
                        MediaWindow.Screens[i].bottom = 0.0;
                        MediaWindow.Screens[i].w = (double)MediaWindow.rx / (double)MediaWindow.screenCount;
                        MediaWindow.Screens[i].h = (double)MediaWindow.ry;

                        MediaWindow.Screens[i].tx0 = (double)i / (double)MediaWindow.screenCount;
                        MediaWindow.Screens[i].tx1 = ((double)i + 1.0) / (double)MediaWindow.screenCount;
                        MediaWindow.Screens[i].ty0 = 0.0;
                        MediaWindow.Screens[i].ty1 = 1.0;
                    }
                    MediaWindow.Screens[0].cframe = MediaWindow.Screens[0].newFrame;
                    MediaWindow.Screens[0].cframeSmooth = MediaWindow.Screens[0].newFrame;
                    MediaWindow.Screens[2].cframe = MediaWindow.Screens[2].newFrame;
                    MediaWindow.Screens[2].cframeSmooth = MediaWindow.Screens[2].newFrame;
                    //ChangeMode();
                    //Console.WriteLine("jumped " + id + " cf = " + cframe + ", msMode " + MediaWindow.multipleScreen);
                    gazeRadius = 0.0;
                    threshold = 0;
                    iscolor = false;
                    ischoosingframe = true;
                    colorcount = 0;
                    Console.WriteLine("1: " + MediaWindow.multipleScreen + " colorcount: " + colorcount);
                }
                // Gaze is not fixated
                if (deviation > 40)
                {
                    iscolor = false;
                    colorcount = 0;
                    gazeRadius = 0.0;
                    threshold = 0;
                    Console.WriteLine("2: " + MediaWindow.multipleScreen + " colorcount: " + colorcount);
                    //Console.WriteLine(id + " deviation > 40, cf = " + cframe);
                    //return;
                }
            }
            else if (ischoosingframe)
            {
                DoZoom();
                GL.ClearColor(0.0f, 0.6f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, MediaWindow.rx, 0.0, MediaWindow.ry, -1.0, 1.0);

                for (int i = 0; i < MediaWindow.screenCount; i++)
                {
                    double l = 0.0;
                    double b = 0.0;//0.5 * (double)ry * (1.0 - (1.0 / (double)screenCount));
                    double w = (double)MediaWindow.rx;
                    double h = (double)MediaWindow.ry; /// (double)screenCount;
                    Screen sc = new Screen(i, l, b, w, h);

                    sc.tx0 = 0.0;
                    sc.tx1 = 1.0;

                    sc.ty0 = 0.0;
                    sc.ty1 = 1.0;
                    MediaWindow.Screens.Add(sc);
                }
            }
            // Gaze is considered fixated after staring at a point for 30 frames
            else if (deviation > 0 && deviation < 40)
            {
                //projF = projGM;
                iscolor = true;
                colorcount = 0;
                threshold = 0;
                MediaWindow.Screens[0].newFrame = MediaWindow.maskAvgRGBTransition(cframe, num, gazeColor, false);
                MediaWindow.Screens[2].newFrame = MediaWindow.maskAvgRGBTransition(cframe, num, gazeColor, true);

                Console.WriteLine("3: " + MediaWindow.multipleScreen + " colorcount: " + colorcount);
                //newFrame = MediaWindow.domiHueTransition(cframe, true);
                //Console.WriteLine(id + "gaze focused, newframe = " + newFrame + ", msMode while color: " + MediaWindow.multipleScreen);
            }
            // First frame of video starts here
            else
            {
                MediaWindow.multipleScreen = false;
                Console.WriteLine("0: " + MediaWindow.multipleScreen + " colorcount: " + colorcount);
            }
            colorcount++;
            //pframe = cframe;
            //Console.WriteLine(id + " cf = " + cframe +", cc :" + colorcount + ", mcMode " + MediaWindow.multipleScreen);
        }

        static double ColorDist(byte[] px, RGBA_Quad quad)
        {
            return Math.Sqrt((quad.R - px[2]) * (quad.R - px[2])
                             + (quad.G - px[1]) * (quad.G - px[1])
                             + (quad.B - px[0]) * (quad.B - px[0]));
        }

        byte[, ,] RecreateScreenColor(VFrame vf, double threshold, bool distinctColor)
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

        byte[, ,] RecreateGazeColor(VFrame vf, double threshold, bool distinctColor)
        {
            byte[, ,] recreate_pix_data = new byte[MediaWindow.ry, MediaWindow.rx, 3];

            for (int j = 0; j < MediaWindow.ry; j++)
            {
                for (int i = 0; i < MediaWindow.rx; i++)
                {
                    double min = 442.0;
                    int minId = 0;
                    //if (deviation > 0 && deviation < 40)
                    {
                        if ((i - MediaWindow.dpoint.X) * (i - MediaWindow.dpoint.X) + (j - MediaWindow.dpoint.Y) * (j - MediaWindow.dpoint.Y) < gazeRadius * gazeRadius)
                        //if ((i - ActualGaze(projF).X) * (i - ActualGaze(projF).X) + (j - ActualGaze(projF).Y) * (j - ActualGaze(projF).Y) < gazeRadius * gazeRadius) //projF                    
                        //if (Math.Abs(i - MediaWindow.dpoint.X) < gazeRadius && Math.Abs(j - MediaWindow.dpoint.Y) < gazeRadius)   //square
                        {
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
                        else
                        {
                            recreate_pix_data[j, i, 2] = vf.pix_data[j, i, 2];
                            recreate_pix_data[j, i, 1] = vf.pix_data[j, i, 1];
                            recreate_pix_data[j, i, 0] = vf.pix_data[j, i, 0];
                        }
                    }
                }
            }
            return recreate_pix_data;
        }

        byte[, ,] RecreateGazeColorInward(VFrame vf, double threshold)
        {
            
byte[, ,] recreate_pix_data = new byte[MediaWindow.ry, MediaWindow.rx, 3];

            for (int j = 0; j < MediaWindow.ry; j++)
            {
                for (int i = 0; i < MediaWindow.rx; i++)
                {
                    double min = 442.0;
                    int minId = 0;

                    if ((i - MediaWindow.dpoint.X) * (i - MediaWindow.dpoint.X) + (j - MediaWindow.dpoint.Y) * (j - MediaWindow.dpoint.Y) < gazeRadius * gazeRadius)
                    //if ((i - ActualGaze(projF).X) * (i - ActualGaze(projF).X) + (j - ActualGaze(projF).Y) * (j - ActualGaze(projF).Y) < gazeRadius * gazeRadius) //projF                    
                    //if (Math.Abs(i - MediaWindow.dpoint.X) < gazeRadius && Math.Abs(j - MediaWindow.dpoint.Y) < gazeRadius)   //square
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
                        recreate_pix_data[j, i, 2] = vf.pix_data[j, i, 2];
                        recreate_pix_data[j, i, 1] = vf.pix_data[j, i, 1];
                        recreate_pix_data[j, i, 0] = vf.pix_data[j, i, 0];
                    }
                    else
                    {
                        recreate_pix_data[j, i, 2] = vf.DiffColorMap[minId].R;
                        recreate_pix_data[j, i, 1] = vf.DiffColorMap[minId].G;
                        recreate_pix_data[j, i, 0] = vf.DiffColorMap[minId].B;
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
            if (mode == Mode.sequence) DoSequence();
            if (mode == Mode.pan) DoPan();
            if (mode == Mode.color) DoColor();
            //////////////////////////////////////////////////////////////////////////////////try other things too
        }

        public void DrawVbit()
        {
            // if (!ison) { return; }
            double _tx0, _tx1, _ty0, _ty1, a;
            a = 1.0;
            if (!iszooming || (iszooming && isfading))
            {
                //byte[, ,] px = MediaWindow.Vframe_repository[cframe].pix_data;
                byte[, ,] px;
                if (iscolor)    //in the beginning deviation = 0 so must have deviation > very small number
                {
                    //..............................................for using projF,the static point
                    Vector3d ag = ActualGaze(projF);
                    //double s = 1.0 + zoomrate * zoomcount * zoomcount * zoomcount * zoomcount / 10000;
                    double s = 1.0;
                    _tx0 = ((s - 1.0) * projF.X / _rx + tx0) / s;
                    _tx1 = ((s - 1.0) * projF.X / _rx + tx1) / s;
                    _ty0 = ((s - 1.0) * projF.Y / _ry + ty0) / s;
                    _ty1 = ((s - 1.0) * projF.Y / _ry + ty1) / s;
                    
                    //...............................................end for using projF
                    //draw the color change 
                    //px = RecreateScreenColor(MediaWindow.Vframe_repository[cframe], threshold, false);
                    px = RecreateGazeColor(MediaWindow.Vframe_repository[cframe], threshold, true);
                    
                    //if (threshold <= 500)
                    //{
                        threshold += 2 * (int) Math.Sqrt(colorcount);
                    //}
                    gazeRadius += 1.0 * colorcount* colorcount;
                }
                else if (ischoosingframe)
                {
                    double s = 2.0;
                    _tx0 = ((s - 1.0) * projF.X / _rx + tx0) / s;
                    _tx1 = ((s - 1.0) * projF.X / _rx + tx1) / s;
                    _ty0 = ((s - 1.0) * projF.Y / _ry + ty0) / s;
                    _ty1 = ((s - 1.0) * projF.Y / _ry + ty1) / s;
                    px = RecreateScreenColor(MediaWindow.Vframe_repository[cframe], threshold, true);
                }
                else 
                { 
                    px = MediaWindow.Vframe_repository[cframe].pix_data;
                    if (issequencing)
                    {
                        if (id != 0)
                        {
                            if (MediaWindow.multipleScreen)
                            { a = 1.0; }
                            else { a = 0.34; }
                        }
                        //if (sequenceDir == "left")
                        //{
                        //    if (id != 0)
                        //    {
                        //        a = 0.3;
                        //    }
                        //}
                        //else
                        //{
                        //    if (id != 2)
                        //    {
                        //        a = 0.3;
                        //    }
                        //}
                    }
                }
                vbit.FromFrame(px);
                vbit.Update();
                //vbit.Draw(x0, y0, wd, ht, 1.0);

                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, vbit.texid);
                GL.Color4(1.0, 1.0, 1.0, a);
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

                if (isfading) { a = Math.Min(1.0, Math.Max(0, 1.0 - (double)(0.25 * fadecount * fadecount) / ((double)fadeduration))); }
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