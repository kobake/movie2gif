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
        private string m_tmpDirectory;
        public MainWindow()
        {
            InitializeComponent();

            // {exeディレクトリ}/.tmp をtmpディレクトリとして扱う.
            m_tmpDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), ".tmp");
            Directory.CreateDirectory(m_tmpDirectory);

            // バージョン表示
            System.Diagnostics.FileVersionInfo ver =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.Title += " Ver" + ver.ProductVersion;
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
            if (ret == true)
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
            if (w != 0)
            {
                OutputWidth.Text = w + "";
            }
            OutputFilePath.Text = Regex.Replace(InputFilePath.Text, @"\.[A-Za-z0-9_]+$", ".gif");
        }

        class ConvertParam
        {
            public string inputfile;
            public string outputfile;
            public int fps;
            public int width;
        }

        private bool m_doing = false;
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_doing || !ConvertButton.IsEnabled) return;
            ConvertButton.IsEnabled = false;
            m_doing = true;

            LogText.Text = "";
            Log("doing");
            var param = new ConvertParam
            {
                inputfile = InputFilePath.Text,
                outputfile = OutputFilePath.Text,
                fps = int.Parse(OutputFps.Text),
                width = int.Parse(OutputWidth.Text)
            };
            Task.Run(() =>
            {
                try
                {
                    Convert(param);
                }
                catch (Exception ex)
                {
                    Log("\nError: " + ex.Message);
                }
                finally
                {
                    OnConvertFinished();
                }
            });
            Task.Run(() =>
            {
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
            ConvertButton.Dispatcher.Invoke(() =>
            {
                ConvertButton.IsEnabled = true;
                m_doing = false;
                Log("done");
            });
        }

        private void Convert(ConvertParam param)
        {
            var ffmpeg = new NReco.VideoConverter.FFMpegConverter();
            try
            {
                // 仮ファイル
                string tmp_outputfile = Path.Combine(m_tmpDirectory, Path.GetFileName(param.outputfile));
                string tmp_palettefile = Path.Combine(m_tmpDirectory, "palette.png");

                // 変換
                ffmpeg.Invoke(string.Format(@"-y -i ""{0}"" -vf fps={1},scale={2}:-1:flags=lanczos,palettegen ""{3}""", param.inputfile, param.fps, param.width, tmp_palettefile));
                ffmpeg.Invoke(string.Format(@"-y -i ""{0}"" -i ""{1}"" -filter_complex ""fps={2},scale={3}:-1:flags=lanczos[x];[x][1:v]paletteuse"" ""{4}""", param.inputfile, tmp_palettefile, param.fps, param.width, tmp_outputfile));

                // 変換が終わったら元の outputfile の場所にできあがったものをコピー（上書き）する
                File.Copy(tmp_outputfile, param.outputfile, true);

                // 仮ファイルは削除
                File.Delete(tmp_outputfile);
                File.Delete(tmp_palettefile);
            }
            catch (Exception ex)
            {
                Log("\nError: " + ex.Message);
            }
        }
    }
}
