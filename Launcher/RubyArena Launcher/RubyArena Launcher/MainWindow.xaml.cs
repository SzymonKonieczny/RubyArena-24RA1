using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        State launcherState;
        readonly string versionURL = "https://raw.githubusercontent.com/SzymonKonieczny/RubyArena-24RA1/releases/download/version-lookup/version.txt";
        string buildURL;

        string versionLocal;
        string versionNewest;
        string currentDir;
        string buildPath;
        string buildDir;
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
            buildPath = Path.Combine(buildDir, "Ruby Arena.exe");
            zipPath = Path.Combine(currentDir, "build.zip");
            versionLocal = "";
            try
            {
                versionLocal = Util.ReadFileToString(Path.Combine(buildDir, "version.data"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read version data.");
            }
            try
            {
                versionNewest = Util.ReadTextFileFromUrl(versionURL).Result;
            }
            catch
            {
                MessageBox.Show("Unable to contact the server.\n Please wait or contact the owner");
            }

            if (versionNewest != versionLocal)
            {
                launcherState = State.NeedsUpdate;
                PlayButton.Content = "Update";
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
            PlayButton.Content = "Play";
            PlayButton.IsEnabled = true;
        }
        private void RunGame()
        {

        }
        private async void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            switch (launcherState)
            {
                case State.UpToDate:
                    RunGame();
                    break;
                case State.NeedsUpdate:
                    Update().Start();
                    break;
            }
        }

    }
}