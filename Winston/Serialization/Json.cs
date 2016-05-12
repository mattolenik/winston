using System;
using System.IO;
using fastJSON;

namespace Winston.Serialization
{
    static class Json
    {
        internal static readonly JSONParameters Parameters = new JSONParameters
        {
            SerializeNullValues = false,
            UseExtensions = false,
            SerializeEmptyCollections = false,
            SerializeStaticMembers = false
        };

        public static T Load<T>(string path)
        {
            return JSON.ToObject<T>(File.ReadAllText(path));
        }

        public static void Save(object obj, string path, bool nice = false)
        {
            var json = ToJson(obj, nice);
            File.WriteAllText(path, json);
        }

        public static string ToJson(object obj, bool nice = false)
        {
            return nice ? JSON.ToNiceJSON(obj, Parameters) : JSON.ToJSON(obj);
        }
    }
}
