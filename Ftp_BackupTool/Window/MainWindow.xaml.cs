using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Sftp;
using BackupFileManagement;
using RegistryConnector;

namespace Ftp_BackupTool.Window
{
    /// <summary> Interaction logic for MainWindow.xaml </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const string KeyLocation = "JrdiverSftpBackup";
        private bool autoRun;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckBoxAutoRun.IsChecked == true)
            {
                RunTasks();
                autoRun = true;
            }
        }

        public async void RunTasks(bool runButton = false)
        {
            ProgressBar1.IsIndeterminate = true;

            if (!ValidateSettings())
            {
                return;
            }

            if (autoRun && !runButton)
            {
                Thread.Sleep(5000);
                if(!autoRun)
                    return;
            }

            int.TryParse(TextSftpPort.Text, out int ftpPort);
            int.TryParse(TextMySqlPort.Text, out int sqlPort);

            Download download = new(TextHostName.Text, TextSftpUserName.Text, TextSftpPassword.Password, TextRemoteDirectory.Text, TextLocalDirectory.Text,
                TextMySqlUserName.Text, TextMySqlPassword.Password, ftpPort, sqlPort);
            BackupScheme backup = new(TextLocalDirectory.Text);

            Task currentTask = Task.Run(download.StartDownload);
            await currentTask;

            currentTask = Task.Run(backup.CleanBackupFolder);
            await currentTask;

            ProgressBar1.IsIndeterminate = false;
            Application.Current.Shutdown();
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            RunTasks(true);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void CloseRun_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SaveSettings()
        {
            if (!ValidateSettings())
            {
                CheckBoxAutoRun.IsChecked = false;
                return;
            }

            RegistryUser settings = new() { KeyLocation = KeyLocation };
            settings.AddUserValue("HostName", TextHostName.Text);
            settings.AddUserValue("SftpUserName", TextSftpUserName.Text);
            settings.AddUserValue("SftpPassword", TextSftpPassword.Password);
            settings.AddUserValue("SftpPort", TextSftpPort.Text);
            settings.AddUserValue("MySqlUserName", TextMySqlUserName.Text);
            settings.AddUserValue("MySqlPassword", TextMySqlPassword.Password);
            settings.AddUserValue("MySqlPort", TextMySqlPort.Text);
            settings.AddUserValue("RemoteDirectory", TextRemoteDirectory.Text);
            settings.AddUserValue("LocalDirectory", TextLocalDirectory.Text);
            if (CheckBoxAutoRun != null) settings.AddUserValue("AutoRun", CheckBoxAutoRun.IsChecked.ToString());
        }

        private void LoadSettings()
        {
            RegistryUser settings = new() { KeyLocation = KeyLocation };

            TextHostName.Text = settings.GetUserValue("HostName");
            TextSftpUserName.Text = settings.GetUserValue("SftpUserName");
            TextSftpPassword.Password = settings.GetUserValue("SftpPassword");
            TextSftpPort.Text = settings.GetUserValue("SftpPort");
            TextMySqlUserName.Text = settings.GetUserValue("MySqlUserName");
            TextMySqlPassword.Password = settings.GetUserValue("MySqlPassword");
            TextMySqlPort.Text = settings.GetUserValue("MySqlPort");
            TextRemoteDirectory.Text = settings.GetUserValue("RemoteDirectory");
            TextLocalDirectory.Text = settings.GetUserValue("LocalDirectory");
            bool.TryParse(settings.GetUserValue("AutoRun"), out bool autoRun);
            CheckBoxAutoRun.IsChecked = autoRun;
        }

        private bool ValidateSettings()
        {
            TextHostName.Background = Brushes.White;
            TextSftpUserName.Background = Brushes.White;
            TextSftpPassword.Background = Brushes.White;
            TextSftpPort.Background = Brushes.White;
            TextMySqlUserName.Background = Brushes.White;
            TextMySqlPassword.Background = Brushes.White;
            TextMySqlPort.Background = Brushes.White;
            TextRemoteDirectory.Background = Brushes.White;
            TextLocalDirectory.Background = Brushes.White;
            if (string.IsNullOrWhiteSpace(TextHostName.Text))
            {
                TextHostName.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextSftpUserName.Text))
            {
                TextSftpUserName.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextSftpPassword.Password))
            {
                TextSftpPassword.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextSftpPort.Text) || !int.TryParse(TextSftpPort.Text, out int _))
            {
                TextSftpPort.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextMySqlUserName.Text))
            {
                TextMySqlUserName.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextMySqlPassword.Password))
            {
                TextMySqlPassword.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextMySqlPort.Text) || !int.TryParse(TextMySqlPort.Text, out int _))
            {
                TextMySqlPort.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextRemoteDirectory.Text))
            {
                TextRemoteDirectory.Background = Brushes.LightCoral;
                return false;
            }
            if (string.IsNullOrWhiteSpace(TextLocalDirectory.Text))
            {
                TextLocalDirectory.Background = Brushes.LightCoral;
                return false;
            }

            return true;
        }

        private void CheckBoxAutoRun_Checked(object sender, RoutedEventArgs e)
        {
            autoRun = CheckBoxAutoRun.IsChecked == true;
        }
    }
}
