using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using C_sawapan_media;//////////

namespace testmediasmall
{
    //see Leptonica for original C code

    public struct RGBA_Quad
    {
        public RGBA_Quad(int r, int g, int b, int a, int n)
        {
            B = (byte)b;
            G = (byte)g;
            R = (byte)r;
            A = (byte)a;
            npixels = n;
        }
        public byte B;
        public byte G;
        public byte R;
        public byte A;
        public int npixels;
    }
       
    public class Colormap : List<RGBA_Quad>
    {
        public void Add(int r,int g,int b, int n)
        {
            Add(new RGBA_Quad(r,g,b,255, n));
        }
    }

    public class PIX
    {
        public PIX(VideoIN bmp)////////
        {
            w = bmp.ResX;//////////////
            h = bmp.ResY;//////////////////////

            data = new uint[w * h];

            int k=0;
            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    uint R = Convert.ToUInt32(bmp.Pixels[j, i].R * 255);
                    uint G = Convert.ToUInt32(bmp.Pixels[j, i].G * 255);
                    uint B = Convert.ToUInt32(bmp.Pixels[j, i].B * 255);
                    data[k++] = (R << 24) | (G << 16) | (B << 8);/////////
                }
            }
        }
        public PIX(VFrame bmp, int _w, int _h)////////
        {
            w = _w;//////////////
            h = _h;//////////////////////

            data = new uint[w * h];

            int k = 0;
            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    uint R = Convert.ToUInt32(bmp.frame_pix_data[j, i, 2]);////////
                    uint G = Convert.ToUInt32(bmp.frame_pix_data[j, i, 1]);////////
                    uint B = Convert.ToUInt32(bmp.frame_pix_data[j, i, 0]);////////
                    data[k++] = (R << 24) | (G << 16) | (B << 8);/////////
                }
            }
        }
        public int w; 
        public int h;
        public uint[] data;

        public L_Box3d GetColorRegion(int subsample)
        {
            uint    rmin, rmax, gmin, gmax, bmin, bmax, rval, gval, bval;
            uint   pixel;
            int line;
     
            rmin = gmin = bmin = 1000000;
            rmax = gmax = bmax = 0;


            for (int j = 0; j < h; j += subsample) {
                line = j * w;
                for (int i = 0; i < w; i += subsample) {
                    pixel = data[line+i];
                    rval = pixel >> (24 + ColorQuant.RSHIFT);
                    gval = (pixel >> (16 + ColorQuant.RSHIFT)) & ColorQuant.MASK;
                    bval = (pixel >> (8 + ColorQuant.RSHIFT)) & ColorQuant.MASK;

                    if (rval < rmin)  rmin = rval;
                    else if (rval > rmax)  rmax = rval;

                    if (gval < gmin)  gmin = gval;
                    else if (gval > gmax)   gmax = gval;

                    if (bval < bmin)   bmin = bval;
                    else if (bval > bmax)   bmax = bval;

                }
            }

            return new L_Box3d(rmin, rmax, gmin, gmax, bmin, bmax);
        }

