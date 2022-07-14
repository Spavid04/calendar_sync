using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CalendarExport
{
    public static class Utils
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RelativeDateTimeRegex = new Regex(
            @"^now(?:([-+])(\d+[wdhm])+)?$"
            , RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParseRelativeDateTime(string relativeDT, out DateTime dt)
        {
            dt = default;

            if (string.IsNullOrWhiteSpace(relativeDT))
            {
                return false;
            }

            string withoutWhitespace = WhitespaceRegex.Replace(relativeDT, ""); // so the actual matching regex isn't littered with \s*
            var match = RelativeDateTimeRegex.Match(withoutWhitespace);
            if (!match.Success)
            {
                return false;
            }

            dt = DateTime.Now;
            if (!match.Groups[2].Success)
            {
                return true;
            }

            int negate = match.Groups[1].Value == "-" ? -1 : 1;
            foreach (Capture capture in match.Groups[2].Captures)
            {
                int value = int.Parse(capture.Value[..^1]) * negate;
                switch (capture.Value[^1])
                {
                    case 'w':
                        dt += TimeSpan.FromDays(7 * value);
                        break;
                    case 'd':
                        dt += TimeSpan.FromDays(value);
                        break;
                    case 'h':
                        dt += TimeSpan.FromHours(value);
                        break;
                    case 'm':
                        dt += TimeSpan.FromMinutes(value);
                        break;
                }
            }

            return true;
        }

        public static string ToQuotedPrintable(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder builder = new StringBuilder();

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            foreach (byte v in bytes)
            {
                // The following are not required to be encoded:
                // - Tab (ASCII 9)
                // - Space (ASCII 32)
                // - Characters 33 to 126, except for the equal sign (61).

                if ((v == 9) || ((v >= 32) && (v <= 60)) || ((v >= 62) && (v <= 126)))
                {
                    builder.Append(Convert.ToChar(v));
                }
                else
                {
                    builder.Append('=');
                    builder.Append(v.ToString("X2"));
                }
            }

            char lastChar = builder[builder.Length - 1];
            if (char.IsWhiteSpace(lastChar))
            {
                builder.Remove(builder.Length - 1, 1);
                builder.Append('=');
                builder.Append(((int)lastChar).ToString("X2"));
            }

            return builder.ToString();
        }

        /// <param name="encoding">defaults to UTF8</param>
        public static string FromQuotedPrintable(this string input, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            var occurences = new Regex(@"(=[0-9A-Z]{2}){1,}", RegexOptions.Multiline | RegexOptions.Compiled);
            var matches = occurences.Matches(input);

            foreach (Match match in matches)
            {
                try
                {
                    byte[] b = new byte[match.Groups[0].Value.Length / 3];
                    for (int i = 0; i < match.Groups[0].Value.Length / 3; i++)
                    {
                        b[i] = byte.Parse(match.Groups[0].Value.Substring(i * 3 + 1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    char[] hexChar = encoding.GetChars(b);
                    input = input.Replace(match.Groups[0].Value, new String(hexChar));
                }
                catch
                {

                }
            }
            input = input.Replace("?=", "").Replace("\r\n", "");

            return input;
        }

        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        public static int ToEpochSeconds(this DateTime dt)
        {
            checked
            {
                // yeah...
                return (int)(dt - Epoch).TotalSeconds;
            }
        }
    }
}
