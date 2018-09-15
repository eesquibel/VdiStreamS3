using CommandLine;
using System;

namespace VdiStreamS3.Model
{
    [Verb("Restore", HelpText = "Restore a backup file")]
    public class RestoreOptions
    {
        [Option(HelpText = "The database to backup", Required = true)]
        public string Database { get; set; }

        [Option(HelpText = "Uri of backup file to verify", Required = true)]
        public Uri Uri { get; set; }
    }
}
