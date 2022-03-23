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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VdiStreamS3
{
    class S3StreamWriter : Stream, IDisposable
    {
        private struct UploadPart
        {
            public int PartNumber;
            public byte[] Buffer;
            public long PartSize;

            public UploadPart(int partNumber, MemoryStream input)
            {
                PartNumber = partNumber;
                PartSize = input.Length;
                Buffer = new byte[input.Length];
                input.Seek(0, SeekOrigin.Begin);
                input.Read(Buffer, 0, Buffer.Length);
                input.Close();
                input.Dispose();
            }
        }

        private MemoryStream Buffer;

        private long Offset;

        private ConcurrentBag<UploadPartResponse> partResponses;

        public readonly AmazonS3Client S3Client;

        private ConcurrentQueue<UploadPart> UploadQueue;

        public string UploadId { get; private set; }

        public string Bucket { get; private set; }

        public string Key { get; private set; }

        public int Part { get; private set; }

        public int PartSize { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        private long length = 0;

        public override long Length => length;

        public override long Position { get => Offset; set => Offset = (int)value; }

        private int inFlight = 0;

        public S3StreamWriter(string bucket, string key) : this(bucket, key, 5242880)
        {
        }

        public S3StreamWriter(string bucket, string key, int size) : this(bucket, key, new AmazonS3Client(), size)
        {
        }

        public S3StreamWriter(string bucket, string key, AmazonS3Client s3client) : this(bucket, key, s3client, 5242880)
        {
        }

        public S3StreamWriter(string bucket, string key, AmazonS3Client s3client, int size) : base()
        {
            if (size < 5242880)
            {
                size = 5242880;
            }

            PartSize = size;

            Buffer = new MemoryStream(size);
            Offset = 0;
            Part = 0;

            S3Client = s3client;
            Bucket = bucket;
            Key = key;

            UploadQueue = new ConcurrentQueue<UploadPart>();
        }

        private void Start()
        {
            partResponses = new ConcurrentBag<UploadPartResponse>();

            var request = new InitiateMultipartUploadRequest
            {
                BucketName = Bucket,
                CannedACL = S3CannedACL.BucketOwnerFullControl,
                Key = Key
            };

            var response = S3Client.InitiateMultipartUpload(request);

            UploadId = response.UploadId;
        }

        public override void Flush()
        {
            while (UploadQueue.Count > 0 || inFlight > 0)
            {
                Thread.Sleep(3000);
                Dequeue();
            }
        }

        private void Dequeue()
        {
            if (string.IsNullOrEmpty(UploadId))
            {
                Start();
            }

            if (inFlight >= 4)
            {
                return;
            }

            if (UploadQueue.TryDequeue(out var part))
            {
                UploadPartAsync(part).ContinueWith(task =>
                {
                    part.Buffer = null;
                    Interlocked.Decrement(ref inFlight);
                });
            }
        }

        private async Task UploadPartAsync(UploadPart part)
        {
            Interlocked.Increment(ref inFlight);

            Console.WriteLine($"UploadPart {part.PartNumber} {part.PartSize}");

            while (true)
            {
                try
                {
                    using (var stream = new MemoryStream(part.Buffer, 0, (int)part.PartSize, false))
                    {
                        var request = new UploadPartRequest()
                        {
                            BucketName = Bucket,
                            Key = Key,
                            UploadId = UploadId,
                            PartNumber = part.PartNumber,
                            PartSize = part.PartSize,
                            InputStream = stream
                        };

                        var response = await S3Client.UploadPartAsync(request);

                        partResponses.Add(response);
                    }

                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(3000);
                }
            }

            Dequeue();
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
            throw new NotImplementedException();
        }

        private void Enqueue()
        {
            Part += 1;
            var upload = new UploadPart(partNumber: Part, input: Buffer);
            UploadQueue.Enqueue(upload);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (Buffer)
            {
                int total = buffer.Length - offset;
                length += Math.Min(total, count);
                Offset = length;
                Buffer.Write(buffer, offset, count);

                if (Buffer.Length >= PartSize)
                {
                    Enqueue();
                    Buffer = new MemoryStream(PartSize);
                    while (inFlight >= 3)
                    {
                        Thread.Sleep(3000);
                    }

                    if (UploadQueue.Count > 0)
                    {
                        Dequeue();
                    }
                }
            }
        }

        public override void Close()
        {
            if (Buffer.CanRead)
            {

                if (string.IsNullOrEmpty(UploadId))
                {
                    if (Buffer.Length == 0)
                    {
                        Buffer.Close();
                        base.Close();
                        return;
                    }
                }

                Console.WriteLine("Close");

                if (Buffer.Length > 0)
                {
                    Enqueue();
                }
            }

            Flush();

            var request = new CompleteMultipartUploadRequest()
            {
                BucketName = Bucket,
                Key = Key,
                UploadId = UploadId,
                PartETags = null
            };

            request.AddPartETags(partResponses.OrderBy(part => part.PartNumber));

            S3Client.CompleteMultipartUpload(request);

            base.Close();
        }
    }
}
