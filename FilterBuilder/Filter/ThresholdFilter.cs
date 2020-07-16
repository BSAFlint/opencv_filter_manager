using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    public enum ThresholdEnum
    {
        Binary = 0,
        BinaryInv = 1,
        Trunc = 2,
        ToZero = 3,
        ToZeroInv = 4,
        AdaptiveMeanBinary = 5,
        AdaptiveGaussianBinary = 6,
        AdaptiveMeanBinaryInv = 7,
        AdaptiveGaussianBinaryInv = 8,
    }

    public class ThresholdFilter : BaseFilter
    {
        public ThresholdFilter(GraphFilter graph=null, string name = null) : base(graph, name)
        {
            Type = FilterType.Threshold;
            SourceType = new DataType() { channels = 1, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 1, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            EnumPropertys["Type"] = new EnumProperty(typeof(ThresholdEnum));
            EnumPropertys["Type"].Set("Binary");

            IntPropertys["Threshold"] = new NumProperty<int>() { Value = 0, MaxVal = 255, MinVal = 0 };
            IntPropertys["ValueSet"] = new NumProperty<int>() { Value = 0, MaxVal = 255, MinVal = 0 };

            IntPropertys["Kern"] = new NumProperty<int>() { Value = 1, MaxVal = 7, MinVal = 1 };
            IntPropertys["SubtractedFromMean"] = new NumProperty<int>() { Value = 0, MaxVal = 255, MinVal = 0 };
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                ThresholdEnum threshType = (ThresholdEnum)EnumPropertys["Type"].Value;
                int threshKern = IntPropertys["Kern"].Value * 2 + 1;
                

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        switch (threshType)
                        {
                            case ThresholdEnum.Binary:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.Threshold(outData.Image, outimg, IntPropertys["Threshold"].Value, IntPropertys["ValueSet"].Value, ThresholdType.Binary);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.BinaryInv:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.Threshold(outData.Image, outimg, IntPropertys["Threshold"].Value, IntPropertys["ValueSet"].Value, ThresholdType.BinaryInv);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.Trunc:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.Threshold(outData.Image, outimg, IntPropertys["Threshold"].Value, IntPropertys["ValueSet"].Value, ThresholdType.Trunc);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.ToZero:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.Threshold(outData.Image, outimg, IntPropertys["Threshold"].Value, IntPropertys["ValueSet"].Value, ThresholdType.ToZero);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.ToZeroInv:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.Threshold(outData.Image, outimg, IntPropertys["Threshold"].Value, IntPropertys["ValueSet"].Value, ThresholdType.ToZeroInv);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.AdaptiveMeanBinary:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.AdaptiveThreshold(outData.Image, outimg, IntPropertys["ValueSet"].Value, AdaptiveThresholdType.MeanC, ThresholdType.Binary, threshKern, IntPropertys["SubtractedFromMean"].Value);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.AdaptiveGaussianBinary:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.AdaptiveThreshold(outData.Image, outimg, IntPropertys["ValueSet"].Value, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, threshKern, IntPropertys["SubtractedFromMean"].Value);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.AdaptiveMeanBinaryInv:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.AdaptiveThreshold(outData.Image, outimg, IntPropertys["ValueSet"].Value, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, threshKern, IntPropertys["SubtractedFromMean"].Value);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case ThresholdEnum.AdaptiveGaussianBinaryInv:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 1);
                                    CvInvoke.AdaptiveThreshold(outData.Image, outimg, IntPropertys["ValueSet"].Value, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, threshKern, IntPropertys["SubtractedFromMean"].Value);
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;
                        }
                    }
            } catch { return false; }
            return true;
        }
    }
}
