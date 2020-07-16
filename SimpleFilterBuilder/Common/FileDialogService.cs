using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace SimpleFilterBuilder.Common
{

    public interface IDialogService
    {
        void ShowMessage(string message);   // показ сообщения
        string FilePath { get; set; }   // путь к выбранному файлу
        bool OpenFileDialog(string filter, string searchDir);  // открытие файла
    }


    public class DialogService : IDialogService
    {
        public string FilePath { get; set; }

        public bool OpenFileDialog(string filter, string searchDir = null)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (searchDir!=null)
                openFileDialog.InitialDirectory = searchDir;

            openFileDialog.Filter = filter; // "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                return true;
            }
            return false;
        }


        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
