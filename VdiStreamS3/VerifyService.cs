using System;
using System.IO.Compression;

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

            using (var VerifyStream = UriStreamUtil.GetUriStream(Options))
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
