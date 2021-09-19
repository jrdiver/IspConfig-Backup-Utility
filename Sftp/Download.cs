using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatabaseMySql;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using sftpObjects;

namespace Sftp
{
    public class Download
    {
        public string Host;
        public int FtpPort;
        public string FtpUsername;
        public string FtpPassword;
        public string ServerFolder;
        public string LocalFolder;
        public int SqlPort;
        public string SqlUsername;
        public string SqlPassword;
        private List<SortDefinition> definitions = new();


        public Download(string host, string ftpUsername, string ftpPassword, string serverFolder, string localFolder, string sqlUsername, string sqlPassword, int ftpPort = 22, int sqlPort = 3306)
        {
            Host = host;
            FtpPort = ftpPort;
            FtpUsername = ftpUsername;
            FtpPassword = ftpPassword;
            ServerFolder = serverFolder;
            LocalFolder = localFolder;
            SqlPort = sqlPort;
            SqlUsername = sqlUsername;
            SqlPassword = sqlPassword;
            IspConfigDb db = new(host, SqlUsername, SqlPassword, SqlPort);
            definitions.AddRange(db.GetWebDefinitions());
        }

        public void StartDownload()
        {
            try
            {
                using SftpClient sftp = new(Host, FtpPort, FtpUsername, FtpPassword);
                sftp.Connect();

                DownloadDirectory(sftp, ServerFolder, LocalFolder);

                sftp.Disconnect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void DownloadDirectory(SftpClient sftpClient, string sourceRemotePath, string destPath, List<SortFileDefinition>? fileDefinitions = null)
        {
            fileDefinitions ??= new List<SortFileDefinition>();

            List<SftpFile> files = sftpClient.ListDirectory(sourceRemotePath).ToList();

            //foreach (SftpFile file in files.Where(file => file.Name is not ("." or "..")))
            Parallel.ForEach(files.Where(file => file.Name is not ("." or "..")), file =>
            {
                if (file.IsDirectory)
                {
                    SortDefinition definition = new ();

                    if (definitions.Any(x => file.Name.Equals(x.SourceFolderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        definition = definitions.First(x => file.Name.Equals(x.SourceFolderName, StringComparison.OrdinalIgnoreCase));
                        destPath = Path.Combine(destPath, definition.DestinationFolderName);
                        destPath = DestinationPathAddDate(destPath, file);
                    }

                    DownloadDirectory(sftpClient, file.FullName, destPath, definition.Files);
                }
                else
                {
                    string destFilePath;
                    string destFolderPath = destPath;

                    string fileNme = file.Name;
                    if (fileNme.StartsWith("manual", StringComparison.OrdinalIgnoreCase))
                    {
                        fileNme = fileNme.Substring(7);
                    }

                    if (fileNme.Contains("roundcube", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("Found");
                    }

                    SortFileDefinition? definition = fileDefinitions.FirstOrDefault(x => fileNme.Split('_')[0].Equals(x.SourceFileName, StringComparison.OrdinalIgnoreCase));
                    if (definition == null && fileDefinitions.Count > 0)
                    {
                        string[] fileNameSplit = fileNme.Split('_');
                        if (fileNameSplit.Length > 1)
                            definition = fileDefinitions.FirstOrDefault(x => (fileNameSplit[0] + "_" + fileNameSplit[1]).Equals(x.SourceFileName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (definition != null)
                    {
                        destFolderPath = Path.Combine(destPath, definition.DestinationFileName);

                        destFilePath = DestinationPathAddDate(destFolderPath, file);
                        destFilePath += definition.FileExtension;
                    }
                    else
                    {
                        destFilePath = Path.Combine(destFolderPath, file.Name);
                    }

                    Directory.CreateDirectory(destFolderPath);

                    if (File.Exists(destFilePath) && new FileInfo(destFilePath).Length == file.Length) return;

                    using Stream fileStream = File.Create(destFilePath);
                    sftpClient.DownloadFile(file.FullName, fileStream);

                }
            });
        }

        public static string DestinationPathAddDate(string destFilePath, SftpFile file)
        {
            DateTime time = SeparateDateTime(file);

            if (time > DateTime.MinValue)
            {
                destFilePath = Path.Combine(destFilePath, time.ToString("yyyy-MM-dd_HH-mm"));
            }

            return destFilePath;
        }

        public static DateTime SeparateDateTime(SftpFile file)
        {
            string fileNme = file.Name;
            if (fileNme.StartsWith("manual", StringComparison.OrdinalIgnoreCase))
            {
                fileNme = fileNme.Substring(7);
            }

            string[] names = fileNme.Split('_').Select(x => x.Split('.')[0]).Reverse().ToArray();

            DateTime time = DateTime.MinValue;
            if (names.Length > 2)
            {
                DateTime.TryParse(names[1] + " " + names[0].Replace('-', ':'), out time);
            }

            return time;
        }
    }
}
