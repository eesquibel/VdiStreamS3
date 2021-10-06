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

using CommandLine;
using System;

namespace VdiStreamS3.Model
{
    [Verb("Restore", HelpText = "Restore a backup file")]
    public class RestoreOptions : IUriOptions
    {
        [Option(HelpText = "The database to backup", Required = true)]
        public string Database { get; set; }

        [Option(HelpText = "Uri of backup file to verify", Required = true)]
        public Uri Uri { get; set; }

        [Option(HelpText = "The AWS credential profile", Default = null)]
        public string AWSProfileName { get; set; }
    }
}
