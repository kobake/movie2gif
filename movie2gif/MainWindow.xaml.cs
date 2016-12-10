using Microsoft.Win32;
using movie2gif.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        private void Log(string text, bool lf = true)
        {
            LogText.Dispatcher.Invoke(() =>
            {
                if (lf) text += "\n";
                LogText.Text += text;
                LogText.CaretIndex = LogText.Text.Length;
                LogText.ScrollToEnd();
            });
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

        // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- //
        // ファイルドロップ受付
        // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- //
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

        // 入力ファイル受付時
        private void GenerateOutputFilePath()
        {
            int w = Ffmpeg.GetWidth(InputFilePath.Text);
            if(w != 0)
            {
                OutputWidth.Text = w + "";
            }
            OutputFilePath.Text = Regex.Replace(InputFilePath.Text, @"\.[A-Za-z0-9_]+$", ".gif");
        }


        private bool m_doing = false;
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_doing || !ConvertButton.IsEnabled) return;
            ConvertButton.IsEnabled = false;
            m_doing = true;

            Log("doing");
            Task.Run(()=> {
                try
                {
                    Convert();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    OnConvertFinished();
                }
            });
            Task.Run(()=>{
                while (true)
                {
                    Log(".", false);
                    Thread.Sleep(500);
                    if (!m_doing) break;
                }
            });
        }
        private void OnConvertFinished()
        {
            ConvertButton.Dispatcher.Invoke(() => {
                ConvertButton.IsEnabled = true;
                m_doing = false;
                Log("done");
            });
        } 

        private static void Convert()
        {
            Directory.SetCurrentDirectory(@"C:\_tmp\New folder");
            Console.WriteLine(Directory.GetCurrentDirectory());
            var ffmpeg = new NReco.VideoConverter.FFMpegConverter();
            try
            {
                ffmpeg.Invoke(@"-y -i ""C:\_tmp\New folder\a.mp4"" -vf fps=8,scale=1024:-1:flags=lanczos,palettegen ""C:\_tmp\New folder\palette.png""");
                ffmpeg.Invoke(@"-y -i ""C:\_tmp\New folder\a.mp4"" -i ""C:\_tmp\New folder\palette.png"" -filter_complex ""fps=8,scale=1024:-1:flags=lanczos[x];[x][1:v]paletteuse"" ""C:\_tmp\New folder\output.gif""");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
