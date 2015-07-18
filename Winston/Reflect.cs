using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Winston
{
    class Reflect
    {
        public static IEnumerable<PropertyInfo> Diff<T>(params T[] structs) => Diff(structs as IEnumerable<T>);

        public static IEnumerable<PropertyInfo> Diff<T>(IEnumerable<T> structs)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var values = structs.Select(s => p.GetValue(s));
                var equal = false;
                // Check if all values are equal for each struct passed in
                var last = values.First();
                foreach (var v in values.Skip(1))
                {
                    if (Equals(v, last))
                    {
                        equal = true;
                        last = v;
                    }
                }
                if (!equal)
                {
                    yield return p;
                }
            }
        }
    }
}