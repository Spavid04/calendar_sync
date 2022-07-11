using System;
using System.Text;
using System.Text.RegularExpressions;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;

namespace CalendarExport
{
    public static class Utils
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RelativeDateTimeRegex = new Regex(
            @"^now(?:-(\d+[wdhm])+)?$"
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
            if (!match.Groups[1].Success)
            {
                return true;
            }

            foreach (Capture capture in match.Groups[1].Captures)
            {
                int value = int.Parse(capture.Value[..^1]);
                switch (capture.Value[^1])
                {
                    case 'w':
                        dt -= TimeSpan.FromDays(7 * value);
                        break;
                    case 'd':
                        dt -= TimeSpan.FromDays(value);
                        break;
                    case 'h':
                        dt -= TimeSpan.FromHours(value);
                        break;
                    case 'm':
                        dt -= TimeSpan.FromMinutes(value);
                        break;
                }
            }

            return true;
        }

        public static string HashString(string input)
        {
            var config = new Argon2Config()
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,
                TimeCost = 8,
                Password = Encoding.UTF8.GetBytes(input),
                Salt = Encoding.ASCII.GetBytes("-nosalt-"), // 🤫
                Secret = null,
                AssociatedData = null,
                HashLength = 64
            };
            var argon2 = new Argon2(config);

            using (SecureArray<byte> hash = argon2.Hash())
            {
                return config.EncodeString(hash.Buffer);
            }
        }
    }
}
