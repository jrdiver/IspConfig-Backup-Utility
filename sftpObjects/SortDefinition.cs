using System.Collections.Generic;

namespace sftpObjects
{
    public class SortDefinition
    {
        public string SourceFolderName = string.Empty;
        public string DestinationFolderName = string.Empty;
        public string DomainName = string.Empty;
        public string User = string.Empty;
        public string BackupType = string.Empty;
        public bool UseWildcard = false;
        public List<SortFileDefinition> Files = new();
    }
}