/*------------------------------------------------------------------------*
 *                        Median cut indexed histogram                    *
 *------------------------------------------------------------------------*/
        /*!
         *  pixMedianCutHisto()
         *
         *      Input:  pixs  (32 bpp; rgb color)
         *              sigbits (valid: 5 or 6)
         *              subsample (integer > 0)
         *      Return: histo (1-d array, giving the number of pixels in
         *                     each quantized region of color space), or null on error
         *
         *  Notes:
         *      (1) Array is indexed by (3 * sigbits) bits.  The array size
         *          is 2^(3 * sigbits).
         *      (2) Indexing into the array from rgb uses red sigbits as
         *          most significant and blue as least.
         */
        public int[] GetHisto(int subsample)
        {
            if (subsample <= 0) return null;

            int histosize = 1 << (3 * ColorQuant.SIGBITS);
            int [] histo = new int[histosize];
            
            for (int i = 0; i < h; i += subsample)
            {
                for (int j = 0; j < w; j += subsample)
                {
                    int index = GetColorIndex(data[i * w + j]);
                    histo[index]++;
                }
            }

            return histo;
        }


        public static int GetColorIndex(uint pixel)
        {
            uint rval = pixel >> (24 + ColorQuant.RSHIFT);
            uint gval = (pixel >> (16 + ColorQuant.RSHIFT)) & ColorQuant.MASK;
            uint bval = (pixel >> (8 + ColorQuant.RSHIFT)) & ColorQuant.MASK;

            return (int)((rval << (2 * ColorQuant.SIGBITS)) + (gval << ColorQuant.SIGBITS) + bval);
        }

    }

    public class L_Box3d : IComparable<L_Box3d>
    {
        public L_Box3d(int _r1,int _r2,int _g1,int _g2,int _b1,int _b2)
        {
            r1 = _r1;
            r2 = _r2;
            g1 = _g1;
            g2 = _g2;
            b1 = _b1;
            b2 = _b2;
        }

        public L_Box3d(uint _r1,uint _r2,uint _g1,uint _g2,uint _b1,uint _b2)
        {
            r1 = (int)_r1;
            r2 = (int)_r2;
            g1 = (int)_g1;
            g2 = (int)_g2;
            b1 = (int)_b1;
            b2 = (int)_b2;
        }

        public double sortparam;   /* parameter on which to sort the vbox */
        public int npix;           /* number of pixels in the vbox        */
        public int vol;            /* quantized volume of vbox            */
        public int r1;             /* min r index in the vbox             */
        public int r2;             /* max r index in the vbox             */
        public int g1;             /* min g index in the vbox             */
        public int g2;             /* max g index in the vbox             */
        public int b1;             /* min b index in the vbox             */
        public int b2;             /* max b index in the vbox             */


        public L_Box3d Clone() {
            return new L_Box3d(r1,r2, g1, g2, b1, b2);
        }

        public int CompareTo(L_Box3d other)
        {
            /*if (this.sortparam < other.sortparam) return -1;
            else if (this.sortparam > other.sortparam) return 1;
            else return 0;*/

            if (this.sortparam < other.sortparam) return 1;
            else if (this.sortparam > other.sortparam) return -1;
            else return 0;
        }


        public int GetCount(int[] histo)
        {
            if (histo == null) return 0;

            int npix = 0;
            for (int i = r1; i <= r2; i++)
            {
                for (int j = g1; j <= g2; j++)
                {
                    for (int k = b1; k <= b2; k++)
                    {
                        int index = (i << (2 * ColorQuant.SIGBITS)) + (j << ColorQuant.SIGBITS) + k;
                        npix += histo[index];
                    }
                }
            }

            return npix;
        }


        /*!
         *  vboxGetVolume()
         *
         *      Input:  vbox (3d region of color space for one quantized color)
         *      Return: quantized volume of vbox, or 0 on error
         */
        public int GetVolume()
        {
            return ((r2 - r1 + 1) * (g2 - g1 + 1) * (b2 - b1 + 1));
        }

        
        /*!
         *  vboxGetAverageColor()
         *
         *      Input:  vbox (3d region of color space for one quantized color)
         *              histo
         *              sigbits (valid: 5 or 6)
         *              index (if >= 0, assign to all colors in histo in this vbox)
         *              &rval, &gval, &bval (<returned> average color)
         *      Return: cmap, or null on error
         *
         *  Notes:
         *      (1) The vbox represents one color in the colormap.
         *      (2) If index >= 0, as a side-effect, all array elements in
         *          the histo corresponding to the vbox are labeled with this
         *          cmap index for that vbox.  Otherwise, the histo array
         *          is not changed.
         *      (3) The vbox is quantized in sigbits.  So the actual 8-bit color
         *          components are found by multiplying the quantized value
         *          by either 4 or 8.  We must add 0.5 to the quantized index
         *          before multiplying to get the approximate 8-bit color in
         *          the center of the vbox; otherwise we get values on
         *          the lower corner.
         */
        public int GetAverageColor(int  []histo,int   index,ref int  prval, ref int  pgval,ref int  pbval)
        {
            int  i, j, k, ntot, mult, histoindex, rsum, gsum, bsum;


            if (histo==null) return 1;

            prval = pgval = pbval = 0;
            ntot = 0;
            mult = 1 << (8 - ColorQuant.SIGBITS);
            rsum = gsum = bsum = 0;
            for (i = r1; i <= r2; i++) {
                for (j = g1; j <= g2; j++) {
                    for (k = b1; k <= b2; k++) {
                        histoindex = (i << (2 * ColorQuant.SIGBITS)) + (j << ColorQuant.SIGBITS) + k;
                         ntot += histo[histoindex];
                         rsum += (int)(histo[histoindex] * (i + 0.5) * mult);
                         gsum += (int)(histo[histoindex] * (j + 0.5) * mult);
                         bsum += (int)(histo[histoindex] * (k + 0.5) * mult);
                         if (index >= 0)
                             histo[histoindex] = index;
                    }
                }
            }

            if (ntot == 0) {
                prval = mult * (r1 + r2 + 1) / 2;
                pgval = mult * (g1 + g2 + 1) / 2;
                pbval = mult * (b1 + b2 + 1) / 2;
            } else {
                prval = rsum / ntot;
                pgval = gsum / ntot;
                pbval = bsum / ntot;
            }

            return 0;
        }


    };

    public class ColorQuant
    {
        /* 5 significant bits for each component is generally satisfactory */
        public const int SIGBITS =5;
        public const int RSHIFT = 8 - SIGBITS;
        public const uint MASK = (uint)0xff >> RSHIFT;
        const int MAX_ITERS_ALLOWED = 5000;  /* prevents infinite looping */

        /* Specify fraction of vboxes made that are sorted on population alone.
         * The remaining vboxes are sorted on (population * vbox-volume).  */
        const double FRACT_BY_POPULATION = 0.85;

        /* To get the max value of 'dif' in the dithering color transfer,
         * divide DIF_CAP by 8. */
        const int DIF_CAP = 100;

        //-----------------------------------------------------------------------------sorting code
        public double[] TranslateHSV(RGBA_Quad rgba)
        {
            return HSV.hsv(rgba.R / 255.0, rgba.G / 255.0, rgba.B / 255.0);
        }
        /*public double ColorCharacteristic(RGBA_Quad rgba)
        {
            double[] chara = HSV.hsv(rgba.R / 255.0, rgba.G / 255.0, rgba.B / 255.0);
            return (100 * chara[0] * chara[0]) + chara[2] * chara[2];
        }*/
        public Colormap SortByHue (Colormap cmap)
        {
            List<RGBA_Quad> sortedmap = (List < RGBA_Quad >) cmap;
            for (int i = 0; i < sortedmap.Count - 1; i++) //swap sorting
            {
                sortedmap.Sort((x, y) => TranslateHSV(x)[0].CompareTo(TranslateHSV(y)[0]));
            }
            return (Colormap)sortedmap;
        }
        private double Difference(RGBA_Quad c, double[] _avg)
        {
            double diffH = Math.Abs(TranslateHSV(c)[0] - _avg[0]) * TranslateHSV(c)[1] * TranslateHSV(c)[2];
            double diffS = Math.Abs(TranslateHSV(c)[1] - _avg[1]);
            double diffV = Math.Abs(TranslateHSV(c)[2] - _avg[2]);
            return 10 * diffH * diffH;// +diffV * diffV;
        }
        public Colormap SortByDifference(Colormap cmap)
        {
            List<RGBA_Quad> sortedmap = cmap;
            double[] diffs = new double[sortedmap.Count];
            double[] avg = new double[3];

            for (int i = 0; i < sortedmap.Count - 1; i++)
            {
                avg[0] += TranslateHSV(cmap[i]) [0];
                avg[1] += TranslateHSV(cmap[i]) [1];
                avg[2] += TranslateHSV(cmap[i]) [2];
            }
            for (int i = 0; i < 3; i++)
            {
                avg[i] = (double)(avg[i] / sortedmap.Count);
            }

            sortedmap.Sort((x, y) => Difference(y,avg).CompareTo(Difference(x,avg)));
            return (Colormap) sortedmap;
        }
        //------------------------------------------------------------------------end of sorting code

        public Colormap MedianCutQuant(PIX pixs, int ditherflag)
        {
            return MedianCutQuantGeneral(pixs, ditherflag, 256, 1);
        }

        public Colormap MedianCutQuantGeneral(VideoIN pixs, int maxcolors)//////////////////////1st edit
        {
            return MedianCutQuantGeneral(new PIX(pixs), 1, maxcolors, 1);
        }

        public Colormap MedianCutQuantGeneral(VideoIN pixs, int ditherflag, int maxcolors, int maxsub)/////////1st edit
        {
            return MedianCutQuantGeneral(new PIX(pixs), ditherflag, maxcolors, maxsub);
        }

        public Colormap MedianCutQuantGeneral(VFrame pixs, int _w, int _h, int maxcolors)//////////////////////2nd Edit
        {
            return MedianCutQuantGeneral(new PIX(pixs,_w,_h), 1, maxcolors, 1);
        }

        public Colormap MedianCutQuantGeneral(PIX pixs,int ditherflag,int maxcolors, int maxsub)
        {
            int i, subsample, ncolors, niters, popcolors;
            L_Box3d vbox, vbox1, vbox2;
            PriorityQueue<L_Box3d> lh, lhs;
            Colormap cmap;


            if (pixs == null || maxcolors < 2) return null;

            if (maxsub <= 0) maxsub = 10;  /* default will prevail for 10^7 pixels or less */

            int w = (int)pixs.w;
            int h = (int)pixs.h;


            /* Compute the color space histogram.  Default sampling
             * is about 10^5 pixels.  */
            if (maxsub == 1)
            {
                subsample = 1;
            }
            else
            {
                subsample = (int)(Math.Sqrt((double)(w * h) / 100000.0));
                subsample = Math.Max(1, Math.Min(maxsub, subsample));
            }

            int [] histo = pixs.GetHisto(subsample);

            /* See if the number of quantized colors is less than maxcolors */
            ncolors = 0;
            bool smalln = true;
            for (i = 0; i < histo.Length; i++)
            {
                if (histo[i] != 0)
                    ncolors++;
                if (ncolors > maxcolors)
                {
                    smalln = false;
                    break;
                }
            }
            if (smalln)
            { 
                cmap = GenerateColorMapFromHisto(histo);
                return cmap;
            }

            /* Initial vbox: minimum region in colorspace occupied by pixels */
            if (ditherflag != 0 || subsample > 1)  /* use full color space */
                vbox = new L_Box3d(0, (1 << SIGBITS) - 1,
                                   0, (1 << SIGBITS) - 1,
                                   0, (1 << SIGBITS) - 1);
            else
                vbox = pixs.GetColorRegion(subsample);

            vbox.npix = vbox.GetCount(histo);
            vbox.vol = vbox.GetVolume();

            /* For a fraction 'popcolors' of the desired 'maxcolors',
             * generate median cuts based on population, putting
             * everything on a priority queue sorted by population. */
            lh = new PriorityQueue<L_Box3d>();// lheapCreate(0, L_SORT_DECREASING);
            lh.Enqueue(vbox);
            ncolors = 1;
            niters = 0;
            popcolors = (int)(FRACT_BY_POPULATION * maxcolors);
            while (true)
            {
                if (niters++ > MAX_ITERS_ALLOWED)
                {
                    //L_WARNING("infinite loop; perhaps too few pixels!\n", procName);
                    break;
                }

                vbox = lh.Dequeue();// (L_BOX3D*)lheapRemove(lh);
                if (vbox.GetCount(histo) == 0)
                { /* just put it back */
                    lh.Enqueue(vbox);
                    continue;
                }
                ApplyMedianCut(histo, vbox, out vbox1, out vbox2);
                if (vbox1 == null)
                {
                    //L_WARNING("vbox1 not defined; shouldn't happen!\n", procName);
                    break;
                }
                if (vbox1.vol > 1)
                    vbox1.sortparam = vbox1.npix;

                //FREE(vbox);
                vbox = null;

                lh.Enqueue(vbox1);

                if (vbox2 != null)
                {  /* vbox2 can be NULL */
                    if (vbox2.vol > 1)
                        vbox2.sortparam = vbox2.npix;
                    lh.Enqueue(vbox2);
                    ncolors++;
                }
                if (ncolors >= popcolors)
                    break;
                
            }

            /* Re-sort by the product of pixel occupancy times the size
             * in color space. */
            lhs = new PriorityQueue<L_Box3d>();// lheapCreate(0, L_SORT_DECREASING);
            while (lh.Count() != 0)
            {
                vbox = lh.Dequeue();
                vbox.sortparam = vbox.npix * vbox.vol;
                lhs.Enqueue(vbox);
            }
            lh = null;
            //lheapDestroy(&lh, TRUE);

            /* For the remaining (maxcolors - popcolors), generate the
             * median cuts using the (npix * vol) sorting. */
            while (true)
            {
                vbox = lhs.Dequeue();
                if (vbox.GetCount(histo) == 0)
                { /* just put it back */
                    lhs.Enqueue(vbox);
                    continue;
                }
                ApplyMedianCut(histo, vbox, out vbox1, out vbox2);
                if (vbox1 == null)
                {
                    //L_WARNING("vbox1 not defined; shouldn't happen!\n", procName);
                    break;
                }
                if (vbox1.vol > 1)
                    vbox1.sortparam = vbox1.npix * vbox1.vol;

                vbox = null;

                lhs.Enqueue(vbox1);
                if (vbox2 != null)
                {  /* vbox2 can be NULL */
                    if (vbox2.vol > 1)
                        vbox2.sortparam = vbox2.npix * vbox2.vol;
                    lhs.Enqueue(vbox2);
                    ncolors++;
                }
                if (ncolors >= maxcolors)
                    break;
                if (niters++ > MAX_ITERS_ALLOWED)
                {
                    //L_WARNING("infinite loop; perhaps too few pixels!\n", procName);
                    break;
                }
            }

            /* Re-sort by pixel occupancy.  This is not necessary,
             * but it makes a more useful listing.  */
            lh = new PriorityQueue<L_Box3d>();// lheapCreate(0, L_SORT_DECREASING);
            while (lhs.Count()!=0)
            {
                vbox = lhs.Dequeue();
                vbox.sortparam = vbox.npix;
                /*        vbox->sortparam = vbox->npix * vbox->vol; */
                lh.Enqueue(vbox);
            }

            lhs = null;
            //lheapDestroy(&lhs, TRUE);

            /* Generate colormap from median cuts and quantize pixd */
            cmap = GenerateColorMapFromMedianCuts(lh, histo);
            //if (outdepth == 0) {
            //     ncolors = pixcmapGetCount(cmap);
            //     if (ncolors <= 2)
            //         outdepth = 1;
            //     else if (ncolors <= 4)
            //         outdepth = 2;
            //     else if (ncolors <= 16)
            //         outdepth = 4;
            //     else
            //         outdepth = 8;
            // }
            // pixd = pixQuantizeWithColormap(pixs, ditherflag, outdepth, cmap,
            //                                histo, histosize, sigbits);

            //     /* Force darkest color to black if each component <= 4 */
            // pixcmapGetRankIntensity(cmap, 0.0, &index);
            // pixcmapGetColor(cmap, index, &rval, &gval, &bval);
            // if (rval < 5 && gval < 5 && bval < 5)
            //     pixcmapResetColor(cmap, index, 0, 0, 0);

            //     /* Force lightest color to white if each component >= 252 */
            // pixcmapGetRankIntensity(cmap, 1.0, &index);
            // pixcmapGetColor(cmap, index, &rval, &gval, &bval);
            // if (rval > 251 && gval > 251 && bval > 251)
            //     pixcmapResetColor(cmap, index, 255, 255, 255);

            // lheapDestroy(&lh, TRUE);
            // FREE(histo);
            return cmap;
        }


        
        /*!
         *  medianCutApply()
         *
         *      Input:  histo  (array; in rgb colorspace)
         *              sigbits
         *              vbox (input 3D box)
         *              &vbox1, vbox2 (<return> vbox split in two parts)
         *      Return: 0 if OK, 1 on error
         */
        int   ApplyMedianCut(int   []histo,   L_Box3d   vbox, out L_Box3d  pvbox1, out L_Box3d  pvbox2)
        {

            pvbox1=null;
            pvbox2=null;

            int   i, j, k, sum, rw, gw, bw, maxw, index;
            int   total, left, right;
            int   []partialsum=new int[128];
            //L_Box3d  vbox1, vbox2;

    
            if (histo==null || vbox==null) return 1;


            if (vbox.GetCount(histo) == 0) return 1; //  return ERROR_INT("no pixels in vbox", procName, 1);

                /* If the vbox occupies just one element in color space, it can't
                 * be split.  Leave the 'sortparam' field at 0, so that it goes to
                 * the tail of the priority queue and stays there, thereby avoiding
                 * an infinite loop (take off, put back on the head) if it
                 * happens to be the most populous box! */
            rw = vbox.r2 - vbox.r1 + 1;
            gw = vbox.g2 - vbox.g1 + 1;
            bw = vbox.b2 - vbox.b1 + 1;


            if (rw == 1 && gw == 1 && bw == 1) {
                pvbox1 = vbox.Clone();
                return 0;
            }

                /* Select the longest axis for splitting */
            maxw = Math.Max(bw, Math.Max(rw, gw));
        

                /* Find the partial sum arrays along the selected axis. */
            total = 0;
            if (maxw == rw) {
                for (i = vbox.r1; i <= vbox.r2; i++) {
                    sum = 0;
                    for (j = vbox.g1; j <= vbox.g2; j++) {
                        for (k = vbox.b1; k <= vbox.b2; k++) {
                            index = (i << (2 * SIGBITS)) + (j << SIGBITS) + k;
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else if (maxw == gw) {
                for (i = vbox.g1; i <= vbox.g2; i++) {
                    sum = 0;
                    for (j = vbox.r1; j <= vbox.r2; j++) {
                        for (k = vbox.b1; k <= vbox.b2; k++) {
                            index = (i << SIGBITS) + (j << (2 * SIGBITS)) + k;
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else {  /* maxw == bw */
                for (i = vbox.b1; i <= vbox.b2; i++) {
                    sum = 0;
                    for (j = vbox.r1; j <= vbox.r2; j++) {
                        for (k = vbox.g1; k <= vbox.g2; k++) {
                            index = i + (j << (2 * SIGBITS)) + (k << SIGBITS);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }

                /* Determine the cut planes, making sure that two vboxes
                 * are always produced.  Generate the two vboxes and compute
                 * the sum in each of them.  Choose the cut plane within
                 * the greater of the (left, right) sides of the bin in which
                 * the median pixel resides.  Here's the surprise: go halfway
                 * into that side.  By doing that, you technically move away
                 * from "median cut," but in the process a significant number
                 * of low-count vboxes are produced, allowing much better
                 * reproduction of low-count spot colors. */
            if (maxw == rw) {
                for (i = vbox.r1; i <= vbox.r2; i++) {
                    if (partialsum[i] > total / 2) {
                        pvbox1 = vbox.Clone();
                        pvbox2 = vbox.Clone();
                        left = i - vbox.r1;
                        right = vbox.r2 - i;
                        if (left <= right)
                            pvbox1.r2 = Math.Min(vbox.r2 - 1, i + right / 2);
                        else  /* left > right */
                            pvbox1.r2 = Math.Max(vbox.r1, i - 1 - left / 2);
                        pvbox2.r1 = pvbox1.r2 + 1;
                        break;
                    }
                }
            } else if (maxw == gw) {
                for (i = vbox.g1; i <= vbox.g2; i++) {
                    if (partialsum[i] > total / 2) {
                        pvbox1 = vbox.Clone();
                        pvbox2 = vbox.Clone();
                        left = i - vbox.g1;
                        right = vbox.g2 - i;
                        if (left <= right)
                            pvbox1.g2 = Math.Min(vbox.g2 - 1, i + right / 2);
                        else  /* left > right */
                            pvbox1.g2 = Math.Max(vbox.g1, i - 1 - left / 2);
                        pvbox2.g1 = pvbox1.g2 + 1;
                        break;
                    }
                }
            } else {  /* maxw == bw */
                for (i = vbox.b1; i <= vbox.b2; i++) {
                    if (partialsum[i] > total / 2) {
                        pvbox1 = vbox.Clone();
                        pvbox2 = vbox.Clone();
                        left = i - vbox.b1;
                        right = vbox.b2 - i;
                        if (left <= right)
                            pvbox1.b2 = Math.Min(vbox.b2 - 1, i + right / 2);
                        else  /* left > right */
                            pvbox1.b2 = Math.Max(vbox.b1, i - 1 - left / 2);
                        pvbox2.b1 = pvbox1.b2 + 1;
                        break;
                    }
                }
            }

            pvbox1.npix = pvbox1.GetCount(histo);
            pvbox2.npix = pvbox2.GetCount(histo);
            pvbox1.vol = pvbox1.GetVolume();
            pvbox2.vol = pvbox2.GetVolume();

            return 0;
        }


        /*!
         *  pixcmapGenerateFromMedianCuts()
         *
         *      Input:  lh (priority queue of pointers to vboxes)
         *              histo
         *              sigbits (valid: 5 or 6)
         *      Return: cmap, or null on error
         *
         *  Notes:
         *      (1) Each vbox in the heap represents a color in the colormap.
         *      (2) As a side-effect, the histo becomes an inverse colormap,
         *          where the part of the array correpsonding to each vbox
         *          is labeled with the cmap index for that vbox.  Then
         *          for each rgb pixel, the colormap index is found directly
         *          by mapping the rgb value to the histo array index.
         */
        Colormap GenerateColorMapFromMedianCuts(PriorityQueue<L_Box3d> lh, int[] histo)
        {
            int index, rval, gval, bval;
            L_Box3d vbox;
            Colormap cmap;


            if (lh == null || histo == null) return null;

            rval = gval = bval = 0;
            cmap = new Colormap();
            index = 0;
            while (lh.Count() > 0)
            {
                vbox = lh.Dequeue();
                if (vbox.npix < 1) continue;
                vbox.GetAverageColor(histo, index, ref rval, ref gval, ref bval);
                cmap.Add(rval, gval, bval, vbox.npix);                
                index++;
            }

            return cmap;
        }


        

         /*  Notes:
         *      (1) This is used when the number of colors in the histo
         *          is not greater than maxcolors.
         *      (2) As a side-effect, the histo becomes an inverse colormap,
         *          labeling the cmap indices for each existing color.
         */
        public static Colormap  GenerateColorMapFromHisto(int[] histo)
        {
            uint rval, gval, bval, i;

            if (histo == null) return null;

            /* Capture the rgb values of each occupied cube in the histo,
             * and re-label the histo value with the colormap index. */
            Colormap cmap = new Colormap();
            int index = 0;

            for (i = 0; i < histo.Length; i++)
            {
                if (histo[i] != 0)
                {
                    rval = (i >> (2 * SIGBITS)) << RSHIFT;
                    gval = ((i >> SIGBITS) & MASK) << RSHIFT;
                    bval = (i & MASK) << RSHIFT;
                    cmap.Add((int)rval, (int)gval, (int)bval, histo[i]);
                    histo[i] = index++;
                }
            }

            return cmap;
        }
    }


   
}
