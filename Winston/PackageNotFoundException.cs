using System;
using System.Runtime.Serialization;

namespace Winston
{
    [Serializable]
    public class PackageNotFoundException : Exception, IExitCodeException
    {
        public int ErrorCode => ExitCodes.PackageNotFound;

        public PackageNotFoundException()
        {
        }

        public PackageNotFoundException(string message) : base(message)
        {
        }

        public PackageNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PackageNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}