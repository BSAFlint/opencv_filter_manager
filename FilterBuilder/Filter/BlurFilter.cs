using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{
    public enum BlurEnum
    {
        Gaussian = 0,
        Median = 1,
        Bileteral = 2
    };

    public class BlurFilter : BaseFilter
    {        
        public BlurFilter(GraphFilter graph=null, string name = null) : base(graph, name)
        {
            Type = FilterType.Blur;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            IntPropertys["Kern"] = new NumProperty<int>() { Value = 1, MaxVal = 7, MinVal = 1 };
            EnumPropertys["Type"] = new EnumProperty(typeof(BlurEnum));
            EnumPropertys["Type"].Set("Gaussian");
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                BlurEnum blurType = (BlurEnum)EnumPropertys["Type"].Value;

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = new Mat();
                        int nBlurKern = IntPropertys["Kern"].Value * 2 + 1;
                        switch (blurType)
                        {
                            case BlurEnum.Gaussian:
                                CvInvoke.GaussianBlur(outData.Image, imgout, new System.Drawing.Size(nBlurKern, nBlurKern), 0);
                                break;
                            case BlurEnum.Median:
                                CvInvoke.MedianBlur(outData.Image, imgout, nBlurKern);
                                break;
                            case BlurEnum.Bileteral:
                                CvInvoke.BilateralFilter(outData.Image, imgout, nBlurKern, nBlurKern * 2, nBlurKern * 2, 0);
                                break;
                        }
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            } catch { return false; }
             return true;
        }
    }
}
