using System;
using SharedMethods;

namespace sftpObjects
{
    public class SortFileDefinition
    {
        public string SourceFileName = string.Empty;
        public string DestinationFileName = string.Empty;
        public string Format = string.Empty;
        public string BackupType = string.Empty;
        public int ReportedSize = 0;
        public string FileExtension = string.Empty;
        public DateTime LastModified = DateTime.MinValue;

        public void LoadDateFromEpoch(long epoch)
        {
            DateTime date = StaticMethods.Epoch2SDate(epoch);
            if (date > DateTime.MinValue)
            {
                LastModified = date;
            }
        }
    }
}
