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

using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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
            AmazonS3Client s3client = null;

            if (! string.IsNullOrEmpty(Options.AWSProfileName))
            {
                var sharedFile = new SharedCredentialsFile();
                if (sharedFile.TryGetProfile(Options.AWSProfileName, out CredentialProfile profile))
                {
                    if (AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out AWSCredentials credentials))
                    {
                        s3client = new AmazonS3Client(credentials, Options.RegionEndpoint);
                    }
                }
            }

            if (s3client == null)
            {
                s3client = new AmazonS3Client(Options.RegionEndpoint);
            }

            //Backup the database			
            VdiEngine BackupDevice = new VdiEngine();
            BackupDevice.CommandIssued += new EventHandler<CommandIssuedEventArgs>(BackupDevice_CommandIssued);
            BackupDevice.InfoMessageReceived += new EventHandler<InfoMessageEventArgs>(BackupDevice_InfoMessageReceived);

            int partSize = Options.PartSize * 1024 * 1024;

            using (S3StreamWriter BackupStream = new S3StreamWriter(Options.Bucket, Options.Key, s3client, partSize))
            {
                using (DeflateStream CompressedBackupStream = new DeflateStream(BackupStream, CompressionMode.Compress))
                {
                    DateTime Start = DateTime.Now;
                    BackupDevice.ExecuteCommand("BACKUP DATABASE [" + Options.Database + "] TO VIRTUAL_DEVICE='{0}' WITH STATS = 1", CompressedBackupStream);
                    Console.WriteLine(DateTime.Now.Subtract(Start));
                }

                BackupStream.Close();
            }

            s3client.Dispose();

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
