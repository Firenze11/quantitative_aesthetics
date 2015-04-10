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
        //create grayscale image and corresponding property bins for openCV
        Image<Gray, byte> gray0;
        Image<Gray, byte> gray;

        Image<Gray, byte> imgR;
        Image<Gray, byte> imgG;
        Image<Gray, byte> imgB;

        Image<Bgr, byte> bgr;
        byte[, ,] imgdata;
        byte[, ,] imgdataR;
        byte[, ,] imgdataG;
        byte[, ,] imgdataB;
        byte[, ,] imgdataBGR;

        RGBHisto HistoR = new RGBHisto();
        RGBHisto HistoG = new RGBHisto();
        RGBHisto HistoB = new RGBHisto();
        RGBHisto HistoH = new RGBHisto();
        RGBHisto HistoS = new RGBHisto();
        RGBHisto HistoV = new RGBHisto();

        // byte[, ,] mask1data;
        // byte[, ,] mask2data;
        // Image<Gray, byte> mask1;
        // Image<Gray, byte> mask2;

        List<byte[, ,]> maskData = new List<byte[, ,]>();
        List<Image<Gray, byte>> masks = new List<Image<Gray, byte>>();

        StreamWriter histo_writer;

        public void Init(VideoPixel[,] px, int rx, int ry)
        {
            if (histo_writer == null)
            {
                histo_writer = new StreamWriter("C:/Users/anakano/Documents/Classes/GSD6432/Final_Project/quantitative_aesthetics/video_basics/histo.csv");
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
                for (int k = 0; k < 5; ++k)
                {
                    byte[, ,] mdat = new byte[ry, rx, 1];
                    maskData.Add(mdat);
                }

                //create mask:
                /* 
                |  --     3    --  |
                |   1  |  0  |  2  |
                |  --     4    --  |
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
                    }
                }

                for (int k = 0; k < maskData.Count; ++k)
                {
                    masks.Add(new Image<Gray, byte>(maskData[k]));
                }
            }

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
                }
            }

            if (gray == null)
            {
                gray = new Image<Gray, byte>(imgdata);
                bgr = new Image<Bgr, byte>(imgdataBGR);
                gray0 = new Image<Gray, byte>(imgdata);

                imgR = new Image<Gray, byte>(imgdataR);
                imgG = new Image<Gray, byte>(imgdataG);
                imgB = new Image<Gray, byte>(imgdataB);
            }
            else
            {
                gray0.Data = gray.Data;
                gray.Data = imgdata;
                bgr.Data = imgdataBGR;

                imgR.Data = imgdataR;
                imgG.Data = imgdataG;
                imgB.Data = imgdataB;
            }

            Image<Hsv, Byte> hsvImage = new Image<Hsv, Byte>(imgdata);
            Hsv hsvColour = hsvImage[0, 0];

            //extract the hue and saturation channels
            Image<Gray, Byte>[] channels = hsvImage.Split();
            Image<Gray, Byte> imgHue = channels[0];
            Image<Gray, Byte> imgSat = channels[1];
            Image<Gray, Byte> imgVal = channels[2];
        }

        public void FrameUpdate(VideoPixel[,] px, int rx, int ry)
        {
            Init(px, rx, ry);

            //imgR.ROI = new System.Drawing.Rectangle(0, 0, 10, 10);
            //float[] HistRA = HistoR.CalculateRGBHistogram(imgR.Copy());
            //visualize histogram 

            for (int m = 0; m < masks.Count; ++m)
            {
                histo_writer.Write("mask" + m + ",");
                float[] HistRA = HistoR.CalculateRGBHistogram(imgR, masks[m]);
                float[] HistGA = HistoG.CalculateRGBHistogram(imgG, masks[m]);
                float[] HistBA = HistoB.CalculateRGBHistogram(imgB, masks[m]);

                float[] HistSA = HistoH.CalculateRGBHistogram(imgHue, masks[m]);

                //render histogram on screen
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0.0, HistoR.binNum, 0.0, (rx / 4) * (ry / 2) * masks.Count * 3, -1.0, 1.0);    //min bin size of 450 

                //RGB histo for masks 0 - 4, 0 being at the top
                GL.Color4(1.0, 0.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistRA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistRA[i] + rx * rx / (masks.Count) * m);

                }
                GL.End();

                GL.Color4(0.0, 1.0, 0.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistGA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistGA[i] + rx * rx / (masks.Count) * m);
                }
                GL.End();

                GL.Color4(0.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistBA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistBA[i] + rx * rx / (masks.Count) * m);
                }
                GL.End();

                GL.Color4(1.0, 0.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < HistSA.Length; ++i)
                {
                    GL.Vertex2(i, 10.0 + HistSA[i] + rx * rx / (masks.Count) * m);
                }
                GL.End();

                for (int k = 0; k < HistoR.binNum; k++)
                {
                    histo_writer.Write(
                    HistRA[k] + "," +
                    HistGA[k] + "," +
                    HistBA[k] + ","
                    );
                }
            }

            histo_writer.WriteLine(); //move to next line for each frame update  
        }
    }

    public class RGBHisto
    {
        public int binNum = 10;
        public int range = 256;
        DenseHistogram Histogram;

        public float[] CalculateRGBHistogram(Image<Gray, byte> imgRGB, Image<Gray, byte> mask)
        {
            Histogram = new DenseHistogram(binNum, new RangeF(0, 256));
            // imgRGB.ROI = new System.Drawing.Rectangle(0, 0, 20, 20);
            Histogram.Calculate(new Image<Gray, Byte>[] { imgRGB }, false, mask);
            float[] HistRGBValues = new float[range];
            Histogram.MatND.ManagedArray.CopyTo(HistRGBValues, 0);
            Histogram.Clear();

            return HistRGBValues;
        }  
    }
}
