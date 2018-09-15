using Amazon.S3.Transfer;
using Amazon.S3.Util;

using System;
using System.IO;
using System.IO.Compression;
using System.Net;

using VdiDotNet;
using VdiStreamS3.Model;

namespace VdiStreamS3
{
    public class VerifyService
    {
        VerifyOptions Options;

        public VerifyService(VerifyOptions options)
        {
            Options = options;
        }

        public int Verify()
        {
            VdiEngine BackupDevice = new VdiEngine();
            BackupDevice.CommandIssued += new EventHandler<CommandIssuedEventArgs>(BackupDevice_CommandIssued);
            BackupDevice.InfoMessageReceived += new EventHandler<InfoMessageEventArgs>(BackupDevice_InfoMessageReceived);

            using (var VerifyStream = GetUriStream())
            {
                using (DeflateStream CompressedRestoreStream = new DeflateStream(VerifyStream, CompressionMode.Decompress))
                {
                    DateTime Start = DateTime.Now;
                    BackupDevice.ExecuteCommand("RESTORE VERIFYONLY FROM VIRTUAL_DEVICE = '{0}' WITH STATS = 1", CompressedRestoreStream);
                    Console.WriteLine(DateTime.Now.Subtract(Start));
                }
            }

            return 0;
        }

        private Stream GetUriStream()
        {
            switch (Options.Uri.Scheme)
            {
                case "file":
                    return new FileStream(Options.Uri.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.None, 1048576);
                case "http":
                case "https":
                    if (Options.Uri.Host.EndsWith("amazonaws.com"))
                    {
                        var uri = new AmazonS3Uri(Options.Uri);
                        var util = new TransferUtility(uri.Region);
                        return util.OpenStream(uri.Bucket, uri.Key);
                    } else
                    {
                        var request = WebRequest.Create(Options.Uri);
                        return request.GetRequestStream();
                    }
                default:
                    throw new FileNotFoundException("Cannot parse Uri", Options.Uri.AbsoluteUri);
            }
            
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
