using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace CalendarStorage.CustomFormatters
{
    public class BinaryInputFormatter : InputFormatter
    {
        public readonly int MaxPayloadLengthApprox;
        private readonly bool UseLocking;
        private readonly SemaphoreSlim Semaphore;

        public BinaryInputFormatter(int maxPayloadLengthApprox, bool useLocking)
        {
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/octet-stream"));

            this.MaxPayloadLengthApprox = maxPayloadLengthApprox;
            this.UseLocking = useLocking;
            if (useLocking)
            {
                this.Semaphore = new SemaphoreSlim(1, 1);
            }
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            int length;
            try
            {
                checked
                {
                    // Headers.Content-Length should always be set, and it's value should be asserted by kestrel... but who knows...
                    length = (int)(context.HttpContext.Request.ContentLength ?? this.MaxPayloadLengthApprox);
                }
            }
            catch (Exception)
            {
                return await InputFormatterResult.FailureAsync();
            }

            if (length > this.MaxPayloadLengthApprox)
            {
                return await InputFormatterResult.FailureAsync();
            }

            if (this.UseLocking)
            {
                await this.Semaphore.WaitAsync(); // todo timeout?
            }

            byte[] data;

            try
            {
                data = new byte[length];
                int read = await TryReadMaxFromStream(context.HttpContext.Request.Body, data, length >= this.MaxPayloadLengthApprox);
                if (read == -1 || read == 0)
                {
                    return await InputFormatterResult.FailureAsync();
                }

                if (read != length)
                {
                    byte[] resized = new byte[read];
                    Array.Copy(data, resized, read);
                    data = resized;
                }
            }
            catch (Exception)
            {
                return await InputFormatterResult.FailureAsync();
            }
            finally
            {
                if (this.UseLocking)
                {
                    this.Semaphore.Release();
                }
            }
            
            return await InputFormatterResult.SuccessAsync(data);
        }
        
        /// <returns>actual bytes read, or -1 if the source length exceeded the buffer size</returns>
        private static async Task<int> TryReadMaxFromStream(Stream source, byte[] buffer, bool maxCheck=true)
        {
            int length = buffer.Length;
            int read = 0;
            while (read < length)
            {
                int newRead = await source.ReadAsync(buffer, read, length - read);
                if (newRead == 0)
                {
                    break;
                }

                read += newRead;
            }

            if (maxCheck)
            {
                if (read == length)
                {
                    // reached max length and there might still be data left to read
                    byte[] buf = new byte[1];
                    if (await source.ReadAsync(buf, 0, 1) != 0)
                    {
                        // payload too big
                        return -1;
                    }
                }
            }

            return read;
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(byte[]);
        }
    }
}
