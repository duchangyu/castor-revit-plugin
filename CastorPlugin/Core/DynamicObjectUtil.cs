namespace CastorPlugin.Core
{
    public static class DynamicObjectUtil
    {



        /// <summary>
        /// 
        ///  在嵌套字典中添加对象的辅助方法
        ///  
        ///  usage: 动态地在嵌套字典中添加一个对象
        /// AddToNestedDictionary(nestedDictionary, "Person.Address.PostalCode", "100000");

        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="keyPath"></param>
        /// <param name="value"></param>
        public static void AddToNestedDictionary(Dictionary<string, object> dictionary, string keyPath, object value)
        {
            // 分割键路径
            string[] keys = keyPath.Split('.');
            Dictionary<string, object> currentDict = dictionary;
            // 遍历键路径，直到倒数第二个键
            for (int i = 0; i < keys.Length - 1; i++)
            {
                // 如果当前键不存在，创建一个新的嵌套字典
                if (!currentDict.TryGetValue(keys[i], out object obj) || obj is not Dictionary<string, object>)
                {
                    currentDict[keys[i]] = new Dictionary<string, object>();
                }
                // 更新当前字典为下一级嵌套字典
                currentDict = (Dictionary<string, object>)currentDict[keys[i]];
            }
            // 添加新的键值对到最后的嵌套字典
            currentDict[keys[keys.Length - 1]] = value;
        }

        // 创建嵌套字典的辅助方法
        public static Dictionary<string, object> CreateNestedDictionary(string key, params (string Key, object Value)[] keyValuePairs)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var (Key, Value) in keyValuePairs)
            {
                if (Value is (string, object)[] nestedKeyValuePairs)
                {
                    // 递归创建嵌套字典
                    dictionary[Key] = CreateNestedDictionary(Key, nestedKeyValuePairs);
                }
                else
                {
                    // 添加键值对到当前字典
                    dictionary[Key] = Value;
                }
            }
            return dictionary;
        }


        // 递归打印字典内容
        public static void PrintDictionary(Dictionary<string, object> dictionary, string indent)
        {
            foreach (var item in dictionary)
            {
                Console.WriteLine($"{indent}{item.Key}: ");
                if (item.Value is Dictionary<string, object> nestedDict)
                {
                    PrintDictionary(nestedDict, indent + "  ");
                }
                else
                {
                    Console.WriteLine($"{indent}  {item.Value}");
                }
            }
        }
    }
}