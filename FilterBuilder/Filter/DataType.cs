using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using Emgu.CV.CvEnum;
using System.Collections;

namespace FilterBuilder.Filter
{
    public class DataType
    {
        public bool Source { get; set; } = false;
        public Size size { get; set; }
        public DepthType depth { get; set; }
        public int channels { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is DataType)
            {
                var ob = (DataType)obj;
                return (ob.size == size) && (ob.depth == depth) && (ob.channels == channels);
            }

            return false;
        }
    }
}
