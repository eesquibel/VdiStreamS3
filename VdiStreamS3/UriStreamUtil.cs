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
using Amazon.S3.Transfer;
using Amazon.S3.Util;

using System.IO;
using System.Net;

using VdiStreamS3.Model;

namespace VdiStreamS3
{
    public static class UriStreamUtil
    {
        public static Stream GetUriStream(IUriOptions Options)
        {
            switch (Options.Uri.Scheme)
            {
                case "file":
                    return new FileStream(Options.Uri.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.None, 1048576);
                case "http":
                case "https":
                    if (Options.Uri.Host.EndsWith("amazonaws.com"))
                    {
                        TransferUtility util = null;
                        var uri = new AmazonS3Uri(Options.Uri);

                        if (!string.IsNullOrEmpty(Options.AWSProfileName))
                        {
                            var sharedFile = new SharedCredentialsFile();
                            if (sharedFile.TryGetProfile(Options.AWSProfileName, out CredentialProfile profile))
                            {
                                if (AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out AWSCredentials credentials))
                                {
                                    var s3client = new AmazonS3Client(credentials, uri.Region);
                                    util = new TransferUtility(s3client);
                                }
                            }
                        }

                        if (util == null)
                        {
                            util = new TransferUtility(uri.Region);
                        }

                        return util.OpenStream(uri.Bucket, uri.Key);
                    }
                    else
                    {
                        var request = WebRequest.Create(Options.Uri);
                        return request.GetRequestStream();
                    }
                default:
                    throw new FileNotFoundException("Cannot parse Uri", Options.Uri.AbsoluteUri);
            }
        }
    }
}
