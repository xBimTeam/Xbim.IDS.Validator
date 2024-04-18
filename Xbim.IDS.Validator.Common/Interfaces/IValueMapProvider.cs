using System;
using System.Collections.Generic;
using System.Text;

namespace Xbim.IDS.Validator.Common.Interfaces
{
    /// <summary>
    /// Interface enabling consumers to define value mappings used unwrap objects
    /// </summary>
    public interface IValueMapProvider
    {
        /// <summary>
        /// Create mappings in the <see cref="IValueMapper"/>
        /// </summary>
        /// <param name="mapper"></param>
        void CreateMappings(IValueMapper mapper);
    }
}
