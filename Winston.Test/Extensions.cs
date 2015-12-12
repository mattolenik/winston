﻿using Newtonsoft.Json;

namespace Winston.Test
{
    public static class Extensions
    {
        public static string ToJson(this object obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return json;
        }
    }
}
