using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Policy;
using static System.Windows.Forms.LinkLabel;

namespace YouDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Regex detectUtubeID = new Regex(@"(?:youtu\.be\/|youtube\.com(?:\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=|shorts\/)|youtu\.be\/|embed\/|v\/|m\/|watch\?(?:[^=]+=[^&]+&)*?v=))([^""&?\/\s]{11})");
        readonly Regex detectPlaylist = new Regex(@"^(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=|shorts\/|playlist\?list=)|youtu\.be\/)([^""&?\/\s]{34})$");
        string selectedDir;
        string selectedRes;
        string pythonVersion = null;
        bool mp3Convert = false;

        public MainWindow()
        {
            InitializeComponent();


            //Populate ComboBoxes
            update_log("Initializing...");
            youtube_videos_resBox.Items.Add("1080p");
            youtube_videos_resBox.Items.Add("720p");
            youtube_videos_resBox.Items.Add("480p");
            youtube_videos_resBox.Items.Add("360p");
            youtube_videos_resBox.Items.Add("240p");
            youtube_videos_resBox.Items.Add("144p");
            youtube_videos_resBox.SelectedIndex = 0;

            update_log("Make sure you have Python 3.10.8 installed. Other versions should work, but 3.10.8 is the one used while developing this app.");

            //Get python version if any
            try
            {
                pythonVersion = GetPythonVersion();
                update_log(pythonVersion + " detected.");
            }
            catch (Exception ex)
            {
                update_log("Python not found!");
                update_log("Exception: " + ex.Message);
            }
            if (pythonVersion != null)
            {
                getPythonModules();
            }
        }

        private async void getPythonModules()
        {
            //Install needed python modules
            update_log("Installing needed python modules..");
            //RunCMD("pip install pytube");
            //update_log(await RunCMDAsync("pip install pytube"));
            //update_log(await RunCMDAsync("pip install moviepy"));
            await Task.Run(() => RunCMDAsync("pip install pytube"));
            await Task.Run(() => RunCMDAsync("pip install moviepy"));
            update_log("Modules done.");
            update_log("Ready.");
        }
        private void youtube_videos_clearBtn_Click(object sender, RoutedEventArgs e)
        {
            youtube_videos_txtbox.Clear();
        }

        private async Task<string> RunCMDAsync(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C {command}";

                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;

                process.Start();

                return null;

                process.WaitForExit();
            }
        }

        static string GetPythonVersion()
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                //process.StartInfo.CreateNoWindow = true;

                process.Start();
                string version = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return version;
            }
        }



        private void update_log(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                youtube_videos_log.AppendText(text + "\n");
                youtube_videos_log.ScrollToEnd();
                youtube_playlist_log.AppendText(text + "\n");
                youtube_playlist_log.ScrollToEnd();
            });
        }

        private async void youtube_videos_downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (youtube_videos_mp3Chkbx.IsChecked == true)
            {
                mp3Convert = true;
            }
            else
            {
                mp3Convert = false;
            }
            List<string> urlList = new List<string>();

            if (selectedDir != null)
            {
                int i = 0;
                string[] lines = youtube_videos_txtbox.Text.Split(new[] { Environment.NewLine, "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                update_log("Getting URLs..");
                foreach (string line in lines)
                {
                    i++;
                    Match match = detectUtubeID.Match(line);
                    string utubeID = match.Groups[1].Value;
                    if (detectUtubeID.IsMatch(line))
                    {
                        //update_log("LINE " + i + ": YOUTUBE LINK! ID: " + utubeID);
                        urlList.Add(utubeID);
                    }
                    else
                    {
                        //update_log("LINE " + i + ": not youtube link");
                    }
                    //update_log(line);
                }

                if (urlList.Count > 0)
                {
                    foreach (string url in urlList)
                    {
                        update_log("DOWNLOADING: https://www.youtube.com/watch?v=" + url + "..");
                        string result = await Task.Run(() => RunCMDAsync("python utubedl.py https://www.youtube.com/watch?v=" + url + " 720p " + "\"" + selectedDir + "\" " + mp3Convert));
                        update_log(result);
                        //update_log("Output dir: " + selectedDir);
                    }

                    update_log("Once all CMD windows close, all should be done. I guess. I can't be arsed to figure how to check if python is done.");

                }
                else
                {
                    update_log("No URLs found!");
                }
            }
            else
            {
                update_log("Please choose an output folder!");
            }
        }

        private void youtube_videos_browseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                //folderDialog.Description = "Select a Directory";
                DialogResult result = folderDialog.ShowDialog();

                if (!string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    //selectedDir = folderDialog.SelectedPath.Replace("\\", "/");
                    selectedDir = folderDialog.SelectedPath;
                    youtube_videos_outDir.Text = selectedDir;
                    youtube_playlist_outDir.Text = selectedDir;
                }
            }
        }

        private void youtube_videos_resBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (youtube_videos_resBox.SelectedIndex)
            {
                case 0:
                    selectedRes = "1080p";
                    break;
                case 1:
                    selectedRes = "720p";
                    break;
                case 2:
                    selectedRes = "480p";
                    break;
                case 3:
                    selectedRes = "360p";
                    break;
                case 4:
                    selectedRes = "240p";
                    break;
                case 5:
                    selectedRes = "144p";
                    break;
            }
        }

        private void youtube_videos_mp3Chkbx_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void youtube_playlist_clrBtn_Click(object sender, RoutedEventArgs e)
        {
            youtube_playlist_txtBox.Clear();
        }

        private async void youtube_playlist_downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (youtube_playlist_mp3Chkbx.IsChecked == true)
            {
                mp3Convert = true;
            }
            else
            {
                mp3Convert = false;
            }

            if (selectedDir != null)
            {
                update_log("Getting Playlist..");

                Match match = detectPlaylist.Match(youtube_playlist_txtBox.Text);
                string playlist = match.Groups[1].Value;
                if (detectPlaylist.IsMatch(youtube_playlist_txtBox.Text))
                {
                    update_log(playlist);
                    update_log("DOWNLOADING..");
                    string result = await Task.Run(() => RunCMDAsync("python playlist.py https://www.youtube.com/playlist?list=" + playlist + " \"" + selectedDir + "\" " + mp3Convert));
                    update_log(result);

                    update_log("Once all CMD windows close, all should be done. I guess. I can't be arsed to figure how to check if python is done.");
                }
                else
                {
                    update_log("No playlist URL detected.");
                }


            }
            else
            {
                update_log("Please choose an output folder!");
            }
            
        }
    }
}
