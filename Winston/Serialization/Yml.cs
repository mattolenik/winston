using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Winston.Serialization
{
    public class Yml
    {
        public static T Load<T>(string path)
        {
            var deserializer = new Deserializer();
            deserializer.RegisterTypeConverter(new YmlUriConverter());
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
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
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
            serializer.RegisterTypeConverter(new YmlUriConverter());
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, obj);
            }
        }

        public static void Serialize(TextWriter writer, object obj)
        {
            var serializer = new Serializer();
            serializer.RegisterTypeConverter(new YmlUriConverter());
            serializer.Serialize(writer, obj);
        }

        public static string Serialize(object obj)
        {
            using (var sw = new StringWriter())
            {
                Serialize(sw, obj);
                return sw.ToString();
            }
        }
    }
}