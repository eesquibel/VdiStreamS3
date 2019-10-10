using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
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
    public class RestoreService
    {
        RestoreOptions Options;

        public RestoreService(RestoreOptions options)
        {
            Options = options;
        }

        public int Restore()
        {
            VdiEngine BackupDevice = new VdiEngine();
            BackupDevice.CommandIssued += new EventHandler<CommandIssuedEventArgs>(BackupDevice_CommandIssued);
            BackupDevice.InfoMessageReceived += new EventHandler<InfoMessageEventArgs>(BackupDevice_InfoMessageReceived);

            using (var RestoreStream = GetUriStream())
            {
                using (DeflateStream CompressedRestoreStream = new DeflateStream(RestoreStream, CompressionMode.Decompress))
                {
                    DateTime Start = DateTime.Now;

                    BackupDevice.ExecuteCommand(string.Join(Environment.NewLine,
                        "ALTER DATABASE [" + Options.Database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                        "RESTORE DATABASE [" + Options.Database + "] FROM VIRTUAL_DEVICE = '{0}' WITH REPLACE, STATS = 1",
                        "ALTER DATABASE[" + Options.Database + "] SET MULTI_USER"
                        ), CompressedRestoreStream);

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
                        TransferUtility util = null;
                        var uri = new AmazonS3Uri(Options.Uri);

                        if (!string.IsNullOrEmpty(Options.AWSProfileName))
                        {
                            var sharedFile = new SharedCredentialsFile();
                            if (sharedFile.TryGetProfile(Options.AWSProfileName, out CredentialProfile profile))
                            {
                                if (AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out AWSCredentials credentials))
                                {
                                    var s3client = new AmazonS3Client(credentials, uri.Region);
                                    util = new TransferUtility(s3client);
                                }
                            }
                        }

                        if (util == null)
                        {
                            util = new TransferUtility(uri.Region);
                        }

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
