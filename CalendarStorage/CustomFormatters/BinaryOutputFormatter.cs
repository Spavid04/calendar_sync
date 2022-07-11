using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarStorage.CustomFormatters
{
    public class BinaryOutputFormatter : OutputFormatter
    {
        public BinaryOutputFormatter()
        {
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/octet-stream"));
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            byte[] data = context.Object as byte[];
            if (data == null)
            {
                context.HttpContext.Response.StatusCode = 500;
                return;
            }

            await context.HttpContext.Response.Body.WriteAsync(data, 0, data.Length);
        }

        protected override bool CanWriteType(Type type)
        {
            return type == typeof(byte[]);
        }
    }
}
