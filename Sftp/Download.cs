using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatabaseMySql;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using SendgridConnector;
using Shared.Objects;

namespace Sftp;

public class Download
{
    BackupDefinition backupInfo;
    public string ApiKey;
    public string FromEmail = string.Empty;
    public string FromName = string.Empty;
    public string ToEmail = string.Empty;
    public string ToName = string.Empty;
    public string Message = string.Empty;

    private readonly List<SortDefinition> definitions = new();


    public Download(BackupDefinition info, string apiKey="")
    {
        //SendError();
        backupInfo = info;
        IspConfigDb db = new(backupInfo.Host, backupInfo.SqlUsername, backupInfo.SqlPassword, backupInfo.SqlPort);
        definitions.AddRange(db.GetWebDefinitions());
        definitions.AddRange(db.GetMailDefinitions());

        definitions.Add(new()
        {
            DestinationFolderName = "ISPConfig",
            SourceFolderName = "ispconfig_",
            UseWildcard = true,
            BackupType = "ISPConfig"
        });
    }

    public void StartDownload()
    {
        if (definitions.Count(x => !x.DestinationFolderName.ToUpper().Contains("ISPCONFIG")) == 0)
        {
            Debug.WriteLine("No Definitions Found");
            SendError("No SQL Domain Name Definitions Found", "No SQL Domain Name Definitions Found on " + backupInfo.Host);
            return;
        }

        try
        {
            using SftpClient sftp = new(backupInfo.Host, backupInfo.FtpPort, backupInfo.FtpUsername, backupInfo.FtpPassword);
            sftp.Connect();

            DownloadDirectory(sftp, backupInfo.ServerFolder, backupInfo.LocalFolder);

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

        List<ISftpFile> files = sftpClient.ListDirectory(sourceRemotePath).ToList();

        //foreach (SftpFile file in files.Where(file => file.Name is not ("." or "..")))
        Parallel.ForEach(files.Where(file => file.Name is not ("." or "..")), file =>
        {
            string currentDestinationPath = destPath;
            try
            {
                if (file.IsDirectory)
                {
                    SortDefinition definition = new();

                    //Exact Match
                    if (definitions.Any(x =>
                            file.Name.Equals(x.SourceFolderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        definition = definitions.First(x =>
                            file.Name.Equals(x.SourceFolderName, StringComparison.OrdinalIgnoreCase));
                        currentDestinationPath = Path.Combine(currentDestinationPath, definition.BackupType);
                        currentDestinationPath =
                            Path.Combine(currentDestinationPath, definition.DestinationFolderName);
                        currentDestinationPath = DestinationPathAddDate(currentDestinationPath, file);
                    }
                    //Wildcard like the ISPConfig Folder
                    else if (definitions.Any(x =>
                                 file.Name.StartsWith(x.SourceFolderName, StringComparison.OrdinalIgnoreCase) &&
                                 x.UseWildcard))
                    {
                        definition = definitions.First(x =>
                            file.Name.StartsWith(x.SourceFolderName, StringComparison.OrdinalIgnoreCase));
                        currentDestinationPath =
                            Path.Combine(currentDestinationPath, definition.DestinationFolderName);
                        currentDestinationPath = DestinationPathAddDate(currentDestinationPath, file);
                    }
                    //if Somehow there's no Definition
                    else
                    {
                        currentDestinationPath = Path.Combine(currentDestinationPath, file.Name);
                    }

                    DownloadDirectory(sftpClient, file.FullName, currentDestinationPath, definition.Files);
                }
                else
                {
                    string destFilePath;

                    string fileNme = file.Name;
                    if (fileNme.StartsWith("manual", StringComparison.OrdinalIgnoreCase))
                    {
                        fileNme = fileNme[7..];
                    }

                    SortFileDefinition? definition = fileDefinitions.FirstOrDefault(x =>
                        fileNme.Equals(x.SourceFileName, StringComparison.OrdinalIgnoreCase));
                    if (definition == null && fileDefinitions.Count > 0)
                    {
                        string[] fileNameSplit = fileNme.Split('_');
                        if (fileNameSplit.Length > 1)
                            definition = fileDefinitions.FirstOrDefault(x =>
                                (fileNameSplit[0] + "_" + fileNameSplit[1]).Equals(x.SourceFileName,
                                    StringComparison.OrdinalIgnoreCase));
                    }

                    if (definition != null)
                    {
                        currentDestinationPath =
                            Path.Combine(currentDestinationPath, definition.DestinationFileName);

                        if (definition.BackupType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                            currentDestinationPath = Path.Combine(currentDestinationPath, "MySQL");


                        destFilePath = DestinationPathAddDate(currentDestinationPath, file);
                        destFilePath += "." + definition.FileExtension;
                    }
                    else
                    {
                        destFilePath = Path.Combine(currentDestinationPath, file.Name);
                    }

                    Directory.CreateDirectory(currentDestinationPath);

                    if (File.Exists(destFilePath) && new FileInfo(destFilePath).Length == file.Length) return;

                    using Stream fileStream = File.Create(destFilePath);
                    sftpClient.DownloadFile(file.FullName, fileStream);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
        //}
    }

    public static string DestinationPathAddDate(string destFilePath, ISftpFile file)
    {
        DateTime time = SeparateDateTime(file);

        if (time > DateTime.MinValue)
        {
            destFilePath = Path.Combine(destFilePath, time.ToString("yyyy-MM-dd_HH-mm"));
        }

        return destFilePath;
    }

    public static DateTime SeparateDateTime(ISftpFile file)
    {
        string fileNme = file.Name;
        if (fileNme.StartsWith("manual", StringComparison.OrdinalIgnoreCase))
        {
            fileNme = fileNme[7..];
        }

        string[] names = fileNme.Split('_').Select(x => x.Split('.')[0]).Reverse().ToArray();

        DateTime time = DateTime.MinValue;
        if (names.Length > 2)
        {
            DateTime.TryParse(names[1] + " " + names[0].Replace('-', ':'), out time);
        }

        return time;
    }

    public void SendError(string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return;

        SendEmail email = new(ApiKey)
        {
            FromEmail = "BackupUser@sharkbytecomputers.com",
            ToEmail = "sharkbytecomputer@gmail.com",
            Subject = subject,
            Message = message
        };
        email.SendMessage();
    }
}
