using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{
    public class NumProperty<T> where T : IComparable<T>
    {        
        private T m_value;        
        public T MaxVal { get; set; }
        public T MinVal { get; set; }
        public T Value
        {
            get { return m_value; }
            set
            {
                if (MaxVal.CompareTo(value) < 0)
                { 
                    m_value = MaxVal;
                    return;
                }

                if (MinVal.CompareTo(value) > 0)
                {
                    m_value = MinVal;
                    return;
                }

                m_value = value;
            }
        }

        public string Description { get; set; }
    }
}
