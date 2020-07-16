using Emgu.CV;
using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{



    // собирает информацию о модели pb
    public class DnnInfo
    {
        private Net                           m_dnnModel;
        private Dictionary<string, Layer>     m_layers;

        private void UpdateInfo()
        {
            m_layers = new Dictionary<string, Layer>();
            foreach (var nm in m_dnnModel.LayerNames)
            {
                m_layers[nm] = m_dnnModel.GetLayer(nm);
                Mat outMat = m_dnnModel.Forward(nm);
            }
        }

        public DnnInfo(Net model)
        {
            m_dnnModel = model;
            UpdateInfo();
        }

        public DnnInfo(string modeFileName)
        {
            try
            {
                m_dnnModel = DnnInvoke.ReadNetFromTensorflow(modeFileName);
                UpdateInfo();
            }
            catch (Exception ex)
            {
                m_dnnModel = null;
            }
        }
    }
}
