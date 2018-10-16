using Amazon;
using CommandLine;

namespace VdiStreamS3.Model
{
    [Verb("Backup",  HelpText = "Backup a database to S3")]
    public class BackupOptions
    {
        [Option(HelpText = "Region your S3 bucket is in", Required = true)]
        public string Region { get; set; }

        [Option(HelpText = "Your S3 bucket name", Required = true)]
        public string Bucket { get; set; }

        [Option(HelpText = "The object key to store the backup at", Required = true)]
        public string Key { get; set; }

        [Option(HelpText = "The database to backup", Required = true)]
        public string Database { get; set; }

        [Option(HelpText = "The multipart upload part size, in MB", Default = 5)]
        public int PartSize { get; set; }

        public RegionEndpoint RegionEndpoint
        {
            get
            {
                return RegionEndpoint.GetBySystemName(Region);
            }
        }
    }
}
