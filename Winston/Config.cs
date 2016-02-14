using System.Security.Cryptography.X509Certificates;

namespace Winston
{
    public class Config
    {
        /// <summary>
        /// The directory in which Winston will install itself.
        /// </summary>
        public string WinstonDir { get; set; }

        /// <summary>
        /// Whether or not to write to the registry and update the user's PATH.
        /// Usually false for bootstrapped/embedded installations.
        /// </summary>
        public bool WriteRegistryPath { get; set; }
    }
}