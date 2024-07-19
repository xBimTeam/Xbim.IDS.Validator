using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class BinderContext: IBinderContext
    {
        /// <summary>
        /// The current model
        /// </summary>
        public IModel? Model { get; set; }
    }
}
