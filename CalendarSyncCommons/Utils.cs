using System;
using System.Text;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;

namespace CalendarSyncCommons
{
    public static class Utils
    {
        public static string HashString(string input)
        {
            var config = new Argon2Config()
            {
                Type = Argon2Type.DataDependentAddressing,
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

        public static DateTime ToDateTime(this string text)
        {
            return DateTime.Parse(text);
        }

        public static DateTime ToDateTime(this string text, DateTime onInvalid)
        {
            DateTime result;

            if (DateTime.TryParse(text, out result))
            {
                return result;
            }
            else
            {
                return onInvalid;
            }
        }
    }
}
