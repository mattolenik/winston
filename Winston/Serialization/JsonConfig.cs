using System;
using fastJSON;

namespace Winston.Serialization
{
    public static class JsonConfig
    {
        public static void Init()
        {
            JSON.Parameters = Json.Parameters;
            JSON.Manager.OverrideConverter<Uri>(new UriConverter());
        }
    }

    public class UriConverter : JsonConverter<Uri, string>
    {
        protected override string Convert(string fieldName, Uri fieldValue)
        {
            return fieldValue.ToString();
        }

        protected override Uri Revert(string fieldName, string fieldValue)
        {
            return new Uri(fieldValue);
        }
    }
}