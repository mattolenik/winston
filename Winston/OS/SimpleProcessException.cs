using System;
using System.Runtime.Serialization;

namespace Winston.OS
{
    public class SimpleProcessException : Exception
    {
        static readonly string nl = Environment.NewLine;

        public SimpleProcessException(int exitCode, string stdOut, string stdErr)
            : base($"Process exited with {exitCode}{nl}stdout:{nl}{stdOut}{nl + nl}stderr:{stdErr}")
        {
        }

        protected SimpleProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
