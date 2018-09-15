using Amazon.S3;

using System;
using System.IO.Compression;

using VdiDotNet;
using VdiStreamS3.Model;

namespace VdiStreamS3
{
    public class BackupService
    {
        BackupOptions Options;

        public BackupService(BackupOptions options)
        {
            Options = options;
        }

        public int Backup()
        {
            var s3client = new AmazonS3Client(Options.RegionEndpoint);

            //Backup the database			
            VdiEngine BackupDevice = new VdiEngine();
            BackupDevice.CommandIssued += new EventHandler<CommandIssuedEventArgs>(BackupDevice_CommandIssued);
            BackupDevice.InfoMessageReceived += new EventHandler<InfoMessageEventArgs>(BackupDevice_InfoMessageReceived);

            using (S3StreamWriter BackupStream = new S3StreamWriter(Options.Bucket, Options.Key, s3client))
            {
                using (DeflateStream CompressedBackupStream = new DeflateStream(BackupStream, CompressionMode.Compress))
                {
                    DateTime Start = DateTime.Now;
                    BackupDevice.ExecuteCommand("BACKUP DATABASE [" + Options.Database + "] TO VIRTUAL_DEVICE='{0}' WITH STATS = 1", CompressedBackupStream);
                    Console.WriteLine(DateTime.Now.Subtract(Start));
                }

                BackupStream.Close();
            }

            return 0;
        }

        private void BackupDevice_InfoMessageReceived(object sender, InfoMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private void BackupDevice_CommandIssued(object sender, CommandIssuedEventArgs e)
        {
            Console.WriteLine(e.Command);
        }
    }
}
