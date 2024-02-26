using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


    }
}
