using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharedMethods;

namespace BackupFileManagement
{
    public class BackupScheme
    {
        public string FolderPath;
        public int KeepAllFilesDays = 7;
        public int KeepFirstOfMonth = 2;
        public int DeleteOlderThenMonths = 12;

        public BackupScheme(string localFolder)
        {
            FolderPath = localFolder;
        }

        public void CleanBackupFolder()
        {
            if (Directory.Exists(FolderPath))
            {
                ProcessDirectory(FolderPath);
            }
        }

        private void ProcessDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                string[] folders = Directory.GetDirectories(path);
                CleanFiles(path);
                foreach (string folder in folders)
                {
                    if (Path.GetFileName(folder).Equals("IspConfig", StringComparison.OrdinalIgnoreCase))
                    {
                        CleanIspConfigFolder(folder);
                    }
                    ProcessDirectory(folder);
                }
            }
        }

        private void CleanIspConfigFolder(string path)
        {
            string[] files = Directory.GetDirectories(path);
            List<FileCleanupInfo> deleteList = CalculateDeletes(files);

            if (deleteList.Count <= 0) return;

            Parallel.ForEach(deleteList.Where(file => Directory.Exists(file.FilePath)), file =>
            {
                Directory.Delete(file.FilePath,  true);
            });
        }

        private void CleanFiles(string path)
        {
            string[] files = Directory.GetFiles(path);
            List<FileCleanupInfo> deleteList = CalculateDeletes(files);

            if (deleteList.Count <= 0) return;

            Parallel.ForEach(deleteList.Where(file => File.Exists(file.FilePath)), file =>
           {
               File.Delete(file.FilePath);
           });
        }

        private List<FileCleanupInfo> CalculateDeletes(string[] files)
        {
            List<FileCleanupInfo> deleteList = new();

            ConcurrentBag<FileCleanupInfo> concurrentFileList = new();
            Parallel.ForEach(files, file =>
            {
                DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file).Split(".")[0], "yyyy-M-d_HH-mm", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
                FileCleanupInfo currentFile = new() { FilePath = file, BackupTime = date };
                concurrentFileList.Add(currentFile);
            });

            List<FileCleanupInfo> fileList = concurrentFileList.Where(x => x.BackupTime != DateTime.MinValue).OrderBy(x => x.BackupTime).ToList();
            if (fileList.Count < 2) return deleteList;

            //duplicates for single day
            List<DateTime> dateList = fileList.GroupBy(x => x.BackupTime.Date).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (dateList.Count > 0)
            {
                foreach (List<FileCleanupInfo> currentFiles in dateList.Select(date => fileList.Where(x => x.BackupTime.Date == date).ToList()).Where(currentFiles => currentFiles.Count > 1))
                {
                    deleteList.AddRange(currentFiles.OrderBy(x => x.BackupTime).SkipLast(1));
                }
            }

            fileList = fileList.Except(deleteList).ToList();

            //over a year
            if (fileList.Count > 1)
            {
                List<FileCleanupInfo> tooOld = fileList.Where(x => x.BackupTime.Date < DateTime.Now.AddDays(DateTime.Now.Day * -1).AddMonths(DeleteOlderThenMonths * -1).Date).ToList();
                if (tooOld.Count > 0 && fileList.Count > tooOld.Count)
                {
                    deleteList.AddRange(tooOld);
                }
                else if (tooOld.Count > 0 && fileList.Count == tooOld.Count)
                {
                    deleteList.AddRange(tooOld.OrderBy(x => x.BackupTime).SkipLast(1));
                }
            }

            //over 2 months
            fileList = fileList.Except(deleteList).ToList();
            if (fileList.Count > 1)
            {
                var monthBackups = fileList.Where(x => x.BackupTime.Date < DateTime.Now.AddMonths(KeepFirstOfMonth * -1).AddDays(((int)DateTime.Now.AddMonths(KeepFirstOfMonth * -1).DayOfWeek + 1) * -1)).GroupBy(x => new { month = x.BackupTime.Month, x.BackupTime.Year }).Where(x => x.Count() > 1).ToList();
                if (monthBackups.Count > 0)
                {
                    foreach (var month in monthBackups.Where(week => week.Count() > 1))
                    {
                        deleteList.AddRange(month.OrderBy(x => x.BackupTime).Skip(1));
                    }
                }
            }

            fileList = fileList.Except(deleteList).ToList();

            //over 7 days
            if (fileList.Count > 1)
            {
                var weekBackups = fileList.Where(x => x.BackupTime.Date < DateTime.Now.AddDays(KeepAllFilesDays * -1)).GroupBy(x => new { week = StaticMethods.GetWeekNumber(x.BackupTime.Date), x.BackupTime.Year }).Where(x => x.Count() > 1).ToList();
                if (weekBackups.Count > 0)
                {
                    foreach (var week in weekBackups.Where(week => week.Count() > 1))
                    {
                        if (week.Key.week.Equals(52) && week.Key.Year.Equals(2020))
                        {
                            Debug.WriteLine("date?");
                        }

                        if (week.Key.week.Equals(52))
                        {
                            List<FileCleanupInfo> jan = week.Where(x => x.BackupTime.Month == 1).ToList();
                            List<FileCleanupInfo> dec = week.Where(x => x.BackupTime.Month == 12).ToList();

                            deleteList.AddRange(jan.OrderBy(x => x.BackupTime).Skip(1));
                            deleteList.AddRange(dec.OrderBy(x => x.BackupTime).Skip(1));
                        }
                        else
                        {
                            deleteList.AddRange(week.OrderBy(x => x.BackupTime).Skip(1));
                        }
                    }
                }
            }

            return deleteList;
        }
    }
}
