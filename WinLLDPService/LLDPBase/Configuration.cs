namespace WinLLDPService
{
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Management;
    using Microsoft.Win32;

    /// <summary>
    /// Configuration
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class. 
        /// </summary>
        public Configuration()
        {
            this.Separator = this.defaultSeparator;
            this.ChassisType = this.defaultChassisType;
            this.MachineName = Environment.MachineName;
            this.PortDescription = new List<string>();
            this.SystemDescription = new List<string>();
        }

        /// <summary>
        /// The Default chassis type.
        /// </summary>
        [NonSerialized]
        private readonly ChassisType defaultChassisType = ChassisType.MacAddress;

        /// <summary>
        /// The default separator.
        /// </summary>
        [NonSerialized]
        private readonly string defaultSeparator = " | ";

        /// <summary>
        /// Gets Debug string
        /// </summary>
        private string DebuggerDisplay
        {
            get
            {
                List<string> str = new List<string>
                                       {
                                           string.Format("{0}", this.GetType()),
                                           string.Format("{0}", this.ToString()),
                                       };

                return string.Join(", ", str);
            }
        }

        /// <summary>
        /// Gets or sets the separator.
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// Gets or sets the chassis type.
        /// </summary>
        public ChassisType ChassisType { get; set; }

        /// <summary>
        /// Gets or sets the port description.
        /// </summary>
        public List<string> PortDescription { get; set; }

        /// <summary>
        /// Gets or sets the machine name.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the system description.
        /// </summary>
        public List<string> SystemDescription { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            List<string> str = new List<string>
                                   {
                                       string.Format("{0}", this.GetType()),
                                       string.Format("Chassis type: '{0}'", this.ChassisType),
                                       string.Format("Machine name: '{0}'", this.MachineName),
                                       string.Format("Port description: '{0}'", string.Join(this.Separator, this.PortDescription.ToArray())),
                                       string.Format("System description: '{0}'", string.Join(this.Separator, this.SystemDescription.ToArray())),
                                   };

            return string.Join(Environment.NewLine, str);
        }
    }
}