using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{

    public class CvLUT
    {
        public enum LutType
        {
            NULL = -1,
            LINE = 0,
            INVERSE = 1,
            SIN = 2,
            COS = 3,
            TAN_pi4 = 4,
            TAN_3pi8 = 5,
            POLY = 6
        }
        private LutType m_type;
        private Mat m_lookUpTable = null;

        private double[] m_polyFactors = new double[2] { 1, 1 };


        // polyFactors[0] + polyFactors[1]*x + polyFactors[2]*x*x+...
        private static double polynomResult(double x, double[] polyFactors)
        {
            double poliSum = 0;
            double nPow = 1;
            for (int p = 0; p < polyFactors.Length; p++)
            {
                poliSum += polyFactors[p] * nPow;
                nPow *= x;
            }
            return poliSum;
        }


        /// <summary>
        /// Возвращает карту перехода цветов
        /// </summary>
        /// <param name="type"></param>
        /// <param name="polyFactors">Только для LutType.POLY</param>
        /// <returns></returns>
        public static Mat GetTable(LutType type, double[] polyFactors = null)
        {
            byte[] tableVals = new byte[256];

            switch (type)
            {
                case LutType.LINE:
                    for (int n = 0; n < tableVals.Length; n++)
                        tableVals[n] = (byte)n;
                    break;

                case LutType.INVERSE:
                    for (int n = 0; n < tableVals.Length; n++)
                        tableVals[n] = (byte)(255 - n);
                    break;

                case LutType.SIN:
                    for (int n = 0; n < tableVals.Length; n++) {
                        double rad = (n / 255.0) * (Math.PI / 2.0);                        
                        tableVals[n] = (byte)(255 * Math.Sin(rad));
                        }
                    break;

                case LutType.COS:
                    for (int n = 0; n < tableVals.Length; n++)
                    {
                        double rad = (n / 255.0) * (Math.PI / 2.0);
                        tableVals[n] = (byte)(255 * Math.Cos(rad));
                    }
                    break;

                case LutType.TAN_pi4:
                    for (int n = 0; n < tableVals.Length; n++)
                    {
                        double rng = (Math.PI / 4.0);
                        double rad = ((n / 255.0) * (2* rng)) - rng;
                        tableVals[n] = (byte)(127 * Math.Tan(rad) + 128);
                    }
                    break;

                case LutType.TAN_3pi8:
                    {
                        double rng = (3 * Math.PI / 8.0);
                        double tanmax = Math.Tan(rng);

                        for (int n = 0; n < tableVals.Length; n++)
                        {
                            double rad = ((n / 255.0) * (2 * rng)) - rng;
                            tableVals[n] = (byte)(127 * (Math.Tan(rad) / tanmax) + 128);
                        }
                    }
                    break;

                case LutType.POLY:
                    for (int n = 0; n < tableVals.Length; n++)
                        tableVals[n] = (byte)polynomResult(n, polyFactors);
                    break;
            }


            return OpenCVHelper.GetMat(tableVals);
        }

        public LutType Type
        {
            get { return m_type; }
            set
            {
                m_type = value;
                if (m_lookUpTable != null)
                    m_lookUpTable.Dispose();

                m_lookUpTable = GetTable(m_type, m_polyFactors);
            }
        }

        public double[] PolyFactors
        {
            get { return m_polyFactors; }
            set { m_polyFactors = value; Type = LutType.POLY;}
        }
        public CvLUT()
        {
            if (Type == LutType.NULL)
                Type = LutType.LINE;
        }

        public Mat CreateImg(Mat img)
        {
            Mat dst = new Mat(img.Size, DepthType.Cv8U, img.NumberOfChannels);
            CvInvoke.LUT(img, m_lookUpTable, dst);
            return dst;
        }

        public static Mat CreateBGRLut(Mat img, CvLUT[] LUTS)
        {
            Mat dst = new Mat(img.Size, DepthType.Cv8U, img.NumberOfChannels);
            VectorOfMat brgChannels = new VectorOfMat();
            CvInvoke.Split(img, brgChannels);

            Mat[] mats = new Mat[brgChannels.Size];
            for (int n = 0; n < brgChannels.Size; n++)
                mats[n] = LUTS[n].CreateImg(brgChannels[n]);

            VectorOfMat lutsChannels = new VectorOfMat(mats);
            CvInvoke.Merge(lutsChannels, dst);

            return dst;
        }
    }
}
