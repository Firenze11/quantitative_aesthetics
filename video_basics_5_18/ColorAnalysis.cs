using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.GPU;
using Emgu.CV.UI;
using C_sawapan_media;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace testmediasmall
{
    public class CvAnalysis
    {
        // grayscale image and corresponding property bins for openCV
        Image<Gray, byte> gray0;
        Image<Gray, byte> gray;
        Image<Bgr, byte> bgr;

        //RGBV and for each pixel their data
        Image<Gray, byte> imgR;
        Image<Gray, byte> imgG;
        Image<Gray, byte> imgB;
        Image<Gray, byte> imgR_withMask; //with masks
        Image<Gray, byte> imgG_withMask;
        Image<Gray, byte> imgB_withMask;
        byte[, ,] imgdata ;
        byte[, ,] imgdataR ;
        byte[, ,] imgdataG ;
        byte[, ,] imgdataB ;
        public byte[, ,] imgdataBGR;

        //histograms and their data
        public RGBHisto HistoR = new RGBHisto();
        public RGBHisto HistoG = new RGBHisto();
        public RGBHisto HistoB = new RGBHisto();
        public float[] HistoRA;
        public float[] HistoGA;
        public float[] HistoBA;

        //mask
        List<Image<Gray, byte>> masks = new List<Image<Gray, byte>>();
        public int maskNum = 6;
        int[] mask_pix_n = new int[5];

        //motion
        public Vector2d motionCentroid;
        public double[] motionMaskSum;
        public Vector2d[] motionMaskDir;
        double motionSum;

        //mask average rgb
        public float avgr = 0;
        public float avgg = 0;
        public float avgb = 0;
        public float avgr_index = 0;
        public float avgg_index = 0;
        public float avgb_index = 0;
        public List<float[]> maskAvgRGBColor = new List<float[]>();  //store rgb average for each mask
        StreamWriter histo_writer;

        void CreateMasks(int _rx, int _ry)
        {
            List<byte[, ,]> maskData = new List<byte[, ,]>();
            //........................................................mask to split screen areas
            for (int k = 0; k < maskNum; ++k)
            {
                byte[, ,] mdat = new byte[_ry, _rx, 1];
                maskData.Add(mdat);
            }
            //create mask:
            /* 
            |  --     3    --  |
            |   1  |  0  |  2  |
            |  --     4    --  |
            ** mask 5 is the whole screen
             */
            //j2 starts from the bottom left corner
            for (int j = 0; j < _ry; ++j)
            {
                int j2 = _ry - j - 1;
                for (int i = 0; i < _rx; ++i)
                {
                    if (j2 > (3 * _ry) / 4) maskData[3][j2, i, 0] = 255;
                    else maskData[3][j2, i, 0] = 0;

                    if (j2 >= _ry / 4 && j2 <= (3 * _ry) / 4)
                    {
                        if (i < (_rx / 4)) maskData[1][j2, i, 0] = 255;
                        else maskData[1][j2, i, 0] = 0;

                        if (i >= (_rx / 4) && i <= (_rx * 3) / 4) maskData[0][j2, i, 0] = 255;
                        else maskData[0][j2, i, 0] = 0;

                        if (i > (3 * _rx) / 4) maskData[2][j2, i, 0] = 255;
                        else maskData[2][j2, i, 0] = 0;
                    }

                    if (j2 < _ry / 4) maskData[4][j2, i, 0] = 255;
                    else maskData[4][j2, i, 0] = 0;

                    maskData[5][j2, i, 0] = 255;
                }
            }

            for (int k = 0; k < maskData.Count; ++k)
            {
                masks.Add(new Image<Gray, byte>(maskData[k]));
            }
        }

        void InitalizeCvImages(VideoPixel[,] _px, int _rx, int _ry)
        {
            if (imgdata == null)
            {
                imgdata = new byte[_ry, _rx, 1];
                imgdataR = new byte[_ry, _rx, 1];
                imgdataG = new byte[_ry, _rx, 1];
                imgdataB = new byte[_ry, _rx, 1];
                imgdataBGR = new byte[_ry, _rx, 3];
            }
            
            motionSum = 0;
            motionCentroid = new Vector2d();
            motionMaskSum = new double[5];
            motionMaskDir = new Vector2d[5];
            mask_pix_n = new int[5];

            //scan each pixel for rgb
            for (int j = 0; j < _ry; ++j)
            {
                int j2 = _ry - j - 1;
                for (int i = 0; i < _rx; ++i)
                {
                    int maskn = MediaWindow.maskN( (double)i / (double)_rx, (double)j / (double)_ry);

                    imgdata[j2, i, 0] = (byte)((_px[j, i].V * 255.0));
                    imgdataBGR[j2, i, 0] = (byte)((_px[j, i].B * 255.0));
                    imgdataBGR[j2, i, 1] = (byte)((_px[j, i].G * 255.0));
                    imgdataBGR[j2, i, 2] = (byte)((_px[j, i].R * 255.0));

                    imgdataR[j2, i, 0] = (byte)((_px[j, i].R * 255.0));
                    imgdataG[j2, i, 0] = (byte)((_px[j, i].G * 255.0));
                    imgdataB[j2, i, 0] = (byte)((_px[j, i].B * 255.0));

                    double px_motion = Math.Sqrt( _px[j, i].mx * _px[j, i].mx + _px[j, i].my * _px[j, i].my);
                    Vector2d px_motion_dir = new Vector2d(_px[j, i].mx, _px[j, i].my);
                    motionSum += px_motion;
                    motionCentroid += (new Vector2d(i, j)) * px_motion;
                    //motionMaskSum[maskn] += px_motion;
                    motionMaskDir[maskn] += px_motion_dir;
                    mask_pix_n[maskn]++;
                    
                    //need??
                    avgr += imgdataR[j2, i, 0] - (imgdataG[j2, i, 0] + imgdataB[j2, i, 0]) / 2;
                    avgg += imgdataG[j2, i, 0] - (imgdataR[j2, i, 0] + imgdataB[j2, i, 0]) / 2;
                    avgb += imgdataB[j2, i, 0] - (imgdataR[j2, i, 0] + imgdataG[j2, i, 0]) / 2;
                }
            }
            motionCentroid /= motionSum;
            for (int i = 0; i < 5; i++)
            {
                motionMaskDir[i] /= ((double)mask_pix_n[i]/1000.0);
                //Console.Write((int)motionMaskDir[i].X + ", " + (int)motionMaskDir[i].Y + ";;; ");
            }

            //creates Grayscale image for openCV
            if (gray == null)
            {
                gray = new Image<Gray, byte>(imgdata);
                bgr = new Image<Bgr, byte>(imgdataBGR);
                gray0 = new Image<Gray, byte>(imgdata);

                imgR = new Image<Gray, byte>(imgdataR);
                imgG = new Image<Gray, byte>(imgdataG);
                imgB = new Image<Gray, byte>(imgdataB);

                imgR_withMask = new Image<Gray, byte>(imgdataR).Copy();
                imgG_withMask = new Image<Gray, byte>(imgdataG).Copy();
                imgB_withMask = new Image<Gray, byte>(imgdataB).Copy();
            }
            else
            {
                gray0.Data = gray.Data;
                gray.Data = imgdata;
                bgr.Data = imgdataBGR;

                imgR.Data = imgdataR;
                imgG.Data = imgdataG;
                imgB.Data = imgdataB;

                for (int i = 0; i < masks.Count; i++)
                {
                    imgR_withMask = imgR.Copy(masks[i]);
                    imgG_withMask = imgG.Copy(masks[i]);
                    imgB_withMask = imgB.Copy(masks[i]);
                }
            }
        }

        void DrawHisto(int _rx, int _ry)
        {
            //render histogram on screen
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, HistoR.binNum, 0.0, (_rx / 4) * (_ry / 2) * masks.Count * 3, -1.0, 1.0);    //min bin size of 450 

            for (int j = 0; j < masks.Count; ++j)
            {
                //RGB histo for masks 0 - 4, 0 being at the top
                GL.Color4(1.0, 0.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoRA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoRA[i] + _rx * _rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(0.0, 1.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoGA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoGA[i] + _rx * _rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(0.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoBA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoBA[i] + _rx * _rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(1.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                /*for (int i = 0; i < HistSA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistSA[i] + rx * rx / (masks.Count) * m);
                }
                GL.End();
                */
            }
        }

        void CalculateHisto()
        {
            for (int j = 0; j < masks.Count; ++j)
            {
                histo_writer.Write("mask" + j + ",");
                HistoRA = HistoR.CalculateRGBHistogram(imgR, masks[j]);
                HistoGA = HistoG.CalculateRGBHistogram(imgG, masks[j]);
                HistoBA = HistoB.CalculateRGBHistogram(imgB, masks[j]);

                //get highest bin index
                avgr = HistoRA.Max();
                avgg = HistoGA.Max();
                avgb = HistoBA.Max();

                avgr_index = HistoRA.ToList().IndexOf(avgr);
                avgg_index = HistoGA.ToList().IndexOf(avgg);
                avgb_index = HistoBA.ToList().IndexOf(avgb);

                avgr_index = avgr_index - (avgg_index + avgb_index) / 2;
                avgg_index = avgg_index - (avgr_index + avgb_index) / 2;
                avgb_index = avgb_index - (avgg_index + avgr_index) / 2;

                float[] eachMaskRGBColor = new float[3] { avgr_index, avgg_index, avgb_index };
                maskAvgRGBColor.Add(eachMaskRGBColor);

                for (int k = 0; k < HistoR.binNum; k++)
                {
                    histo_writer.Write(
                    HistoRA[k] + "," +
                    HistoGA[k] + "," +
                    HistoBA[k] + ","
                    );
                }
            }
            histo_writer.WriteLine();
            //DrawHisto(rx, ry);
        }

        void CalculateMotion()
        {
            
        } 

        void CalculateMotion_NotUsed()
        {
            using (MemStorage storage = new MemStorage()) //create storage for motion components
            {
                MotionHistory _motionHistory;
                //update the motion history
                _motionHistory = new MotionHistory(
                1.0, //in second, the duration of motion history you wants to keep
                1.0, //in second, maxDelta for cvCalcMotionGradient
                0.001); //in second, minDelta for cvCalcMotionGradient

                _motionHistory.Update(gray);

                //#region get a copy of the motion mask and enhance its color
                //double[] minValues, maxValues;
                //Point[] minLoc, maxLoc;
                //_motionHistory.Mask.MinMax(out minValues, out maxValues, out minLoc, out maxLoc);
                //Image<Gray, Byte> motionMask = _motionHistory.Mask.Mul(255.0 / maxValues[0]);
                //#endregion

                //create the motion image 
                //display the motion pixels in blue (first channel)

                //Threshold to define a motion area, reduce the value to detect smaller motion
                double minArea = 100;

                storage.Clear(); //clear the storage
                Seq<MCvConnectedComp> motionComponents = _motionHistory.GetMotionComponents(storage);

                int count = 0;

                //iterate through each of the motion component
                foreach (MCvConnectedComp comp in motionComponents)
                {
                    count++;
                    //reject the components that have small area;
                    if (comp.area < minArea) continue;

                    // find the angle and motion pixel count of the specific area
                    double angle, motionPixelCount;
                    _motionHistory.MotionInfo(comp.rect, out angle, out motionPixelCount);

                    //reject the area that contains too few motion
                    if (motionPixelCount < comp.area * 0.1) continue;

                    //Draw each individual motion in red
                    //DrawMotion_NotUsed(bgr, comp.rect, angle, new Bgr(Color.Red));
                }
                Console.WriteLine("moving region count: " + count);

                // find and draw the overall motion angle
                double overallAngle, overallMotionPixelCount;
                _motionHistory.MotionInfo(gray.ROI, out overallAngle, out overallMotionPixelCount);

                //Display the amount of motions found on the current image
                //UpdateText(String.Format("Total Motions found: {0}; Motion Pixel count: {1}", motionComponents.Total, overallMotionPixelCount));
            }
        }

        static void DrawMotion_NotUsed(Image<Bgr, byte> image, Rectangle motionRegion, double angle, Bgr color)
        {
            float circleRadius = (motionRegion.Width + motionRegion.Height) >> 2;
            Point center = new Point(motionRegion.X + motionRegion.Width >> 1, motionRegion.Y + motionRegion.Height >> 1);

            CircleF circle = new CircleF(
               center,
               circleRadius);

            int xDirection = (int)(Math.Cos(angle * (Math.PI / 180.0)) * circleRadius);
            int yDirection = (int)(Math.Sin(angle * (Math.PI / 180.0)) * circleRadius);
            Point pointOnCircle = new Point(
                center.X + xDirection,
                center.Y - yDirection);
            LineSegment2D line = new LineSegment2D(center, pointOnCircle);

            image.Draw(circle, color, 1);
            image.Draw(line, color, 2);
        }

        void Init(VideoPixel[,] px, int rx, int ry)
        {
            if (histo_writer == null)
            {
                histo_writer = new StreamWriter(@"histo.csv");
                histo_writer.Flush();
            }
            if (masks.Count == 0) { CreateMasks(rx, ry); } 
            InitalizeCvImages(px, rx, ry);
        }

        public void FrameUpdate(VideoPixel[,] px, int rx, int ry)
        {
            Init(px, rx, ry);

            //avg rgb for the whole screen///needed?
            avgr = avgr / (rx * ry);
            avgg = avgg / (rx * ry);
            avgb = avgb / (rx * ry);

            //imgR.ROI = new System.Drawing.Rectangle(0, 0, 10, 10);
            //float[] HistRA = HistoR.CalculateRGBHistogram(imgR.Copy());
            //visualize histogram 

            CalculateHisto();
            CalculateMotion();

        }

    }



    public class RGBHisto
    {
        public int binNum = 10;
        public int range = 256;
        public DenseHistogram Histogram;

        public float[] CalculateRGBHistogram(Image<Gray, byte> imgRGB, Image<Gray, byte> mask)
        {
            Histogram = new DenseHistogram(binNum, new RangeF(0, 256));
            // imgRGB.ROI = new System.Drawing.Rectangle(0, 0, 20, 20);
            Histogram.Calculate(new Image<Gray, Byte>[] { imgRGB }, false, mask);
            float[] HistoRGBValues = new float[range];
            Histogram.MatND.ManagedArray.CopyTo(HistoRGBValues, 0);
            Histogram.Clear();
            return HistoRGBValues;
        }  
    }
}
