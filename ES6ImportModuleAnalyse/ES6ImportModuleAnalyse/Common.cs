using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ES6ImportModuleAnalyse
{
    public static class Common
    {
        public static IDictionary<string, object> NewDictionary()
        {
            return new Dictionary<string, object>();
        }

        public static IDictionary<string, object> JsonToDictionary(string str)
        {
            try
            {
                return new JavaScriptSerializer().Deserialize<IDictionary<string, object>>(str);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static IList<IDictionary<string, object>> JsonToList(string str)
        {
            try
            {
                return new JavaScriptSerializer().Deserialize<IList<IDictionary<string, object>>>(str);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static string ToJsonString(this object o)
        {
            StringBuilder sb = new StringBuilder();
            JavaScriptSerializer json = new JavaScriptSerializer();
            json.Serialize(o, sb);
            return sb.ToString();
        }
        /// <summary>
        /// 等同于Java Map的put
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(this IDictionary<string, object> dic, string key, object value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
        }
        public static object Get(this IDictionary<string, object> dic, string key, object defaultValue = null)
        {
            if (dic.ContainsKey(key))
            {
                var result = dic[key];
                return result == null ? defaultValue : result;
            }
            else
            {
                return defaultValue;
            }
        }
        public static string GetString(this IDictionary<string, object> dic, string key, string defaultValue = "")
        {
            if (dic.ContainsKey(key))
            {
                var result = dic[key];
                return result == null ? defaultValue : result.ToString();
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetJsonString(this IDictionary<string, object> dic, string key, string defaultValue = "")
        {
            if (dic.ContainsKey(key))
            {
                var result = dic[key];
                return result == null ? defaultValue : result.ToJsonString();
            }
            else
            {
                return defaultValue;
            }
        }
        public static IDictionary<string, object> GetDic(this IDictionary<string, object> dic, string key)
        {
            if (dic.ContainsKey(key))
            {
                var result = dic[key] as IDictionary<string, object>;
                return result == null ? NewDictionary() : result;
            }
            else
            {
                return NewDictionary();
            }
        }
        public static string GetString(this IEnumerable dic, string[] key)
        {
            var result = "";
            if (key.Length > 0)
            {
                object value = dic;
                for (var i = 0; i < key.Length; i++)
                {
                    if (new Regex(@"\d+").IsMatch(key[i]))
                    {
                        var l = value as IList;
                        var index = int.Parse(key[i]);
                        if (l.Count > index)
                        {
                            value = l[index];
                        }
                        else
                        {
                            return result;
                        }
                    }
                    else
                    {
                        var d = value as IDictionary;
                        if (d.Contains(key[i]))
                        {
                            value = d[key[i]];
                        }
                        else
                        {
                            return result;
                        }
                    }
                }
                return value.ToString();
            }
            else
            {
                return result;
            }
        }
    }
}
