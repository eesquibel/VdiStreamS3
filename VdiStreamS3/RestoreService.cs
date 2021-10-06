/*
Copyright 2019 Eric Esquibel
    
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    
http://www.apache.org/licenses/LICENSE-2.0
    
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
