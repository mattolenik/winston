using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Winston.Serialization
{
    class YmlUriConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof (Uri);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var reader = new EventReader(parser);
            var scalar = reader.Expect<Scalar>();
            return new Uri(scalar.Value);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(value.ToString()));
        }
    }
}
