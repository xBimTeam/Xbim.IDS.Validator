using System;
using System.Collections.Generic;
using System.Text;

namespace Xbim.IDS.Validator.Common.Interfaces
{
    /// <summary>
    /// An interface for extracting values from objects
    /// </summary>
    public interface IValueMapper
    {
        /// <summary>
        /// Unpacks a value from an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The object to map value from</param>
        /// <param name="mappedValue">The mapped value or null if not mapped</param>
        /// <returns><c>true</c> if the value could be mapped; otherwise <c>false</c></returns>
        bool MapValue<T>(T value, out object? mappedValue);

        /// <summary>
        /// Adds a mapping to the Mapper
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="fn">The accessor function for an instance of <typeparamref name="T"/></param>
        void AddMap<T>(Func<T, object> fn);

        /// <summary>
        /// Return true if the map exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool ContainsMap<T>();
    }
}
