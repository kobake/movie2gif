using NReco.VideoConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace movie2gif.Models
{
    public class Ffmpeg
    {
        public  static int GetWidth(string filepath)
        {
            int width = 0;
            bool endFlag = false;
            try
            {
                var ffmpeg = new NReco.VideoConverter.FFMpegConverter();
                ffmpeg.LogReceived += (object sender, FFMpegLogEventArgs args) => {
                    // Console.WriteLine(">>>" + args.Data);
                    var m = Regex.Match(args.Data, @", ([0-9]+)x[0-9]+");
                    if (m.Success)
                    {
                        width = int.Parse(m.Groups[1].Value);
                    }
                    if (args.Data.IndexOf("At least one output file must be specified") >= 0)
                    {
                        endFlag = true;
                    }
                };
                ffmpeg.Invoke(string.Format(@"-y -i ""{0}""", filepath));
                // 情報が収集されるまでしばらく待つ (500ms程度)
                for(int i = 0; i < 50; i++)
                {
                    if (endFlag) break;
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return width;
        }
    }
}
