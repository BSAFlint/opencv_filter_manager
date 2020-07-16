using Emgu.CV;
using Emgu.CV.CvEnum;
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
    public enum CenterDrawMode
    {
        BoundingRectCenter = 0, // центр вписанного прямоугольника
        PixelWeightCenter = 1,  // Средний пиксель всех точек контура
        HistogramCenter = 2    // центр гистрограмм-XY_cnt маски контура
    }

    public class CvContourInfo
    {
        Point[] m_points = null;
        public int PointsCnt { get { return m_points.Length; } }
        public void CopyPointsTo(Point[] array, int shift)
        {
            foreach (var pt in m_points)
                array[shift++] = pt;
        }

        public int Id = 0;
        public int ParentId = -1; // использую только чтобы отрисовывать в верном порядке
        public int Level { get; private set; }
        public double Area { get; private set; }
        public double Perimeter { get; private set; }
        public Rectangle BoundingRect { get; private set; }
        public Point BoundingCenter { get; private set; }
        public Point WeightCenter { get; private set; }
        public MCvScalar MainColor { get; set; }
        public MCvScalar PerimeterColor { get; set; }

        // гистограмки даже работают, но могут и не дать того лучшего результата
        public Point HistogramCenter { get; private set; } = new Point(0, 0);
        public uint[] XHistogram { get; private set; } = new uint[1];
        public uint[] YHistogram { get; private set; } = new uint[1];

        /// <summary>
        /// Обновляем XY гистограммы контуров
        /// </summary>
        public void UpdateXYHistogram()
        {
            // Возможно стоит сделать через CvInvoke.Reduce метод
            // просто выделить Mat с размером на BoundingRect
            // Закрасить, и посчитать


            if ((XHistogram.Length != 1))
                return;

            uint[,] mask = new uint[BoundingRect.Height, BoundingRect.Width];
            uint[,] mask2 = new uint[BoundingRect.Height, BoundingRect.Width];

            Point[] BoundingPoints = new Point[m_points.Length];
            for (int n = 0; n < m_points.Length; n++)
            {
                mask[m_points[n].Y - BoundingRect.Y, m_points[n].X - BoundingRect.X] = 1;
                mask2[m_points[n].Y - BoundingRect.Y, m_points[n].X - BoundingRect.X] = 1;
            }


            uint[,] xorTable = new uint[2, 2] { {0,     // 0->0 = 0
                                                  1 },   // 0->1 = 1
                                                  {1,    // 1->0 = 0
                                                  0 } }; // 1->1 = 1


            // fill
            for (int y = 0; y < BoundingRect.Height; y++)
                for (int x = 0; x < BoundingRect.Width - 1; x++)
                    mask[y, x + 1] = xorTable[mask[y, x], mask[y, x + 1]];


            // fill (дважды закрашиваю, т.к. филл этот где-то косячный, и неверно обсчитывает суммы при чужой закркске)
            for (int x = 0; x < BoundingRect.Width; x++)
                for (int y = 0; y < BoundingRect.Height - 1; y++)
                    mask2[y + 1, x] = xorTable[mask2[y, x], mask2[y + 1, x]];


            // YHistogram
            int yMaxID = 0;
            YHistogram = new uint[BoundingRect.Height];

            for (int y = 0; y < BoundingRect.Height; y++)
            {
                uint xSum = 0;
                for (int x = 0; x < BoundingRect.Width; x++)
                    xSum += mask2[y, x];

                YHistogram[y] = xSum;

                if (YHistogram[yMaxID] < YHistogram[y])
                    yMaxID = y;
            }

            // XHistogram
            int xMaxID = 0;
            XHistogram = new uint[BoundingRect.Width];
            for (int x = 0; x < BoundingRect.Width; x++)
            {
                uint ySum = 0;
                for (int y = 0; y < BoundingRect.Height; y++)
                    ySum += mask[y, x];

                XHistogram[x] = ySum;

                if (XHistogram[xMaxID] < XHistogram[x])
                    xMaxID = x;
            }

            HistogramCenter = new Point(xMaxID + BoundingRect.X, yMaxID + BoundingRect.Y);
        }

        private MCvScalar RndColor(int id)
        {
            int c1 = 255 - ((id * 45) % 255);
            int c2 = (155 + id * 55) % 255;
            int c3 = (id * 37) % 255;
            return new MCvScalar(c1, c2, c3);
        }

        public static void PointsShift(Point[] points, int shiftX, int shiftY)
        {
            for (int n = 0; n < points.Length; n++) {
                points[n].X += shiftX;
                points[n].Y += shiftY;
            }
        }

        /// <summary>
        /// Сдвигает точки контура, не создаёт копию
        /// </summary>
        /// <param name="shiftX"></param>
        /// <param name="shiftY"></param>
        public void Shift(int shiftX, int shiftY, int width = -1, int height = -1)
        {
            if ((BoundingRect.X + shiftX) < 0)
                shiftX = -BoundingRect.X;

            if ((BoundingRect.Y + shiftY) < 0)
                shiftY = -BoundingRect.Y;

            if ((width > 0) && ((BoundingRect.X + BoundingRect.Width + shiftX) > width))
                shiftX = (width - (BoundingRect.X + BoundingRect.Width));

            if ((height > 0) && ((BoundingRect.Y + BoundingRect.Height + shiftY) > height))
                shiftY = (height - (BoundingRect.Y + BoundingRect.Height));

            PointsShift(m_points, shiftX, shiftY);
            BoundingRect = new Rectangle(BoundingRect.X + shiftX, BoundingRect.Y + shiftY, BoundingRect.Width, BoundingRect.Height);
            BoundingCenter = new Point(BoundingCenter.X + shiftX, BoundingCenter.Y + shiftY);
            WeightCenter = new Point(WeightCenter.X + shiftX, WeightCenter.Y + shiftY);

            if ((HistogramCenter.X != 0) && (HistogramCenter.Y!=0))
                HistogramCenter = new Point(HistogramCenter.X + shiftX, HistogramCenter.Y + shiftY);
        }

        public Mat CreateMask(Size size, DepthType depth, int val = 1)
        {
            Mat cont_mask = new Mat(size, depth, 1);
            using (VectorOfPoint vp = new VectorOfPoint(m_points))
            using (VectorOfVectorOfPoint vvp = new VectorOfVectorOfPoint(vp))
            {
                CvInvoke.FillPoly(cont_mask, vvp, new MCvScalar(val));
            }
            return cont_mask;
        }

        /// <summary>
        /// [src]*[mask] + [blank]*[not mask]
        /// </summary>
        /// <param name="src">Изображение из кторого нужно вырезать обьект по маске</param>
        /// <param name="blank">Изображение куда нужно вырезанный объект поместить</param>
        /// <param name="shiftX">Сместить по горизонтали при наложении на бланк</param>
        /// <param name="shiftY">Сместить по вертикали при наложении на бланк</param>
        /// <param name="scale">Изменить масштаб наложении на бланк</param>
        /// <param name="angle">Если контур нужно повернуть(относительно BoundingCenter) при наложении на бланк</param>
        /// <returns></returns>
        public Mat MergeWithContourMask(Mat src, Mat blank, int shiftX = 0, int shiftY = 0, double scale = 1, double angle = 0)
        {
            // mask
            Mat mask = CreateMask(src.Size, src.Depth);

            // !mask
            Mat blankFill = mask.Clone();
            blankFill.SetTo(new MCvScalar(1));
            Mat maskInverse = blankFill - mask;

            Mat cropSrc = src.Clone();
            cropSrc.SetTo(new MCvScalar(0, 0, 0), mask);

            Mat cropblank = blank.Clone();
            cropblank.SetTo(new MCvScalar(0, 0, 0), maskInverse);

            Mat resImg = new Mat();
            CvInvoke.AddWeighted(cropSrc, 1,
                                 cropblank, 1,
                                 0, resImg, DepthType.Cv8U);


            mask.Dispose();
            blankFill.Dispose();
            maskInverse.Dispose();
            cropSrc.Dispose();
            cropblank.Dispose();

            return resImg;

            // CvInvoke.WarpAffine(img, dst, rotationMatrix, outSz, InterEnum);
        }

        // Как же мне сделать свободное перемещение камня?
        public void UpdateMainColorAsMeanOfMask(Mat img)
        {
            Mat cont_mask = CreateMask(img.Size, img.Depth, 255);
            this.MainColor = CvInvoke.Mean(img, cont_mask);
            cont_mask.Dispose();
        }

        public static Point GeWeightCenter(Point[] points)
        {
            double xx = 0, yy = 0;
            foreach (var pt in points)
            {
                xx += pt.X;
                yy += pt.Y;
            }

            xx = xx / points.Length;
            yy = yy / points.Length;

            return new Point((int)xx, (int)yy);
        }


        public bool IsInsideRect(Rectangle rect)
        {
            return (rect.X <= BoundingRect.X) && ((rect.X + rect.Width) >= (BoundingRect.X + BoundingRect.Width)) &&
                   (rect.Y <= BoundingRect.Y) && ((rect.Y + rect.Height) >= (BoundingRect.Y + BoundingRect.Height));
        }

        public CvContourInfo(int id, int parentId, VectorOfPoint points)
        {
            this.Id = id;
            this.ParentId = parentId;
            this.m_points = points.ToArray();
            this.MainColor = RndColor(id);
            this.PerimeterColor = RndColor(id + 100);
            this.Area = CvInvoke.ContourArea(points, false);
            this.Perimeter = CvInvoke.ArcLength(points, false);
            this.BoundingRect = CvInvoke.BoundingRectangle(points);
            this.BoundingCenter = new Point(BoundingRect.X + BoundingRect.Width / 2, BoundingRect.Y + BoundingRect.Height / 2);
            this.WeightCenter = GeWeightCenter(m_points);
        }

        public CvContourInfo(int id, int parentId, Point[] points, bool searchProperty = false)
        {
            this.Id = id;
            this.ParentId = parentId;
            this.m_points = points;
            this.MainColor = RndColor(id);
            this.PerimeterColor = RndColor(id + 100);

            if (searchProperty)
            {
                this.BoundingCenter = new Point(BoundingRect.X + BoundingRect.Width / 2, BoundingRect.Y + BoundingRect.Height / 2);
                this.WeightCenter = GeWeightCenter(m_points);

                using (VectorOfPoint vp = new VectorOfPoint(m_points))
                using (VectorOfVectorOfPoint vvp = new VectorOfVectorOfPoint(vp))
                {
                    this.BoundingRect = CvInvoke.BoundingRectangle(vvp);
                    this.Perimeter = CvInvoke.ArcLength(vvp, false);

                    //todo
                }
            }
        }

        public CvContourInfo()
        {
        }

        public CvContourInfo Clone()
        {
            CvContourInfo info = new CvContourInfo();

            info.m_points = new Point[this.m_points.Length];
            for (int i = 0; i < this.m_points.Length; i++)
                info.m_points[i] = new Point(this.m_points[i].X, this.m_points[i].Y);

            info.Id = this.Id;
            info.ParentId = this.ParentId;
            info.MainColor = new MCvScalar(this.MainColor.V0, this.MainColor.V1, this.MainColor.V2);
            info.PerimeterColor = new MCvScalar(this.PerimeterColor.V0, this.PerimeterColor.V1, this.PerimeterColor.V2);
            info.Area = this.Area;
            info.Perimeter = this.Perimeter;
            info.BoundingRect = new Rectangle(this.BoundingRect.X, this.BoundingRect.Y, this.BoundingRect.Width, this.BoundingRect.Height);
            info.BoundingCenter = new Point(this.BoundingCenter.X, this.BoundingCenter.Y);
            info.WeightCenter = new Point(this.WeightCenter.X, this.WeightCenter.Y);
            info.HistogramCenter = new Point(this.HistogramCenter.X, this.HistogramCenter.Y);
            return info;
        }


        /// <summary>
        /// Отдаёт внешний мыссив точек при помощи афинной гистограммы
        /// Чтобы не счиатать чисто углу, считается двухмерная-гистограмм периметра прямоугольника с ранджем:
        /// [+1;-1]     []      [+1;+1]
        /// []          []          []
        /// [-1;-1]     []      [-1;+1]
        /// Все внешние точки периметра это элементы гистограммы, мин. размер [3;3] и он скейлится на [3*scale;3*scale], если конутр нужно описать более точными углами
        /// Периметр скорее описывает апроксимированный круг, а не квадрат
        /// </summary>
        /// <param name="center">Аффинный центр</param>
        /// <param name="points">Список точек</param>
        /// <param name="histogramScale">Число баров гистограммы 3*histogramScale* (длина стороны матрицы)</param>
        /// <param name="radiusScale">Изменяет радиус от центра для каждой внешней точки из пропорции</param>
        /// <param name="movingPredict">Если не null, то результат будет пропущен через скользящую для разглаживания контура</param>
        /// <returns></returns>
        public static Point[] AraundPoints(Point center, Point[] points, int histogramScale = 1, double radiusScale = 1, MovingPredictType movingType = MovingPredictType.None, int movingRange = 1)
        {
            int perimeterSRectSz = 2 * 3 * histogramScale + 2 * (3 * histogramScale - 2);
            Point[] exPoints = new Point[perimeterSRectSz];


            int maxIdx = 3 * histogramScale - 1;
            int maxIdxHalf = (3 * histogramScale) / 2;

            int maxIdxHalfB = maxIdxHalf;
            if (histogramScale % 2 == 0)
                maxIdxHalfB -= 1;


            double reMaxIdx = 1.0 / maxIdx;


            // псевдоафинное пространсво
            double[,] sincosHistogram = new double[3 * histogramScale, 3 * histogramScale];

            // set Histogram
            foreach (var pt in points)
            {
                double sqlen = (center.X - pt.X) * (center.X - pt.X) +
                             (center.Y - pt.Y) * (center.Y - pt.Y);
                double len = Math.Sqrt(sqlen);

                if (len == 0)
                    continue;

                // [-1;+1]
                double sinx = (pt.X - center.X) / len;
                double cosx = (pt.Y - center.Y) / len;

                // [0;+2]
                sinx += 1;
                cosx += 1;

                // [0;+1]
                sinx /= 2;
                cosx /= 2;

                // [0;+maxIdx]
                int xx = (int)Math.Round(sinx * maxIdx, MidpointRounding.AwayFromZero);
                int yy = (int)Math.Round(cosx * maxIdx, MidpointRounding.AwayFromZero);

                if (sincosHistogram[xx, yy] < len)
                    sincosHistogram[xx, yy] = len;
            }

            // Для востановления будем пользоваться индексами гистограммы
            // w = l * sin(x)
            // h = l * cos(x)

            // теперь точки нужно связать, для этого их нужно в верном порядке обойти
            // Т.к. точки расположенный по кругу, остаётся искать не нулевые элементы в четвертях и их обхоить
            // обход по часовой стрелки:

            int ptId = 0;
            // w - sin
            // h - cos


            // верл - право
            for (int w = maxIdxHalf; w <= maxIdx; w++)
                for (int h = 0; h < maxIdxHalf; h++)
                    if (sincosHistogram[w, h] != 0)
                    {
                        double widht = radiusScale * sincosHistogram[w, h] * (w * reMaxIdx * 2 - 1);
                        double height = radiusScale * sincosHistogram[w, h] * (h * reMaxIdx * 2 - 1);
                        exPoints[ptId++] = new Point(center.X + (int)(widht), center.Y + (int)(height));
                    }

            // низ право 
            for (int h = maxIdxHalf; h <= maxIdx; h++)
                for (int w = maxIdx; w > maxIdxHalfB; w--)
                    if (sincosHistogram[w, h] != 0)
                    {
                        double widht = radiusScale * sincosHistogram[w, h] * (w * reMaxIdx * 2 - 1);
                        double height = radiusScale * sincosHistogram[w, h] * (h * reMaxIdx * 2 - 1);
                        exPoints[ptId++] = new Point(center.X + (int)(widht), center.Y + (int)(height));
                    }

            //// низ лево 
            for (int w = maxIdxHalfB; w >= 0; w--)
                for (int h = maxIdx; h > maxIdxHalfB; h--)
                    if (sincosHistogram[w, h] != 0)
                    {
                        double widht = radiusScale * sincosHistogram[w, h] * (w * reMaxIdx * 2 - 1);
                        double height = radiusScale * sincosHistogram[w, h] * (h * reMaxIdx * 2 - 1);
                        exPoints[ptId++] = new Point(center.X + (int)(widht), center.Y + (int)(height));
                    }

            // верх лево
            for (int h = maxIdxHalfB; h >= 0; h--)
                for (int w = 0; w < maxIdxHalf; w++)
                    if (sincosHistogram[w, h] != 0)
                    {
                        double widht = radiusScale * sincosHistogram[w, h] * (w * reMaxIdx * 2 - 1);
                        double height = radiusScale * sincosHistogram[w, h] * (h * reMaxIdx * 2 - 1);
                        exPoints[ptId++] = new Point(center.X + (int)(widht), center.Y + (int)(height));
                    }

            // Некоторые точки уходят в ноль, их стоит исключить из ответа
            // Плюс тут же пррёдусь скользящей средней по ним всем
            var outPoints = new Point[ptId];

            if (ptId>0)
            switch (movingType)
            {
                case MovingPredictType.None:
                    for (int n = 0; n < ptId; n++)
                        outPoints[n] = exPoints[n];
                    break;
                case MovingPredictType.MovingAverage:
                    {
                        MovingAverage movingX = new MovingAverage(movingRange);
                        MovingAverage movingY = new MovingAverage(movingRange);

                        for (int n = 0; n < movingRange; n++)
                        {
                            movingX.Predict(exPoints[0].X);
                            movingY.Predict(exPoints[0].Y);
                        }

                        for (int n = 0; n < ptId; n++)
                            outPoints[n] = new Point((int)movingX.Predict(exPoints[n].X),
                                                     (int)movingY.Predict(exPoints[n].Y));

                    }
                    break;
                case MovingPredictType.MovingMediana:
                        {
                            MovingMediana movingX = new MovingMediana(movingRange);
                            MovingMediana movingY = new MovingMediana(movingRange);

                            for (int n = 0; n < movingRange; n++)
                            {
                                movingX.Predict(exPoints[0].X);
                                movingY.Predict(exPoints[0].Y);
                            }

                            for (int n = 0; n < ptId; n++)
                                outPoints[n] = new Point((int)movingX.Predict(exPoints[n].X),
                                                         (int)movingY.Predict(exPoints[n].Y));

                        }
                    break;
            }

             return outPoints;
        }


        /// <summary>
        /// Создаёт контур из внешних точек при помощи афинной гистограммы
        /// </summary>
        /// <param name="centerMode">Выбор центра внешнего контура</param>
        /// <param name="histogramScale">Число баров гистограммы 3*histogramScale* (длина стороны матрицы)</param>
        /// <param name="radiusScale">Скалированный радиус от центра</param>
        /// <returns></returns>
        public CvContourInfo ToAraundContour(CenterDrawMode centerMode = CenterDrawMode.BoundingRectCenter, int histogramScale = 1, double radiusScale = 1, MovingPredictType movingType = MovingPredictType.None, int movingRange = 1)
        {
            CvContourInfo araundContour = new CvContourInfo();

            Point center = this.BoundingCenter;
            switch (centerMode)
            {
                case CenterDrawMode.PixelWeightCenter: center = this.WeightCenter; break;
                case CenterDrawMode.HistogramCenter:
                    UpdateXYHistogram();
                    center = this.HistogramCenter;
                    break;
            }

            araundContour.m_points = AraundPoints(center, m_points, histogramScale, radiusScale, movingType, movingRange);

            araundContour.Id = this.Id;
            araundContour.ParentId = this.ParentId;
            araundContour.MainColor = new MCvScalar(this.MainColor.V0, this.MainColor.V1, this.MainColor.V2);
            araundContour.PerimeterColor = new MCvScalar(this.PerimeterColor.V0, this.PerimeterColor.V1, this.PerimeterColor.V2);
            araundContour.Area = this.Area;
            araundContour.Perimeter = this.Perimeter;
            araundContour.BoundingRect = new Rectangle(this.BoundingRect.X, this.BoundingRect.Y, this.BoundingRect.Width, this.BoundingRect.Height);
            araundContour.BoundingCenter = new Point(this.BoundingCenter.X, this.BoundingCenter.Y);
            araundContour.WeightCenter = new Point(this.WeightCenter.X, this.WeightCenter.Y);
            araundContour.HistogramCenter = new Point(this.HistogramCenter.X, this.HistogramCenter.Y);

            return araundContour;
        }



        public void Draw(Mat src, bool bFill = false, bool bShowBoundingRect = false, CenterDrawMode centerDrawMode = CenterDrawMode.BoundingRectCenter, int thickness = 3, bool bHistogram = false)
        {
            using (VectorOfPoint vp = new VectorOfPoint(m_points))
            using (VectorOfVectorOfPoint vvp = new VectorOfVectorOfPoint(vp))
            {
                if (bFill)
                    CvInvoke.FillPoly(src, vvp, MainColor);

                if (thickness > 0)
                    CvInvoke.DrawContours(src, vvp, 0, PerimeterColor, thickness, LineType.EightConnected);

                if (bShowBoundingRect && ((thickness > 0)))
                {
                    Point usedCenter = BoundingCenter;
                    switch (centerDrawMode)
                    {
                        case CenterDrawMode.BoundingRectCenter: usedCenter = BoundingCenter; break;
                        case CenterDrawMode.PixelWeightCenter: usedCenter = WeightCenter; break;
                        case CenterDrawMode.HistogramCenter: usedCenter = HistogramCenter; break;
                    }


                    CvInvoke.Rectangle(src, this.BoundingRect, PerimeterColor, thickness);
                    int szX = BoundingRect.Width / 8;
                    int szY = BoundingRect.Height / 8;
                    Point linex1 = new Point(usedCenter.X - szX, usedCenter.Y);
                    Point linex2 = new Point(usedCenter.X + szX, usedCenter.Y);

                    Point liney1 = new Point(usedCenter.X, usedCenter.Y - szY);
                    Point liney2 = new Point(usedCenter.X, usedCenter.Y + szY);

                    CvInvoke.Line(src, linex1, linex2, PerimeterColor, thickness);
                    CvInvoke.Line(src, liney1, liney2, PerimeterColor, thickness);
                }

                if (bHistogram)
                {
                    UpdateXYHistogram();
                    for (int xx = 0; xx < XHistogram.Length; xx++)
                    {
                        Point ln1 = new Point(BoundingRect.X + xx, BoundingRect.Y + BoundingRect.Height);
                        Point ln2 = new Point(BoundingRect.X + xx, BoundingRect.Y + BoundingRect.Height + (int)XHistogram[xx]);
                        CvInvoke.Line(src, ln1, ln2, PerimeterColor, thickness);
                    }

                    for (int yy = 0; yy < YHistogram.Length; yy++)
                    {
                        Point ln1 = new Point(BoundingRect.X + BoundingRect.Width, BoundingRect.Y + yy);
                        Point ln2 = new Point(BoundingRect.X + BoundingRect.Width + (int)YHistogram[yy], BoundingRect.Y + yy);
                        CvInvoke.Line(src, ln1, ln2, PerimeterColor, thickness);
                    }
                }
            }
        }
    }



    public class CvContours
    {
        private List<CvContourInfo> m_contours;

        public CvContours()
        {
            m_contours = new List<CvContourInfo>();
        }


        public void SetColor(MCvScalar color)
        {
            foreach (var cont in m_contours)
            {
                cont.MainColor = color;
                cont.PerimeterColor = color;
            }
        }

        public void Update(Mat img, ChainApproxMethod method = ChainApproxMethod.ChainApproxNone)
        {
            m_contours.Clear();

            Mat grayImg = img;
            if (img.NumberOfChannels > 1)
            {
                grayImg = new Mat();
                CvInvoke.CvtColor(img, grayImg, ColorConversion.Bgr2Gray);
            }

            // hierachy[0][i] 0я иерархии для i-ого контура
            // hierachy[0][i][0] - индекс следующего контура на том же уровне
            // hierachy[0][i][1] - индекс предыдущий контура на том же уровне
            // hierachy[0][i][2] - Индекс потомка
            // hierachy[0][i][3] - Индекс родителя
            using (Mat hierachy = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(grayImg, contours, hierachy, RetrType.Tree, method);
                if (contours.Size == 0)
                    return;

                int sz = contours.Size * 4;
                int[] hierachyArray = new int[sz];
                Marshal.Copy(hierachy.DataPointer, hierachyArray, 0, sz);

                for (int i = 0; i < contours.Size; i++)
                    if (contours[i].Size > 2)
                    {
                        int parentID = hierachyArray[i * 4 + 3];
                        CvContourInfo info = new CvContourInfo(i, parentID, contours[i]); // пока не понял логику
                        m_contours.Add(info);
                    }
            }
        }

        public void UpdateMainColorAsMeanOfMask(Mat img)
        {
            foreach (var contour in m_contours)
                contour.UpdateMainColorAsMeanOfMask(img);
        }

        public void UpdateXYHistogram()
        {
            foreach (var contour in m_contours)
                contour.UpdateXYHistogram();
        }

        // т.к. мы их удаляем, макс индекс может быть другим
        private int GetMaxId()
        {
            int id = 0;
            foreach (var contour in m_contours)
                if (contour.Id > id)
                    id = contour.Id;
            return id;
        }

        /// <summary>
        /// Сдвигает контуры
        /// </summary>
        /// <param name="shiftX"></param>
        /// <param name="shiftY"></param>
        /// <param name="width">Если не -1 будет введино ограничение на макс ширену</param>
        /// <param name="height">Если не -1 будет введино ограничение на макс высоту</param>
        /// <returns></returns>
        public CvContours Shift(int shiftX, int shiftY, int width = -1, int height = -1)
        {
            CvContours resultContour = new CvContours();
            foreach (var contour in m_contours)
            {
                var shiftCntr = contour.Clone();
                shiftCntr.Shift(shiftX, shiftY, width, height);
                resultContour.m_contours.Add(shiftCntr);
            }

            return resultContour;
        }


        public CvContours GetAroundContours(CenterDrawMode centerMode, int histogramScale = 1, double radiusScale = 1, MovingPredictType movingType = MovingPredictType.None, int movingRange = 1)
        {
            CvContours resultContour = new CvContours();
            foreach (var contour in m_contours)
                resultContour.m_contours.Add(contour.ToAraundContour(centerMode, histogramScale, radiusScale, movingType, movingRange));

            return resultContour;
        }

        public CvContours GetAreaInRange(double minVal, double maxVal)
        {
            CvContours resultContour = new CvContours();
            foreach (var contour in m_contours)
                if ((contour.Area >= minVal) && (contour.Area <= maxVal))
                    resultContour.m_contours.Add(contour.Clone());

            return resultContour;
        }

        public CvContours GetMaxAreaContour()
        {
            CvContours resultContours = new CvContours();
            int maxAreaId = 0;

            if (m_contours.Count == 0)
                return resultContours;

            for (int nId = 0; nId < m_contours.Count; nId++)
                if (m_contours[maxAreaId].Area < m_contours[nId].Area)
                    maxAreaId = nId;

            resultContours.m_contours.Add(m_contours[maxAreaId].Clone());
            return resultContours;
        }

        public CvContours GetNearPointContour(Point pt, CenterDrawMode centerMode)
        {
            CvContours resultContours = new CvContours();
            int nearId = 0;
            double minLen = double.MaxValue;

            for (int nId = 0; nId < m_contours.Count; nId++)
            {
                Point centerPoint = m_contours[nId].BoundingCenter;
                switch (centerMode)
                {
                    case CenterDrawMode.PixelWeightCenter:
                        centerPoint = m_contours[nId].WeightCenter;
                        break;
                    case CenterDrawMode.HistogramCenter:
                        centerPoint = m_contours[nId].HistogramCenter;
                        break;
                }

                double len = (centerPoint.X - pt.X) * (centerPoint.X - pt.X) +
                             (centerPoint.Y - pt.Y) * (centerPoint.Y - pt.Y);

                if (len < minLen)
                {
                    minLen = len;
                    nearId = nId;
                }
            }

            resultContours.m_contours.Add(m_contours[nearId].Clone());
            return resultContours;
        }

        public CvContours GetPerimeterInRange(double minVal, double maxVal)
        {
            CvContours resultContours = new CvContours();
            foreach (var contour in m_contours)
                if ((contour.Perimeter >= minVal) && (contour.Perimeter <= maxVal))
                    resultContours.m_contours.Add(contour.Clone());

            return resultContours;
        }

        public CvContours GetBoundingRectInRange(int minWidth, int maxWidth, int minHeight, int maxHeight)
        {
            CvContours resultContours = new CvContours();
            foreach (var contour in m_contours)
                if ((contour.BoundingRect.Width >= minWidth) && (contour.BoundingRect.Width <= maxWidth) &&
                    (contour.BoundingRect.Height >= minHeight) && (contour.BoundingRect.Height <= maxHeight))
                    resultContours.m_contours.Add(contour.Clone());

            return resultContours;
        }

        public CvContours GetInsideRectArea(Rectangle rect)
        {
            CvContours resultContours = new CvContours();
            foreach (var contour in m_contours)
                if (contour.IsInsideRect(rect))
                    resultContours.m_contours.Add(contour.Clone());
            return resultContours;
        }





        // По идее ещё нужен алгоритм кластеризации
        //public CvContours[] GetClasters()


        /// <summary>
        /// Сливает все конутра коллекции во внешний контур с единым центром
        /// </summary>
        /// <param name="centerMode"></param>
        /// <param name="histogramScale"></param>
        /// <param name="radiusScale"></param>
        /// <returns></returns>
        public CvContours GetConcatenateAroundContour(CenterDrawMode centerMode = CenterDrawMode.BoundingRectCenter, int histogramScale = 1, double radiusScale = 1)
        {
            CvContours resultContour = new CvContours();
            if (m_contours.Count == 0)
                return resultContour;

            int totalPoints = 0;
            foreach (var contour in m_contours)
                totalPoints += contour.PointsCnt;

            if (totalPoints == 0)
                return resultContour;

            double xx = 0;
            double yy = 0;

            int shift = 0;
            Point[] points = new Point[totalPoints];
            foreach (var contour in m_contours)
            {
                contour.CopyPointsTo(points, shift);
                shift += contour.PointsCnt;

                switch (centerMode)
                {
                    case CenterDrawMode.BoundingRectCenter:
                        xx += contour.BoundingCenter.X;
                        yy += contour.BoundingCenter.Y;
                        break;

                    case CenterDrawMode.PixelWeightCenter:
                        xx += contour.WeightCenter.X;
                        yy += contour.WeightCenter.Y;
                        break;

                    case CenterDrawMode.HistogramCenter:
                        contour.UpdateXYHistogram();
                        xx += contour.HistogramCenter.X;
                        yy += contour.HistogramCenter.Y;
                        break;
                }
            }

            Point commonCenterPoint = new Point((int)(xx / m_contours.Count), (int)(yy / m_contours.Count));
            var aroundPoints = CvContourInfo.AraundPoints(commonCenterPoint, points, histogramScale, radiusScale);

            CvContourInfo contourInfo = new CvContourInfo(0,-1, aroundPoints);
            resultContour.m_contours.Add(contourInfo);

            return resultContour;
        }


        /// <summary>
        /// Cоздаёт прямоугольник вокруг точки скопления центров контуров (на случай если контура разрываются)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bUseWeightCenter">Если True, то в качестве центра будет использован весовой центр, а не геометрический</param>
        /// <returns></returns>
        public Rectangle GeRectWithConcatenateCentert(int width, int height, bool bUseWeightCenter = false)
        {
            double xc = 0;
            double yc = 0;
            foreach (var contour in m_contours)
            {
                if (bUseWeightCenter) { xc += contour.WeightCenter.X; yc += contour.WeightCenter.Y; }
                else { xc += contour.BoundingCenter.X; yc += contour.BoundingCenter.Y; }
            }

            int sz = m_contours.Count;
            xc = xc / sz;
            yc = yc / sz;
            return new Rectangle((int)(xc - width / 2), (int)(yc - height / 2), width, height);
        }


        /// <summary>
        /// экспортирует все регионы, для дальнейшего кропа и.т.д.
        /// </summary>
        /// <param name="width">Абсолютная ширена</param>
        /// <param name="height">Абсолютная выстоа</param>
        /// <param name="bUseWeightCenter">Если True, то в качестве центра будет использован весовой центр, а не геометрический</param>
        /// <returns></returns>
        public List<Rectangle> ExportRects(int width, int height, bool bUseWeightCenter = false)
        {
            List<Rectangle> exList = new List<Rectangle>();
            foreach (var contour in m_contours)
            {
                int xc = 0;
                int yc = 0;

                if (bUseWeightCenter) { xc += contour.WeightCenter.X; yc += contour.WeightCenter.Y; }
                else { xc += contour.BoundingCenter.X; yc += contour.BoundingCenter.Y; }

                exList.Add(new Rectangle(xc - (int)(width / 2), yc - (int)(height / 2), width, height));
            }

            return exList;
        }

        /// <summary>
        /// экспортирует все регионы, для дальнейшего кропа и.т.д. Всегда от центра BoundingRects (НЕТ ВЕРСИИ СО СМЕЩЁННЫМ ЦЕНТРОМ, Т.К. ТАМ НЕ ЯСНО ЧТО ИСПОЛЬЗОВАТЬ ЗА SIZE)
        /// </summary>
        /// <param name="scaleX"> множитель ширены </param>
        /// <param name="scaleY"> множитель высоты </param>
        /// <returns></returns>
        public List<Rectangle> ExportBoundingRects(double scaleX = 1, double scaleY = 1)
        {
            List<Rectangle> exList = new List<Rectangle>();
            foreach (var contour in m_contours)
            {
                int xc = contour.BoundingRect.X + contour.BoundingRect.Width / 2;
                int yc = contour.BoundingRect.Y + contour.BoundingRect.Height / 2;

                int scaleWidth = (int)(contour.BoundingRect.Width * scaleX);
                int scaleHeight = (int)(contour.BoundingRect.Height * scaleY);

                exList.Add(new Rectangle(xc - (int)(scaleWidth / 2), yc - (int)(scaleHeight / 2), scaleWidth, scaleHeight));
            }

            return exList;
        }



        /// <summary>
        /// [src]*[mask] + [blank]*[not mask]
        /// </summary>
        /// <param name="src">Изображение из кторого нужно вырезать обьект по маске</param>
        /// <param name="blank">Изображение куда нужно вырезанный объект поместить</param>
        /// <param name="shiftX">Сместить по горизонтали при наложении на бланк</param>
        /// <param name="shiftY">Сместить по вертикали при наложении на бланк</param>
        /// <param name="scale">Изменить масштаб наложении на бланк</param>
        /// <param name="angle">Если контур нужно повернуть(относительно BoundingCenter) при наложении на бланк</param>
        /// <returns></returns>
        public List<Mat> MergeWithContourMask(Mat src, Mat blank, int shiftX = 0, int shiftY = 0, double scale = 1, double angle = 0)
        {
            List<Mat> exList = new List<Mat>();
            foreach (var contour in m_contours)
                exList.Add(contour.MergeWithContourMask(src, blank, shiftX, shiftY, scale, angle));
            return exList;
        }



        // Можешь удалять и чистить свои коллекции контуров, я вяжусь на инекс внутри них, а не на индекс по коллкции
        // так дольше но не надо возиться с другими вещами
        private void FillDrawRecurrent(Mat src, bool[] WriteAlready, int nID, int thickness = 3, bool bHistogram = false)
        {
            if (WriteAlready[nID])
                return;

            foreach (var contour in m_contours)
                if (contour.Id == nID) // если коллекция пересобранна, то не всегда m_contours[i].Id = i
                {
                    if (contour.ParentId != -1)
                        FillDrawRecurrent(src, WriteAlready, contour.ParentId, thickness);
                    contour.Draw(src, true, false, CenterDrawMode.BoundingRectCenter, thickness, bHistogram);
                    WriteAlready[nID] = true;
                    return;
                }
        }

        public void Draw(Mat src, bool bFill = false, bool bShowBoundingRect = false, CenterDrawMode centerDrawMode = CenterDrawMode.BoundingRectCenter, int thickness = 3, bool bHistogram = false)
        {
            if (!bFill)
            {
                foreach (var contour in m_contours)
                    contour.Draw(src, bFill, bShowBoundingRect, centerDrawMode, thickness, bHistogram);
                return;
            }

            // Нужно рисовать исходя из иерархии
            bool[] WriteAlready = new bool[GetMaxId() + 1];
            foreach (var contour in m_contours)
                FillDrawRecurrent(src, WriteAlready, contour.Id, thickness, bHistogram);
        }
    }
}
