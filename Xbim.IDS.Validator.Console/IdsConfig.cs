using System;

namespace Xbim.IDS.Validator.Console
{
    /// <summary>
    /// Configuration related IDS validation
    /// </summary>
    public sealed class IdsConfig
    {
        internal static readonly string SectionName = "IdsConfig";

        /// <summary>
        /// Indicates whether detokenisation will be performed
        /// </summary>
        public bool Detokenise { get; set; }

        /// <summary>
        /// Tokens allow IDS files to be parameterised at runtime, by replacing a values such as <c>{{ProjectNumber}}</c>
        /// with a value used on a specific project
        /// </summary>
        public Dictionary<string,string> Tokens { get; set; } = new Dictionary<string,string>();
    }
}
