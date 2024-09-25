using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Utils
{
    public static class Util
    {





        /// <summary>
        ///  filter out the invalid char for filename
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MakeValidFileName(string input)
        {
            // 获取所有非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            // 使用Where方法过滤掉所有非法字符
            return new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
        }


        /// <summary>
        /// Converts a string to its SHA256 hash representation.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The SHA256 hash of the input string.</returns>
        public static string ConvertToSha256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}
