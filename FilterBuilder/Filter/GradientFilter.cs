using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    public enum GradientEnum
    {
        Sobel = 0,
        Canny = 1,
        Laplacian = 2
    }

    public class GradientFilter : BaseFilter
    {
        public GradientFilter(GraphFilter graph=null, string name = null) : base(graph, name)
        {
            Type = FilterType.Gradient;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };


            EnumPropertys["Type"] = new EnumProperty(typeof(GradientEnum));
            EnumPropertys["Type"].Set("Sobel");

            IntPropertys["Kern"] = new NumProperty<int>() { Value = 1, MaxVal = 7, MinVal = 1 };

            IntPropertys["Low"] = new NumProperty<int>() { Value = 0, MaxVal = 255, MinVal = 0 };
            IntPropertys["Hight"] = new NumProperty<int>() { Value = 255, MaxVal = 255, MinVal = 0 };
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                GradientEnum gradType = (GradientEnum)EnumPropertys["Type"].Value;
                int gradKern = IntPropertys["Kern"].Value * 2 + 1;

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = new Mat();
                        switch (gradType)
                        {
                            case GradientEnum.Sobel:
                                {
                                    Mat sobelX = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv16S, 3);
                                    Mat sobelY = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv16S, 3);
                                    imgout = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);

                                    CvInvoke.Sobel(outData.Image, sobelX, DepthType.Cv16S, 1, 0, gradKern);
                                    CvInvoke.Sobel(outData.Image, sobelY, DepthType.Cv16S, 0, 1, gradKern);

                                    CvInvoke.ConvertScaleAbs(sobelX, sobelX, 1, 0);
                                    CvInvoke.ConvertScaleAbs(sobelY, sobelY, 1, 0);

                                    CvInvoke.AddWeighted(sobelX, 0.5, sobelY, 0.5, 0, imgout, DepthType.Cv8U);
                                }
                                break;

                            case GradientEnum.Canny:
                                {
                                    Mat srcGray = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.CvtColor(outData.Image, srcGray, ColorConversion.Bgr2Gray);

                                    Mat imgout0 = new Mat();
                                    CvInvoke.Canny(srcGray, imgout0, IntPropertys["Low"].Value, IntPropertys["Hight"].Value, gradKern);

                                    imgout = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.CvtColor(imgout0, imgout, ColorConversion.Gray2Bgr);
                                }
                                break;

                            case GradientEnum.Laplacian:
                                {
                                    Mat srcGray = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.CvtColor(outData.Image, srcGray, ColorConversion.Bgr2Gray);

                                    Mat imgout0 = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv16S, 1);
                                    CvInvoke.Laplacian(srcGray, imgout0, DepthType.Cv16S, gradKern);

                                    imgout = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    imgout0.ConvertTo(imgout, DepthType.Cv8U);
                                }
                                break;
                        }

                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            }
            catch { return false; }

            return true;
        }
    }
}
