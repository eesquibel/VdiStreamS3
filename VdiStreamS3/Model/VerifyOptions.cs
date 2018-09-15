using CommandLine;
using System;

namespace VdiStreamS3.Model
{
    [Verb("Verify", HelpText = "Verify a backup file")]
    public class VerifyOptions
    {
        [Option(HelpText = "Uri of backup file to verify", Required = true)]
        public Uri Uri { get; set; }
    }
}
