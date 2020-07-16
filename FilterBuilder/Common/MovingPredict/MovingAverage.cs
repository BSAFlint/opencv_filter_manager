using FilterBuilder.Common.MovingPredict;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{
    public class MovingAverage: IMovingPredict
    {
        private double m_sum;
        private int m_size;
        private double[] m_loopArray;
        private int m_insertIdx;

        public MovingAverage(int size)
        {
            m_size = size;
            m_loopArray = new double[m_size];
            Clear();
        }

        public void Clear()
        {
            m_sum = 0;
            m_insertIdx = 0;
            for (int n = 0; n < m_size; n++) m_loopArray[n] = 0;
        }

        public double Predict(double x)
        {
            m_sum = m_sum + x - m_loopArray[m_insertIdx];
            m_loopArray[m_insertIdx] = x;
            m_insertIdx = (m_insertIdx + 1) % m_size;
            return m_sum / m_size;
        }

        public double CurrentVal()
        {
            return m_sum / m_size;
        }
    }
}
