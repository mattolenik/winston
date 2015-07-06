using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Winston
{
    public class Yml
    {
        public static T Load<T>(string path)
        {
            var deserializer = new Deserializer();
            using (var reader = new StreamReader(path))
            {
                return deserializer.Deserialize<T>(reader);
            }
        }

        public static bool TryLoad<T>(string path, out T result)
        {
            try
            {
                result = Load<T>(path);
            }
            catch
            {
                result = default(T);
                return false;
            }
            return true;
        }

        public static bool TryLoad<T>(string path, out T result, Func<T> defaultVal)
        {
            var success = TryLoad(path, out result);
            if (!success)
            {
                result = defaultVal();
            }
            return success;
        }

        public static void Save(object obj, string path)
        {
            var serializer = new Serializer();
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, obj);
            }
        }
    }
}