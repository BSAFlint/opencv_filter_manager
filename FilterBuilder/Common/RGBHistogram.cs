using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FilterBuilder.Common.MovingPredict;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{
    public class RGBHistogram
    {

        /// <summary>
        /// Возвращает гистограмму с shape [3,255]
        /// </summary>
        /// <param name="imgU8"></param>
        /// <returns></returns>
        public static double[,] GetHistograms(Mat imgU8)
        {
            int size = imgU8.Width * imgU8.Height * imgU8.NumberOfChannels;
            byte[] managedArray = new byte[size];
            Marshal.Copy(imgU8.DataPointer, managedArray, 0, size);  // imgU8.Data.GetValue(a,b);

            double[,] histograms = new double[imgU8.NumberOfChannels, 256];

            int nChannel = 0;
            foreach (var px in managedArray)
            {
                nChannel = (nChannel + 1) % imgU8.NumberOfChannels;
                histograms[nChannel, px] += 1;
            }

            return histograms;
        }

        public static void DrawBar(Mat img, int xLeft, int yBottom, int barIndex, double scaleX, double scaleY, double preValue, double currentVal, MCvScalar colorFill, MCvScalar color, bool isFill = false)
        {
            Point[] polyline = new Point[] {
                new Point( (int)(xLeft + barIndex*scaleX) , yBottom),
                new Point( (int)(xLeft + barIndex*scaleX) , (int)(yBottom - preValue*scaleY)),
                new Point( (int)(xLeft + (barIndex+1)*scaleX) , (int)(yBottom - currentVal*scaleY)),
                new Point( (int)(xLeft + (barIndex+1)*scaleX) , (int)(yBottom))
            };

            if (isFill)
                using (VectorOfPoint vp = new VectorOfPoint(polyline))
                using (VectorOfVectorOfPoint vvp = new VectorOfVectorOfPoint(vp))
                {
                    CvInvoke.FillPoly(img, vvp, colorFill);
                }

            CvInvoke.Line(img, polyline[1], polyline[2], color, 2);
        }

        public static void DrawChannel(Mat img, int xPos, int yPos, double scaleX, double scaleY, IMovingPredict movingPredict, double[,] histograms, int nChannel, MCvScalar colorFill, MCvScalar color, bool isFill = false)
        {
            movingPredict.Clear();
            double preValue = 0;
            for (int nColor = 0; nColor < 255; nColor++)
            {
                double currentValue = histograms[nChannel, nColor];
                currentValue = movingPredict.Predict(currentValue); // пропускаю currentValue через скользящую функцию
                DrawBar(img, xPos, yPos, nColor, scaleX, scaleY, preValue, currentValue, colorFill, color, isFill);
                preValue = currentValue;
            }
        }
    }
}
