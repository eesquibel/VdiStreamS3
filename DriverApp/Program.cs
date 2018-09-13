using Amazon;
using Amazon.S3;
using System;
using System.IO;
using System.IO.Compression;
using VdiDotNet;

namespace DriverApp
{
    class Program
	{
		static void Main(string[] args)
		{
            var s3client = new AmazonS3Client(RegionEndpoint.USEast1);
            var bucket = "bucket";

            //Backup the database			
            VdiEngine BackupDevice = new VdiEngine();
			BackupDevice.CommandIssued += new EventHandler<CommandIssuedEventArgs>(BackupDevice_CommandIssued);
			BackupDevice.InfoMessageReceived += new EventHandler<InfoMessageEventArgs>(BackupDevice_InfoMessageReceived);

            // using (FileStream BackupStream = new FileStream(@"E:\Sites\AdventureWorks534PM.bak", FileMode.Create, FileAccess.Write, FileShare.None, 1048576))
            using (S3StreamWriter BackupStream = new S3StreamWriter(bucket, "AdventureWorks" + DateTime.Now.ToShortTimeString().Replace(" ", "").Replace(":", "") + ".bak", s3client))
            {
                using (DeflateStream CompressedBackupStream = new DeflateStream(BackupStream, CompressionMode.Compress))
                {
                    DateTime Start = DateTime.Now;
                    BackupDevice.ExecuteCommand("BACKUP DATABASE AdventureWorks TO VIRTUAL_DEVICE='{0}' WITH STATS = 1", CompressedBackupStream);
                    Console.WriteLine(DateTime.Now.Subtract(Start));
                }

                BackupStream.Close();
            }


/*
            using (FileStream RestoreStream = new FileStream(@"E:\Sites\AdventureWorks600PM.bak", FileMode.Open, FileAccess.Read, FileShare.None, 1048576))
            {
                using (DeflateStream CompressedRestoreStream = new DeflateStream(RestoreStream, CompressionMode.Decompress))
                {
                    DateTime Start = DateTime.Now;
                    BackupDevice.ExecuteCommand("RESTORE VERIFYONLY FROM VIRTUAL_DEVICE = '{0}' WITH STATS = 1", CompressedRestoreStream);
                    Console.WriteLine(DateTime.Now.Subtract(Start));
                }
            }
*/

            Console.WriteLine("Done");
			Console.ReadLine();
		}

		static void BackupDevice_InfoMessageReceived(object sender, InfoMessageEventArgs e)
		{
			Console.WriteLine(e.Message);
		}

		static void BackupDevice_CommandIssued(object sender, CommandIssuedEventArgs e)
		{
			Console.WriteLine(e.Command);
		}

	}
}