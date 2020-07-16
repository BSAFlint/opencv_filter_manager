using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common.MovingPredict
{
    /// <summary>
    /// Базовый класс всех скользящих "средних"
    /// </summary>
    public interface IMovingPredict
    {
        void Clear();
        double Predict(double x);
        double CurrentVal();
    }

    public enum MovingPredictType
    {
        None = 0,
        MovingAverage = 1,
        MovingMediana = 2
    }
}
