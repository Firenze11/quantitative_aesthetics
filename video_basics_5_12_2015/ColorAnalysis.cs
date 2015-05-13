using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class ColorAnalysis
    {
        // grayscale image and corresponding property bins for openCV
        Image<Gray, byte> gray0;
        Image<Gray, byte> gray;

        Image<Gray, byte> imgR;
        Image<Gray, byte> imgG;
        Image<Gray, byte> imgB;

        Image<Gray, byte> imgR_withMask;
        Image<Gray, byte> imgG_withMask;
        Image<Gray, byte> imgB_withMask;
        List<Image<Gray,byte>> imgR_masks;
        List<Image<Gray, byte>> imgG_masks;
        List<Image<Gray, byte>> imgB_masks;

        //RGB and V values for each pixel 
        Image<Bgr, byte> bgr;
        byte[, ,] imgdata;
        byte[, ,] imgdataR;
        byte[, ,] imgdataG;
        byte[, ,] imgdataB;
        public byte[, ,] imgdataBGR;

        public RGBHisto HistoR = new RGBHisto();
        public RGBHisto HistoG = new RGBHisto();
        public RGBHisto HistoB = new RGBHisto();

        //histogram values for each mask
        public float[] HistoRA;
        public float[] HistoGA;
        public float[] HistoBA;

        public float avgr = 0;
        public float avgg = 0;
        public float avgb = 0;

        public float avgr_index = 0;
        public float avgg_index = 0;
        public float avgb_index = 0;
        public List<float[]> maskAvgRGBColor = new List<float[]>();  //store rgb average for each mask

        //public byte[] HistoRA_byte;
        //public byte[] HistoGA_byte;
        //public byte[] HistoBA_byte;

        // byte[, ,] mask1data;
        // byte[, ,] mask2data;
        // Image<Gray, byte> mask1;
        // Image<Gray, byte> mask2;

        //mask
        List<byte[, ,]> maskData = new List<byte[, ,]>();
        List<Image<Gray, byte>> masks = new List<Image<Gray, byte>>();
        public int maskNum = 6;

        StreamWriter histo_writer;

        public void Init(VideoPixel[,] px, int rx, int ry)
        {
            if (histo_writer == null)
            {
                histo_writer = new StreamWriter(@"histo.csv");
                histo_writer.Flush();
            }
            if (imgdata == null)
            {
                imgdata = new byte[ry, rx, 1];
                imgdataR = new byte[ry, rx, 1];
                imgdataG = new byte[ry, rx, 1];
                imgdataB = new byte[ry, rx, 1];

                imgdataBGR = new byte[ry, rx, 3];
                //........................................................mask to split screen areas
                for (int k = 0; k < maskNum; ++k)
                {
                    byte[, ,] mdat = new byte[ry, rx, 1];
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
                for (int j = 0; j < ry; ++j)
                {
                    int j2 = ry - j - 1;
                    for (int i = 0; i < rx; ++i)
                    {
                        if (j2 > (3 * ry) / 4) maskData[3][j2, i, 0] = 255;
                        else maskData[3][j2, i, 0] = 0;

                        if (j2 >= ry / 4 && j2 <= (3 * ry) / 4)
                        {
                            if (i < (rx / 4)) maskData[1][j2, i, 0] = 255;
                            else maskData[1][j2, i, 0] = 0;

                            if (i >= (rx / 4) && i <= (rx * 3) / 4) maskData[0][j2, i, 0] = 255;
                            else maskData[0][j2, i, 0] = 0;

                            if (i > (3 * rx) / 4) maskData[2][j2, i, 0] = 255;
                            else maskData[2][j2, i, 0] = 0;
                        }

                        if (j2 < ry / 4) maskData[4][j2, i, 0] = 255;
                        else maskData[4][j2, i, 0] = 0;

                        maskData[5][j2, i, 0] = 255;
                    }
                }

                for (int k = 0; k < maskData.Count; ++k)
                {
                    masks.Add(new Image<Gray, byte>(maskData[k]));
                }
            }

            
            //scan each pixel for rgb
            for (int j = 0; j < ry; ++j)
            {
                int j2 = ry - j - 1;
                for (int i = 0; i < rx; ++i)
                {
                    imgdata[j2, i, 0] = (byte)((px[j, i].V * 255.0));
                    imgdataBGR[j2, i, 0] = (byte)((px[j, i].B * 255.0));
                    imgdataBGR[j2, i, 1] = (byte)((px[j, i].G * 255.0));
                    imgdataBGR[j2, i, 2] = (byte)((px[j, i].R * 255.0));

                    imgdataR[j2, i, 0] = (byte)((px[j, i].R * 255.0));
                    imgdataG[j2, i, 0] = (byte)((px[j, i].G * 255.0));
                    imgdataB[j2, i, 0] = (byte)((px[j, i].B * 255.0));

                    avgr += imgdataR[j2, i, 0] - (imgdataG[j2, i, 0] + imgdataB[j2, i, 0]) / 2;
                    avgg += imgdataG[j2, i, 0] - (imgdataR[j2, i, 0] + imgdataB[j2, i, 0]) / 2;
                    avgb += imgdataB[j2, i, 0] - (imgdataR[j2, i, 0] + imgdataG[j2, i, 0]) / 2;
                }
            }
            //avg rgb for the whole screen
            avgr = avgr / (rx * ry);
            avgg = avgg / (rx * ry);
            avgb = avgb / (rx * ry);

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

                //imgR_masks = new List<Image<Gray, byte>>();
                //imgG_masks = new List<Image<Gray, byte>>();
                //imgB_masks = new List<Image<Gray, byte>>();
            }
            else
            {
                gray0.Data = gray.Data;
                gray.Data = imgdata;
                bgr.Data = imgdataBGR;

                imgR.Data = imgdataR;
                imgG.Data = imgdataG;
                imgB.Data = imgdataB;
                ///////////////////////////////////////////////////////////////////////////////////////////////////
                for(int i = 0; i < masks.Count; i++){
                    imgR_withMask = imgR.Copy(masks[i]);
                    imgG_withMask = imgG.Copy(masks[i]);
                    imgB_withMask= imgB.Copy(masks[i]);

                    //imgR_masks.Add(imgR_withMask);
                    //imgG_masks.Add(imgR_withMask);
                    //imgB_masks.Add(imgR_withMask);
                }
            }

            //.................................................................................HSV stuff
            //Image<Hsv, Byte> hsvImage = new Image<Hsv, Byte>(imgdata);
            //Hsv hsvColour = hsvImage[0, 0];

            ////extract the hue and saturation channels
            //Image<Gray, Byte>[] channels = hsvImage.Split();
            //Image<Gray, Byte> imgHue = channels[0];
            //Image<Gray, Byte> imgSat = channels[1];
            //Image<Gray, Byte> imgVal = channels[2];
            //................................................................................end HSV stuff
        }

        public void FrameUpdate(VideoPixel[,] px, int rx, int ry)
        {
            Init(px, rx, ry);

            //imgR.ROI = new System.Drawing.Rectangle(0, 0, 10, 10);
            //float[] HistRA = HistoR.CalculateRGBHistogram(imgR.Copy());
            //visualize histogram 

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

                float[] eachMaskRGBColor = new float[3] {avgr_index, avgg_index, avgb_index};
                Console.WriteLine("eachMaskRGBColor: " + eachMaskRGBColor[0] 
                    +", " + eachMaskRGBColor[1]
                    + ", " + eachMaskRGBColor[2]
                    );
                maskAvgRGBColor.Add(eachMaskRGBColor);
                //float[] HistSA = HistoH.CalculateRGBHistogram(imgHue, masks[m]);

                //render histogram on screen
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, HistoR.binNum, 0.0, (rx / 4) * (ry / 2) * masks.Count * 3, -1.0, 1.0);    //min bin size of 450 

                //RGB histo for masks 0 - 4, 0 being at the top
                GL.Color4(1.0, 0.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoRA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoRA[i] + rx * rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(0.0, 1.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoGA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoGA[i] + rx * rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(0.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistoBA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistoBA[i] + rx * rx / (masks.Count) * j);
                }
                GL.End();

                GL.Color4(1.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                /*for (int i = 0; i < HistSA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistSA[i] + rx * rx / (masks.Count) * m);
                }
                 */
                GL.End();

                for (int k = 0; k < HistoR.binNum; k++)
                {
                    histo_writer.Write(
                    HistoRA[k] + "," +
                    HistoGA[k] + "," +
                    HistoBA[k] + ","
                    );
                }
            }
            //Console.WriteLine(avgb_index);
            //histo_writer.Write(avgb_index);
            histo_writer.WriteLine(); //move to next line for each frame update  

            /*
            //convert float to bytes for histogram 
            foreach (float histoVal in HistoRA)
            {
                HistoRA_byte = BitConverter.GetBytes(histoVal);
            }
            foreach (float histoVal in HistoGA)
            {
                HistoGA_byte = BitConverter.GetBytes(histoVal);
            }
            foreach (float histoVal in HistoBA)
            {
                HistoBA_byte = BitConverter.GetBytes(histoVal);
            }
            */
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
