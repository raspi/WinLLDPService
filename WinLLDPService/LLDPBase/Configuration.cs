namespace WinLLDPService
{
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Configuration for LLDP TLVs
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Configuration
    {
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
        /// Initializes a new instance of the <see cref="Configuration"/> class. 
        /// </summary>
        public Configuration()
        {
            this.Separator = this.defaultSeparator;
            this.ChassisType = this.defaultChassisType;
            this.PortDescription = new List<string>();
            this.SystemDescription = new List<string>();
            this.SystemName = new List<string>
                                  {
                                      Environment.MachineName,
                                  };
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
        /// Gets or sets the system name.
        /// </summary>
        public List<string> SystemName { get; set; }

        /// <summary>
        /// Gets or sets the system description.
        /// </summary>
        public List<string> SystemDescription { get; set; }

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

        /// <inheritdoc />
        public override string ToString()
        {
            List<string> str = new List<string>
                                   {
                                       string.Format("{0}", this.GetType()),
                                       string.Format("Chassis type: '{0}'", this.ChassisType),
                                       string.Format("System name: '{0}'", string.Join(this.Separator, this.SystemName.ToArray())),
                                       string.Format("Port description: '{0}'", string.Join(this.Separator, this.PortDescription.ToArray())),
                                       string.Format("System description: '{0}'", string.Join(this.Separator, this.SystemDescription.ToArray())),
                                   };

            return string.Join(Environment.NewLine, str);
        }
    }
}