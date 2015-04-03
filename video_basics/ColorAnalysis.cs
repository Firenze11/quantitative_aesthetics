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

        StreamWriter histo_writer;

        public void FrameUpdate(VideoPixel[,] px, int rx, int ry) {

            if (histo_writer == null)
            {
                histo_writer = new StreamWriter("C:/Users/anakano/Documents/Classes/GSD6432/Final_Project/quantitative_aesthetics/video_basics/histo.csv"); 
            }
            if (imgdata == null)
            {
                imgdata = new byte[ry, rx, 1];
                imgdataR = new byte[ry, rx, 1];
                imgdataG = new byte[ry, rx, 1];
                imgdataB = new byte[ry, rx, 1];
                imgdataBGR = new byte[ry, rx, 3];
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

                    int max = Math.Max(imgdataR[j2, i, 0], Math.Max(imgdataG[j2, i, 0], imgdataB[j2, i, 0]));
                    int min = Math.Min(imgdataR[j2, i, 0], Math.Min(imgdataG[j2, i, 0], imgdataB[j2, i, 0]));

                    //int hue = (int)(imgdata.GetHue() * 256f / 360f);
                    int saturation = (max == 0) ? 0 : (int)(1.0 - (1.0 * min / max));
                    int value = (int)(max / 255.0);
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

            float[] HistRA = HistoR.CalculateRGBHistogram(imgR);
            float[] HistGA = HistoG.CalculateRGBHistogram(imgG);
            float[] HistBA = HistoB.CalculateRGBHistogram(imgB);

            //render histogram on screen
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, 255, 0.0, 255, -1.0, 1.0);

            GL.Color4(1.0, 0.0, 0.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < HistRA.Length; ++i)
            {
                GL.Vertex2(i, 10.0 + HistRA[i]);
            }
            GL.End();

            GL.Color4(0.0, 1.0, 0.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < HistGA.Length; ++i)
            {
                GL.Vertex2(i, 10.0 + HistGA[i]);
            }
            GL.End();

            GL.Color4(0.0, 0.0, 1.0, 1.0);
            GL.Begin(PrimitiveType.LineStrip);
            for (int i = 0; i < HistBA.Length; ++i)
            {
                GL.Vertex2(i, 10.0 + HistBA[i]);
            }
            GL.End();


      
            for (int k = 0; k < HistoR.binSize; k++)
            {
                histo_writer.Write(
                HistRA[k] + "," +
                HistGA[k] + "," +
                HistBA[k] + ","
                );            
            }
            histo_writer.WriteLine();

           
        }

        public void CalculateHSV(Image<Gray, byte> imgR, Image<Gray, byte> imgG, Image<Gray, byte> imgB)
        {
           /*int max = Math.Max(imgR, Math.Max(imgG, imgB));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = (int)(color.GetHue() * 256f / 360f);
            saturation = (max == 0) ? 0 : (int)(1d - (1d * min / max));
            value = (int)(max / 255d);
            */
        }
    }

    public class RGBHisto
    {
        public int binSize = 10;
        public int range = 256;
        DenseHistogram Histogram;
        
        public float[] CalculateRGBHistogram(Image<Gray, byte> imgRGB)
        {
            Histogram = new DenseHistogram(binSize, new RangeF(0, 256));
            Histogram.Calculate(new Image<Gray, Byte>[] { imgRGB }, true, null);
            float[] HistRGBValues = new float[range];
            Histogram.MatND.ManagedArray.CopyTo(HistRGBValues, 0);
            Histogram.Clear();

            return HistRGBValues;
        }
    }
}
