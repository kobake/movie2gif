using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace movie2gif
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string text)
        {
            LogText.Text += text + "\n";
            LogText.CaretIndex = LogText.Text.Length;
            LogText.ScrollToEnd();
        }

        private void InputFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "MovieFile(*.avi;*.mp4;*.mpg;*.mov;*.flv;*.wmv)|*.avi;*.mp4;*.mpg;*.mov;*.flv;*.wmv|AnyFile(*.*)|*.*";
            dialog.Title = "Select source movie file";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            bool? ret = dialog.ShowDialog();
            if(ret == true)
            {
                InputFilePath.Text = dialog.FileName;
                GenerateOutputFilePath();
            }
        }

        private void GenerateOutputFilePath()
        {
            OutputFilePath.Text = Regex.Replace(InputFilePath.Text, @"\.[A-Za-z0-9_]+$", ".gif");
        }

        private void OutputFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(OutputFilePath.Text);
            dialog.Filter = "GitFile(*.gif)|*.gif";
            dialog.Title = "Select destination gif file";
            dialog.RestoreDirectory = true;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            bool? ret = dialog.ShowDialog();
            if (ret == true)
            {
                OutputFilePath.Text = dialog.FileName;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filenames.Length > 0)
                {
                    InputFilePath.Text = filenames[0];
                    GenerateOutputFilePath();
                }
            }
        }
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }
}
