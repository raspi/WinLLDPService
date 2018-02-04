namespace WinLLDPService
{
    using System;
    using System.Runtime.Serialization;

    /// <inheritdoc />
    [Serializable]
    public class PowerShellConfiguratorException : Exception
    {
        /// <inheritdoc />
        public PowerShellConfiguratorException()
        {
        }

        /// <inheritdoc />
        public PowerShellConfiguratorException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public PowerShellConfiguratorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc />
        protected PowerShellConfiguratorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}