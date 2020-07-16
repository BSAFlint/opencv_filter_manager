using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{
    public class OpenCVHelper
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public static Mat GetMat(double[,] table, int rows, int Columns)
        {
            double[] d = new double[table.Length];
            int id = 0;
            foreach (var el in table)
                d[id++] = el;

            int size = sizeof(double) * d.Length;
            IntPtr ptrTable = Marshal.AllocCoTaskMem(size);
            Marshal.Copy(d, 0, ptrTable, d.Length);
            Mat ans = new Mat(rows, Columns, DepthType.Cv64F, 1);
            CopyMemory(ans.DataPointer, ptrTable, (uint)size);
            Marshal.FreeCoTaskMem(ptrTable);
            return ans;
        }

        public static Mat GetMat(byte[] table, bool bRow = true)
        {
            Mat ans = null;
            if (bRow)
                ans = new Mat(table.Length, 1, DepthType.Cv8U, 1);
            else
                ans = new Mat(1, table.Length, DepthType.Cv8U, 1);
            Marshal.Copy(table, 0, ans.DataPointer, table.Length);
            return ans;
        }

        public static Mat GetCvMatFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            var Depth = Bitmap.GetPixelFormatSize(bitmap.PixelFormat);
            uint size = (uint)(bitmap.Height * bitmap.Width * (Depth / 8));

            Mat ans = null;
            switch (Depth)
            {
                case 8: ans = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 1); break;
                case 24: ans = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 3); break;
                case 32: ans = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 4); break;
            }

            if (ans == null)
                return ans;

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            CopyMemory(ans.DataPointer, bmpData.Scan0, size);
            bitmap.UnlockBits(bmpData);
            return ans;
        }
        public static Bitmap GetRGBBitmapFromCvMat(Mat srcMat)
        {
            if (srcMat == null)
                return null;

            Mat srcMatCl = new Mat();

            try
            {
                switch (srcMat.NumberOfChannels)
                {
                    case 1: CvInvoke.CvtColor(srcMat, srcMatCl, ColorConversion.Bgr2Bgra); break;
                    case 4: CvInvoke.CvtColor(srcMat, srcMatCl, ColorConversion.Bgr2Bgra); break;
                    case 3: CvInvoke.CvtColor(srcMat, srcMatCl, ColorConversion.Bgr2Bgra); break;
                    default: srcMatCl = srcMat; break;
                }

                Mat rgbMat = srcMatCl;
                srcMatCl.ConvertTo(rgbMat, DepthType.Cv8U);

                uint size = (uint)(rgbMat.Height * rgbMat.Width * 4);

                Bitmap bitmap = new Bitmap(rgbMat.Width, rgbMat.Height, PixelFormat.Format32bppArgb);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                IntPtr ptrBmp = bmpData.Scan0;
                CopyMemory(ptrBmp, rgbMat.DataPointer, size);
                bitmap.UnlockBits(bmpData);

                return bitmap;
            }
            catch { }

            return null;
        }


        static public double GetPixelSum(Mat img)
        {
            int size = img.Width * img.Height * img.NumberOfChannels;

            byte[] managedArray = new byte[size];
            Marshal.Copy(img.DataPointer, managedArray, 0, size);

            double fSum = 0;
            foreach (var px in managedArray)
                fSum += px;
            return fSum;
        }


        static public Point GetXYPixelSumCenter(Mat img, ReduceType reduceType = ReduceType.ReduceSum)
        {

            Mat imgGray = img;

            if (img.NumberOfChannels>1)
            {
                imgGray = new Mat();
                CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);
            }

            Mat histX = new Mat(1, imgGray.Cols, DepthType.Cv32F, 1);
            Mat histY = new Mat(imgGray.Rows, 1, DepthType.Cv32F, 1);

            CvInvoke.Reduce(imgGray, histX, ReduceDimension.SingleRow, reduceType, DepthType.Cv32F);
            CvInvoke.Reduce(imgGray, histY, ReduceDimension.SingleCol, reduceType, DepthType.Cv32F);

            //---------- XVal
            double minVal, maxVal;
            int[] minIds = new int[2]; // размерность dim of shape
            int[] maxIds = new int[2];

            CvInvoke.MinMaxIdx(histX, out minVal, out maxVal, minIds, maxIds);
            int xx = maxIds[1];
            CvInvoke.MinMaxIdx(histY, out minVal, out maxVal, minIds, maxIds);
            int yy = maxIds[0];

            histX.Dispose();
            histY.Dispose();
            if (img.NumberOfChannels > 1)
                imgGray.Dispose();

            return new Point(xx, yy);
        }
    }
}
