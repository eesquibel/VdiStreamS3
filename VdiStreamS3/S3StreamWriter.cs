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

using Amazon.S3;
using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;

namespace VdiStreamS3
{
    class S3StreamWriter : Stream, IDisposable
    {
        private byte[] Buffer;

        private int Offset;

        private List<UploadPartResponse> partResponses;

        public AmazonS3Client S3Client;

        public string UploadId { get; private set; }

        public string Bucket { get; private set; }

        public string Key { get; private set; }

        public int Part { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => Buffer.LongLength;

        public override long Position { get => Offset; set => Offset = (int)value; }

        public S3StreamWriter(string bucket, string key) : this(bucket, key, 5242880)
        {
        }

        public S3StreamWriter(string bucket, string key, int size) : base()
        {
            if (size < 5242880)
            {
                size = 5242880;
            }

            Buffer = new byte[size];
            Offset = 0;

            S3Client = new AmazonS3Client();
            Bucket = bucket;
            Key = key;
        }

        public S3StreamWriter(string bucket, string key, AmazonS3Client s3client, int size) : base()
        {
            if (size < 5242880)
            {
                size = 5242880;
            }

            Buffer = new byte[size];
            Offset = 0;

            S3Client = s3client;
            Bucket = bucket;
            Key = key;
        }

        public S3StreamWriter(string bucket, string key, AmazonS3Client s3client) : this(bucket, key, s3client, 5242880)
        {
        }

        private void Start()
        {
            partResponses = new List<UploadPartResponse>();

            var request = new InitiateMultipartUploadRequest
            {
                BucketName = Bucket,
                CannedACL = S3CannedACL.BucketOwnerFullControl,
                Key = Key
            };

            var response = S3Client.InitiateMultipartUpload(request);

            UploadId = response.UploadId;
            Part = 0;
        }

        public override void Flush()
        {
            if (Offset < Buffer.Length)
            {
                return;
            }

            if (string.IsNullOrEmpty(UploadId))
            {
                Start();
            }

            if (UploadPart(Buffer, 0, Offset))
            {
                Buffer.Initialize();
                Offset = 0;
            }
        }

        private bool UploadPart(byte[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return false;
            }

            Part += 1;

            Console.WriteLine($"UploadPart {Part} {count}");

            using (var stream = new MemoryStream(buffer, index, count, false))
            {
                var request = new UploadPartRequest()
                {
                    BucketName = Bucket,
                    Key = Key,
                    UploadId = UploadId,
                    PartNumber = Part,
                    PartSize = count,
                    InputStream = stream
                };

                var response = S3Client.UploadPart(request);

                partResponses.Add(response);
            }

            return true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            byte[] buffer = new byte[value];
            Array.Copy(Buffer, buffer, value);
            if (value < Offset)
            {
                Offset = (int)value;
            }

            Buffer = buffer;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (Buffer)
            {
                if (Offset + count < Buffer.Length)
                {
                    Array.Copy(buffer, offset, Buffer, Offset, count);
                    Offset += count;
                }
                else
                {
                    // Calc space remaining in current buffer
                    int free = Buffer.Length - Offset;
                    if (free > count)
                    {
                        free = count;
                    }

                    // Append to the current buffer
                    Array.Copy(buffer, offset, Buffer, Offset, free);
                    Offset += free;
                    offset += free;
                    count -= free;

                    // Flush the buffer
                    Flush();

                    // While buffer isn't empty
                    while (count > 0)
                    {
                        // Upload every full part until the size is less than the Buffer size
                        if (count >= Buffer.Length)
                        {
                            UploadPart(buffer, offset, Buffer.Length);
                            offset += Buffer.Length;
                            count -= Buffer.Length;
                        }
                        // Then append the remaining chuck to the Buffer
                        else
                        {
                            Array.Copy(buffer, offset, Buffer, Offset, count);
                            Offset += count;
                            break;
                        }
                    }
                }
            }
        }

        public override void Close()
        {
            if (!string.IsNullOrEmpty(UploadId))
            {
                Console.WriteLine("Close");

                UploadPart(Buffer, 0, Offset);

                var request = new CompleteMultipartUploadRequest()
                {
                    BucketName = Bucket,
                    Key = Key,
                    UploadId = UploadId,
                    PartETags = null
                };

                request.AddPartETags(partResponses);

                var response = S3Client.CompleteMultipartUpload(request);

                UploadId = null;
                partResponses.Clear();
                Offset = 0;
                Buffer.Initialize();
            }

            base.Close();
        }
    }
}
