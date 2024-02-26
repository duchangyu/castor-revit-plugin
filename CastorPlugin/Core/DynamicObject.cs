using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Core
{
    /// <summary>
    ///  模拟Javascript等动态语言实现动态对象，可以动态加入新的属性和对象
    ///  
    /// </summary>
    /// <example>
    /// 
    ///   
    ///    DynamicObject dynamicObject = new DynamicObject();
    ///    // 添加属性
    ///    dynamicObject.AddProperty("Name", "张三");
    ///    dynamicObject.AddProperty("Age", 30);
    ///    // 序列化
    ///    string json = dynamicObject.ToJson();
    ///    Console.WriteLine(json);
    ///    // 反序列化
    ///    DynamicObject deserializedObject = DynamicObject.FromJson(json);
    ///   Console.WriteLine($"Deserialized Name: {deserializedObject["Name"]}");
    ///    Console.WriteLine($"Deserialized Age: {deserializedObject["Age"]}");
    /// 
    /// </example>
    public class DynamicObject
    {
        /// <summary>
        /// 存储动态属性的字典。
        /// </summary>
        private readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();

        /// <summary>
        /// 通过键访问动态属性的值。
        /// </summary>
        /// <param name="key">属性的键。</param>
        /// <returns>属性的值，如果键不存在则返回 null。</returns>
        public object this[string key]
        {
            get
            {
                dictionary.TryGetValue(key, out var value);
                return value;
            }
            set
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("键不能为空或null。");
                }
                if (!dictionary.ContainsKey(key))
                {
                    // 动态添加属性
                    PropertyInfo property = typeof(DynamicObject).GetProperty(key);
                    if (property == null)
                    {
                        // 如果没有对应的属性，则直接添加到字典中
                        dictionary.Add(key, value);
                    }
                    else
                    {
                        // 如果有对应的属性，但类型不匹配，则抛出异常
                        if (property.PropertyType != value.GetType())
                        {
                            throw new InvalidOperationException($"属性 '{key}' 的类型不匹配。期望类型为 {property.PropertyType.Name}，但实际类型为 {value.GetType().Name}。");
                        }
                        // 设置属性值
                        property.SetValue(this, value);
                    }
                }
                else
                {
                    // 更新现有属性
                    dictionary[key] = value;
                }
            }
        }

        /// <summary>
        /// 向动态对象添加一个新属性。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <param name="value">属性的值。</param>
        public void AddProperty(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("属性名称不能为空或null。");
            }
            // 检查是否存在同名的属性
            var property = typeof(DynamicObject).GetProperty(name);
            if (property != null && property.PropertyType != value.GetType())
            {
                throw new InvalidOperationException($"属性 '{name}' 的类型不匹配。期望类型为 {property.PropertyType.Name}，但实际类型为 {value.GetType().Name}。");
            }
            dictionary.Add(name, value);
        }

        /// <summary>
        /// 从动态对象中移除一个属性。
        /// </summary>
        /// <param name="name">要移除的属性的名称。</param>
        public void RemoveProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("属性名称不能为空或null。");
            }
            dictionary.Remove(name);
        }

        /// <summary>
        /// 检查动态对象是否包含指定的属性。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <returns>如果对象包含指定的属性，则返回 true；否则返回 false。</returns>
        public bool HasProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("属性名称不能为空或null。");
            }
            return dictionary.ContainsKey(name);
        }


        // 辅助方法，用于序列化和反序列化
        /// <summary>
        /// 将 DynamicObject 实例序列化为 JSON 字符串。
        /// </summary>
        /// <returns>JSON 字符串表示的 DynamicObject 实例。</returns>
        public string ToJson() => JsonConvert.SerializeObject(this);

        /// <summary>
        /// 从 JSON 字符串反序列化一个 DynamicObject 实例。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        /// <returns>反序列化后的 DynamicObject 实例。</returns>
        public static DynamicObject FromJson(string json) => JsonConvert.DeserializeObject<DynamicObject>(json);


    }




}
