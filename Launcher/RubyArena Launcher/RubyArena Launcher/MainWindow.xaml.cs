using Microsoft.Web.WebView2.Wpf;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Path = System.IO.Path;
namespace RubyArena_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum State
        {
            UpToDate,
            Updating,
            NeedsUpdate
        }
        private WebView2 WebViewer;
        State launcherState;
        readonly string versionURL = "https://raw.githubusercontent.com/SzymonKonieczny/RubyArena-24RA1/refs/heads/main/version.json";
        readonly string scrollViewContentURL = "https://raw.githubusercontent.com/SzymonKonieczny/RubyArena-24RA1/refs/heads/main/StaticPatchNotes.txt";
        string buildURL;
        JsonDocument remoteVersionData;
        string versionLocal;
        string versionNewest;
        string currentDir;
        string buildPath;
        string buildDir;
        string gameExePath;
        string webViewUrl = null;
        string scrollViewContent = null;
        string zipPath;
        public MainWindow()
        {
            InitializeComponent();
            PlayButton.IsEnabled = false;
            Loaded += InitializeAsync;
        }
        private async void InitializeAsync(object sender, RoutedEventArgs e)
        {
            currentDir = Directory.GetCurrentDirectory();
            buildDir = Path.Combine(currentDir, "Data");
            zipPath = Path.Combine(currentDir, "build.zip");
            gameExePath = Path.Combine(buildDir, "RubyArenaProject.exe");

            versionLocal = "";
            try
            {
                versionLocal = Util.ReadFileToString(Path.Combine(buildDir, "version.data"));
            }
            catch (Exception ex)
            {
               
            }
            try
            {
                string data = await Util.client.GetStringAsync(versionURL);
                remoteVersionData = JsonDocument.Parse(data);
                versionNewest = Util.GetStringOrDefault(remoteVersionData.RootElement, "version", string.Empty);
                webViewUrl = Util.GetStringOrDefault(remoteVersionData.RootElement, "webViewUrl", string.Empty);
                if(string.IsNullOrEmpty(webViewUrl))
                {
                    ScrollView.IsEnabled = true;
                    scrollViewContent = await Util.client.GetStringAsync(scrollViewContentURL);
 
                    StaticPatchText.Text = scrollViewContent;
                }
                else
                {
                    if(WebViewer== null)
                    {
                        WebViewer = new WebView2();
                        WebViewerHost.Children.Add(WebViewer);
                        await WebViewer.EnsureCoreWebView2Async();
                        WebViewer.Source =new Uri(webViewUrl);
                    }
                    WebViewer.IsEnabled = true;
                    ScrollView.IsEnabled = false;
                }
            }
            catch
            {
                MessageBox.Show("Unable to contact the server.\n Please wait or contact the owner");
            }

            if (versionNewest != versionLocal)
            {
                launcherState = State.NeedsUpdate;
                PlayButton.Content = "Update";
                if (Util.IsValidHttpUrl(versionNewest))
                    buildURL = versionNewest;
                else
                    buildURL = $"https://github.com/SzymonKonieczny/RubyArena-24RA1/releases/download/{versionNewest}/Build.zip";
            }
            else
                launcherState = State.UpToDate;
            
            PlayButton.IsEnabled = true;
        }
        private async Task Update()
        {
            PlayButton.Content = "Update";
            PlayButton.IsEnabled = false;
            launcherState = State.Updating;
            await Util.DownloadFileAsync(buildURL, zipPath);
            Util.DeleteAllFiles(buildDir);
            Util.ExtractZip(zipPath, buildDir);
            Util.DeleteFile(zipPath);

            launcherState = State.UpToDate;
            PlayButton.Content = "Play";
            PlayButton.IsEnabled = true;
        }
        private void RunGame()
        {
            Process.Start(gameExePath);
        }
        private async void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            switch (launcherState)
            {
                case State.UpToDate:
                    RunGame();
                    break;
                case State.NeedsUpdate:
                    Update();
                    break;
            }
        }

    }
}