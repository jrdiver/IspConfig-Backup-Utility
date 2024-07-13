using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Sftp;
using BackupFileManagement;
using Shared;
using Shared.Objects;
using System.Linq;

namespace Ftp_BackupTool.Window;

/// <summary> Interaction logic for MainWindow.xaml </summary>
public partial class MainWindow
{
    private const string KeyLocation = "JrdiverSftpBackup";
    private bool autoRun;
    private List<BackupDefinition> backupDefinitions = [];
    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        if (backupDefinitions.Count > 0)
        {
            DropSelectedHost.ItemsSource = backupDefinitions.Select(x => x.Host + ":" + x.FtpPort);
            DropSelectedHost.SelectedIndex = 0;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (CheckBoxAutoRun.IsChecked != true) return;

        RunTasks();
        autoRun = true;
    }

    public async void RunTasks()
    {
        ProgressBar1.IsIndeterminate = true;

        if (!ValidateSettings()) return;

        foreach (BackupDefinition backupInfo in backupDefinitions)
        {
            Download download = new(backupInfo, TextEmailApiKey.Password);
            BackupScheme backup = new(backupInfo.LocalFolder);

            Task currentTask = Task.Run(download.StartDownload);
            await currentTask;

            currentTask = Task.Run(backup.CleanBackupFolder);
            await currentTask;
        }

        ProgressBar1.IsIndeterminate = false;

        if (autoRun)
            Application.Current.Shutdown();
    }

    private void ButtonRun_Click(object sender, RoutedEventArgs e) => RunTasks();

    private void ButtonSave_Click(object sender, RoutedEventArgs e) => SaveSettings();

    private void CloseRun_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void SaveSettings()
    {
        if (!ValidateSettings())
        {
            CheckBoxAutoRun.IsChecked = false;
            return;
        }

        BackupDefinition definition = GetBackupDefinition();
        backupDefinitions = backupDefinitions.Where(x => !(x.Host == definition.Host && x.FtpPort == definition.FtpPort)).ToList();
        backupDefinitions.Add(definition);
        string json = JsonSerializer.Serialize(backupDefinitions);

        RegistryUser settings = new() { KeyLocation = KeyLocation };
        settings.AddUnencryptedUserValue("Definitions", json);
        settings.AddUserValue("EmailApiKey", TextEmailApiKey.Password);
        if (CheckBoxAutoRun != null) settings.AddUserValue("AutoRun", (CheckBoxAutoRun.IsChecked ?? false).ToString());
        DropSelectedHost.ItemsSource = backupDefinitions.Select(x => x.Host + ":" + x.FtpPort);
        //DropSelectedHost.SelectedItem = definition.Host + ":" + definition.FtpPort;
    }

    private BackupDefinition GetBackupDefinition()
    {
        int.TryParse(TextSftpPort.Text, out int ftpPort);
        int.TryParse(TextMySqlPort.Text, out int sqlPort);
        BackupDefinition backupInfo = new()
        {
            Host = TextHostName.Text,
            FtpPort = ftpPort,
            FtpUsername = TextSftpUserName.Text,
            FtpPassword = TextSftpPassword.Password,
            ServerFolder = TextRemoteDirectory.Text,
            LocalFolder = TextLocalDirectory.Text,
            SqlPort = sqlPort,
            SqlUsername = TextMySqlUserName.Text,
            SqlPassword = TextMySqlPassword.Password
        };
        return backupInfo;
    }

    private void LoadSettings()
    {
        RegistryUser settings = new() { KeyLocation = KeyLocation };

        string definitions = settings.GetUnencryptedUserValue("Definitions");
        if (!string.IsNullOrWhiteSpace(definitions))
        {
            backupDefinitions = JsonSerializer.Deserialize<List<BackupDefinition>>(definitions) ?? [];
            backupDefinitions = backupDefinitions.OrderBy(x => x.Host).ToList();
        }

        if (backupDefinitions.Count == 0)
        {
            int.TryParse(settings.GetUserValue("SftpPort"), out int ftpPort);
            int.TryParse(settings.GetUserValue("MySqlPort"), out int sqlPort);
            BackupDefinition backupInfo = new()
            {
                Host = settings.GetUserValue("HostName"),
                FtpPort = ftpPort,
                FtpUsername = settings.GetUserValue("SftpUserName"),
                FtpPassword = settings.GetUserValue("SftpPassword"),
                ServerFolder = settings.GetUserValue("RemoteDirectory"),
                LocalFolder = settings.GetUserValue("LocalDirectory"),
                SqlPort = sqlPort,
                SqlUsername = settings.GetUserValue("MySqlUserName"),
                SqlPassword = settings.GetUserValue("MySqlPassword")
            };
            if (!string.IsNullOrWhiteSpace(backupInfo.Host))
                backupDefinitions.Add(backupInfo);
            settings.AddUnencryptedUserValue("Definitions", JsonSerializer.Serialize(backupDefinitions));
        }
        TextEmailApiKey.Password = settings.GetUserValue("EmailApiKey");
        bool.TryParse(settings.GetUserValue("AutoRun"), out bool autoRuns);
        CheckBoxAutoRun.IsChecked = autoRuns;
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
        TextEmailApiKey.Background = Brushes.White;
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
        if (string.IsNullOrWhiteSpace(TextEmailApiKey.Password))
        {
            TextEmailApiKey.Background = Brushes.LightCoral;
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

    private void CheckBoxAutoRun_Checked(object sender, RoutedEventArgs e) => autoRun = CheckBoxAutoRun.IsChecked == true;

    private void DropSelectedHost_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DropSelectedHost.SelectedIndex < 0) return;

        BackupDefinition backupInfo = backupDefinitions[DropSelectedHost.SelectedIndex];
        TextHostName.Text = backupInfo.Host;
        TextSftpUserName.Text = backupInfo.FtpUsername;
        TextSftpPassword.Password = backupInfo.FtpPassword;
        TextSftpPort.Text = backupInfo.FtpPort.ToString();
        TextMySqlUserName.Text = backupInfo.SqlUsername;
        TextMySqlPassword.Password = backupInfo.SqlPassword;
        TextMySqlPort.Text = backupInfo.SqlPort.ToString();
        TextRemoteDirectory.Text = backupInfo.ServerFolder;
        TextLocalDirectory.Text = backupInfo.LocalFolder;
    }

    private void ButtonDeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        backupDefinitions.RemoveAt(DropSelectedHost.SelectedIndex);
        DropSelectedHost.ItemsSource = backupDefinitions.Select(x => x.Host + ":" + x.FtpPort);
        if (backupDefinitions.Count > 0)
            DropSelectedHost.SelectedIndex = 0;
        else
            ButtonNew_Click(null, null);
        
        string json = JsonSerializer.Serialize(backupDefinitions);
        RegistryUser settings = new() { KeyLocation = KeyLocation };
        settings.AddUnencryptedUserValue("Definitions", json);
    }

    private void ButtonNew_Click(object? sender, RoutedEventArgs? e)
    {
        TextHostName.Text = "";
        TextSftpUserName.Text = "";
        TextSftpPassword.Password = "";
        TextSftpPort.Text = "22";
        TextMySqlUserName.Text = "";
        TextMySqlPassword.Password = "";
        TextMySqlPort.Text = "3306";
        TextRemoteDirectory.Text = "/var/backup";
        TextLocalDirectory.Text = @"C:\IspConfig Backups";
    }
}
