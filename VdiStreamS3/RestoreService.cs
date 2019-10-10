using System;
using System.IO.Compression;

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

            using (var RestoreStream = UriStreamUtil.GetUriStream(Options))
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
