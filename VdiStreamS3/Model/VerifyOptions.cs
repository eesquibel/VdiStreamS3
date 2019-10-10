using CommandLine;
using System;

namespace VdiStreamS3.Model
{
    [Verb("Verify", HelpText = "Verify a backup file")]
    public class VerifyOptions : IUriOptions
    {
        [Option(HelpText = "Uri of backup file to verify", Required = true)]
        public Uri Uri { get; set; }

        [Option(HelpText = "The AWS credential profile", Default = null)]
        public string AWSProfileName { get; set; }
    }
}
