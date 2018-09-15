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