using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    public enum MorphologyEnum
    {
        Erode = 0,
        Detate = 1,
        Morph_Open = 2,
        Morph_Close = 3,
        Morph_Gradient = 4,
        Morph_Tophat = 5,
        Morph_Blackhat = 6,
        Morph_Hitmiss = 7
    }

    public class MorphologyFilter : BaseFilter
    {
        public MorphologyFilter(GraphFilter graph=null, string name = null) : base(graph, name)
        {
            Type = FilterType.Morphology;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            IntPropertys["Kern"] = new NumProperty<int>() { Value = 1, MaxVal = 7, MinVal = 1 };

            IntPropertys["Count"] = new NumProperty<int>() { Value = 1, MaxVal = 7, MinVal = 1 };


            EnumPropertys["Type"] = new EnumProperty(typeof(MorphologyEnum));
            EnumPropertys["Type"].Set("Erode");

            EnumPropertys["KernType"] = new EnumProperty(typeof(ElementShape));
            EnumPropertys["KernType"].Set("Ellipse");
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                int morphKern = IntPropertys["Kern"].Value * 2 + 1;
                MorphologyEnum morphType = (MorphologyEnum)EnumPropertys["Type"].Value;
                ElementShape shape = (ElementShape)EnumPropertys["KernType"].Value;
               
                var kernMat =
                CvInvoke.GetStructuringElement(ElementShape.Ellipse,
                                                new System.Drawing.Size(morphKern, morphKern),
                                                new System.Drawing.Point(IntPropertys["Kern"].Value, IntPropertys["Kern"].Value));

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        switch (morphType)
                        {
                            case MorphologyEnum.Erode:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.Erode(outData.Image, outimg, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Detate:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.Dilate(outData.Image, outimg, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Open:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.MorphologyEx(outData.Image, outimg, MorphOp.Open, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Close:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.MorphologyEx(outData.Image, outimg, MorphOp.Close, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Gradient:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.MorphologyEx(outData.Image, outimg, MorphOp.Gradient, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Tophat:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.MorphologyEx(outData.Image, outimg, MorphOp.Tophat, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Blackhat:
                                {
                                    Mat outimg = new Mat(outData.Image.Height, outData.Image.Width, DepthType.Cv8U, 3);
                                    CvInvoke.MorphologyEx(outData.Image, outimg, MorphOp.Blackhat, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    this.m_out.Add(new DataSrc(outimg, outData.Info, false));
                                }
                                break;

                            case MorphologyEnum.Morph_Hitmiss:
                                {
                                    Mat outimg = new Mat();
                                    Mat img0 = new Mat();
                                    CvInvoke.CvtColor(outData.Image, img0, ColorConversion.Bgr2Gray); // требует 8UC1
                                    CvInvoke.MorphologyEx(img0, outimg, MorphOp.HitMiss, kernMat, new System.Drawing.Point(-1, -1), IntPropertys["Count"].Value, BorderType.Default, new Emgu.CV.Structure.MCvScalar());
                                    CvInvoke.CvtColor(outimg, img0, ColorConversion.Gray2Bgr);
                                    this.m_out.Add(new DataSrc(img0, outData.Info, false));
                                }
                                break;

                        }
                    }
            }
            catch (Exception ex){
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

    }
}
