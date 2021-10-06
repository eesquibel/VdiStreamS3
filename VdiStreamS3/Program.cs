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

using VdiDotNet;
using VdiStreamS3.Model;

namespace VdiStreamS3
{
    class Program
	{
		static void Main(string[] args)
		{
            Parser.Default.ParseArguments<BackupOptions, VerifyOptions, RestoreOptions>(args).MapResult
            (
                (BackupOptions options) =>
                {
                    var service = new BackupService(options);
                    return service.Backup();
                },
                (VerifyOptions options) =>
                {
                    var service = new VerifyService(options);
                    return service.Verify();
                },
                (RestoreOptions options) =>
                {
                    var service = new RestoreService(options);
                    return service.Restore();
                },
                errs => 1
            );

            Console.WriteLine("Done");
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