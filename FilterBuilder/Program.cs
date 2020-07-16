using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FilterBuilder.Common;
using FilterBuilder.Filter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder
{
    class Program
    {
        //----------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

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

        // 24 бита полохо работает, лучше 32 или придётся выравнивать на границу двойного слова по ширине
        public static Bitmap GetRGBBitmapFromCvMat(Mat srcMat)
        {
            if (srcMat == null)
                return null;

            Mat srcMatCl = new Mat();            
            switch (srcMat.NumberOfChannels)
            {
                case 1: CvInvoke.CvtColor(srcMat, srcMatCl, ColorConversion.Gray2Bgr);break;
                case 4: CvInvoke.CvtColor(srcMat, srcMatCl, ColorConversion.Bgra2Bgr);break;
                default: srcMatCl = srcMat; break;
            }


            //Mat rgbMat = srcMatCl;
            //if (srcMatCl.Depth !=  DepthType.Cv8U)
            //    srcMatCl.ConvertTo(rgbMat, DepthType.Cv8U);

            Mat rgbMat = srcMatCl;
            srcMatCl.ConvertTo(rgbMat, DepthType.Cv8U);

            uint size = (uint)(rgbMat.Height * rgbMat.Width * 3);

            Bitmap bitmap = new Bitmap(rgbMat.Width, rgbMat.Height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr ptrBmp = bmpData.Scan0;
            CopyMemory(ptrBmp, rgbMat.DataPointer, size);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }



        // сделай наоборот
        //

        //----------------------------------------------------------------------------------------------------------------------------------------------

        static Mat Resize(Mat src, double scale)
        {
            int w = (int)(src.Width * scale);
            int h = (int)(src.Height * scale);
            Mat dst = new Mat();
            CvInvoke.Resize(src, dst, new System.Drawing.Size(w, h), 0, 0, Inter.Area);
            return dst;
        }

        static Mat Rotation(Mat src, double deg)
        {
            int xc = src.Width / 2;
            int yc = src.Height / 2;
            Mat rotationMatrix = new Mat();
            CvInvoke.GetRotationMatrix2D(new System.Drawing.Point(xc, yc), deg, 1.0, rotationMatrix);

            Mat dst = new Mat();
            CvInvoke.WarpAffine(src, dst, rotationMatrix, new System.Drawing.Size(src.Width, src.Height));
            return dst;
        }



        // add MaskFilter
        // add contour filter
        // draw contours

        // неплохо бы у мержделки лвл выбирать (что брать не только предыдущую)
        static void Main(string[] args)
        {
            // dnn tests
            DnnInfo dnn = new DnnInfo("model_bn.pb");


            //---------------------------------------------------------------------------
            String winName = "Test Window"; //The name of the window
            CvInvoke.NamedWindow(winName); //Create the window using the specific name

            // bitmap to mat
            var bitmap = new Bitmap(@"d:\GM\all\15752977512762.png");
            Mat fromBitmap = GetCvMatFromBitmap(bitmap);
            //CvInvoke.Imshow(winName, fromBitmap);
            //CvInvoke.WaitKey(0);


            var bittt = GetRGBBitmapFromCvMat(fromBitmap);
            bittt.Save("out.jpg");
            

            // graph test-------
            GraphFilter graph = new GraphFilter();
            var scFilter = graph.Add(FilterType.Source);
            scFilter["Source"] = fromBitmap;

            var threshFilter = graph.Add(FilterType.Threshold).ConnectBefore( graph.Add(FilterType.Bgr2Gray).ConnectBefore(scFilter));
            threshFilter["Type"] = "AdaptiveGaussianBinaryInv";
            threshFilter["Threshold"] = 125;
            threshFilter["ValueSet"] = 255;
            threshFilter["Kern"] = 5;
            threshFilter["SubtractedFromMean"] = 0;

            var outs = graph.GetOuts();
            CvInvoke.Imshow(winName, outs[0].Image);
            CvInvoke.WaitKey(0);

            threshFilter["Kern"] = 3;
            outs = graph.GetOuts();
            CvInvoke.Imshow(winName, outs[0].Image);
            CvInvoke.WaitKey(0);

            //-----------------------------------------------------------
            var laadFilter = new LoadBgrFilter();
            laadFilter["File"] =  new FileInfo( @"d:\GM\all\15808995027720.jpg");

            var laadFilter2 = new LoadBgrFilter();
            laadFilter2["File"] = new FileInfo(@"d:\GM\all\15808863021350.png");


            // gray ------------------------------------
            var grayFilter = new Bgr2GrayFilter();
            grayFilter.ConnectBefore(laadFilter);
            var imgs00 = grayFilter.GetOut();
            CvInvoke.Imshow(winName, imgs00[0].Image);
            CvInvoke.WaitKey(0);


            //rotate -----------------------------------
            {
                var rotF1 = new RotationFilter();
                rotF1["Deg"] = 45.0;
                rotF1["Scale"] = 0.75;
                rotF1["NewSize"] = 1;
                rotF1["Width"] = 500;
                rotF1["Height"] = 500;
                rotF1.ConnectBefore(laadFilter);


                var rotF2 = new RotationFilter();
                rotF2["Deg"] = 360.0-45.0;
                rotF2["Scale"] = 0.75;
                rotF2["NewSize"] = 1;
                rotF2["Width"] = 500;
                rotF2["Height"] = 500;
                rotF2.ConnectBefore(laadFilter2);


                // MergeFilter
                var mergeFilter = new MergeFilter();
                mergeFilter.ConnectBefore(rotF1);
                mergeFilter.ConnectBefore(rotF2);
                mergeFilter["Alpha"] = 0.5;
                mergeFilter["Beta"] = 0.5;
                mergeFilter["Gamma"] = 0.5;


                var imgs0 = mergeFilter.GetOut();

                var bittt01 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt01.Save("out1.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);
            }



            // resize
            {
                var resFinlt1 = new ResizeAbsFilter();
                resFinlt1["Width"] = 240;
                resFinlt1["Height"] = 320;
                resFinlt1.ConnectBefore(laadFilter);
                resFinlt1.ConnectBefore(laadFilter2);
                var imgs = resFinlt1.GetOut();

                CvInvoke.Imshow(winName, imgs[0].Image);
                CvInvoke.WaitKey(0);

                CvInvoke.Imshow(winName, imgs[1].Image);
                CvInvoke.WaitKey(0);
           
                // MergeFilter
                var mergeFilter = new MergeFilter();
                mergeFilter.ConnectBefore(resFinlt1);
                mergeFilter["Alpha"] = 1.0;
                mergeFilter["Beta"] = -1.0;
                mergeFilter["Gamma"] = 0.0;


                var imgs0 = mergeFilter.GetOut();

                var bittt02 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt02.Save("out2.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);
            }

            // blur
            {
                var blurFilter = new BlurFilter();
                blurFilter["Type"] = "Median";//"Gaussian";
                blurFilter["Kern"] = 6;
                blurFilter.ConnectBefore(laadFilter);

                var imgs0 = blurFilter.GetOut();

                var bittt03 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt03.Save("out3.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);

                Mat fromBitmap00 = GetCvMatFromBitmap(bittt03);
                CvInvoke.Imshow(winName, fromBitmap00);
                CvInvoke.WaitKey(0);
            }

            // Morphology
            {
                var morphFlt = new MorphologyFilter();
                morphFlt["Type"] = "Erode";
                morphFlt["Kern"] = 5;
                morphFlt["Count"] = 2;
                morphFlt["KernType"] = "Ellipse";
                morphFlt.ConnectBefore(laadFilter);
                var imgs0 = morphFlt.GetOut();

                var bittt04 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt04.Save("out4.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);

                Mat fromBitmap00 = GetCvMatFromBitmap(bittt04);
                CvInvoke.Imshow(winName, fromBitmap00);
                CvInvoke.WaitKey(0);
            }

            // Threshold
            {
                var threshFlt = new ThresholdFilter();
                threshFlt["Type"] = "AdaptiveGaussianBinaryInv";
                threshFlt["Threshold"] = 125;
                threshFlt["ValueSet"] = 255;

                threshFlt["Kern"] = 5;
                threshFlt["SubtractedFromMean"] = 0;
                threshFlt.ConnectBefore(grayFilter);
                var imgs0 = threshFlt.GetOut();

                var bittt05 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt05.Save("out5.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);

                Mat fromBitmap00 = GetCvMatFromBitmap(bittt05);
                CvInvoke.Imshow(winName, fromBitmap00);
                CvInvoke.WaitKey(0);
            }

            // Grad
            {
                var gradFltr = new GradientFilter();
                gradFltr["Type"] = "Laplacian"; // "Canny" "Sobel"
                gradFltr["Kern"] = 1;
                gradFltr["Low"] = 0;
                gradFltr["Hight"] = 255;
                gradFltr.ConnectBefore(laadFilter);
                var imgs0 = gradFltr.GetOut();

                var bittt06 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt06.Save("out6.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);


                Mat fromBitmap00 = GetCvMatFromBitmap(bittt06);
                CvInvoke.Imshow(winName, fromBitmap00);
                CvInvoke.WaitKey(0);
            }


            // My test------------------------------------
            {
                var gradFl = new GradientFilter();
                gradFl["Type"] = "Sobel";
                gradFl["Kern"] = 1;
                gradFl.ConnectBefore(laadFilter);

                var morphFlt = new MorphologyFilter();
                morphFlt["Type"] = "Detate";
                morphFlt["Kern"] = 5;
                morphFlt["Count"] = 2;
                morphFlt["KernType"] = "Ellipse";
                morphFlt.ConnectBefore(gradFl);

                var mergeFlt = new MergeFilter();
                mergeFlt.ConnectBefore(laadFilter);
                mergeFlt.ConnectBefore(gradFl);
                mergeFlt["Alpha"] = 1.0;
                mergeFlt["Beta"] = -1.0;
                mergeFlt["Gamma"] = 0.0;

                var blurFilter = new BlurFilter();
                blurFilter["Type"] = "Median";//"Gaussian";
                blurFilter["Kern"] = 6;
                blurFilter.ConnectBefore(mergeFlt);

                var mergeFlt2 = new MergeFilter();
                mergeFlt2.ConnectBefore(blurFilter);
                mergeFlt2.ConnectBefore(gradFl);
                mergeFlt2["Alpha"] = 0.5;
                mergeFlt2["Beta"] = 0.5;
                mergeFlt2["Gamma"] = 0.0;

                var imgs0 = mergeFlt2.GetOut();

                var bittt7 = GetRGBBitmapFromCvMat(imgs0[0].Image);
                bittt7.Save("out7.jpg");

                CvInvoke.Imshow(winName, imgs0[0].Image);
                CvInvoke.WaitKey(0);


                Mat fromBitmap00 = GetCvMatFromBitmap(bittt7);
                CvInvoke.Imshow(winName, fromBitmap00);
                CvInvoke.WaitKey(0);
            }

            CvInvoke.WaitKey(0);  //Wait for the key pressing event
            CvInvoke.DestroyWindow(winName); //Destroy the window if key is pressed
            return;
        }
    }
}
