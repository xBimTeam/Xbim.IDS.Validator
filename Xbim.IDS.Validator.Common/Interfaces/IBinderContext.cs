using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Represents context used by a binder to access a model
    /// </summary>
    public interface IBinderContext
    {
        /// <summary>
        /// Gets the model being validated
        /// </summary>
        IModel? Model { get; }
    }
}
