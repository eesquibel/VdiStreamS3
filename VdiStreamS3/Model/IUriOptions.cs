using System;

namespace VdiStreamS3.Model
{
    public interface IUriOptions
    {
        Uri Uri { get; set; }
        
        string AWSProfileName { get; set; }
    }
}
