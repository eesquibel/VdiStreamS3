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

        [Option(HelpText = "The AWS credential profile", Default = null)]
        public string AWSProfileName { get; set; }

        public RegionEndpoint RegionEndpoint
        {
            get
            {
                return RegionEndpoint.GetBySystemName(Region);
            }
        }
    }
}
