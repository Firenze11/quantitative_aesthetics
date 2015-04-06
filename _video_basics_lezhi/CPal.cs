using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testmediasmall
{
    class CPal
    {

        const int sigbits = 5;
        const int rshift = 8 - sigbits;
        const int maxIterations = 1000;
        const double fractByPopulations = 0.75;

         // get reduced-space color index for a pixel
        int getColorIndex(int r, int g, int b) {
            return (r << (2 * sigbits)) + (g << sigbits) + b;
        }

  
    }
}
