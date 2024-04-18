using Xbim.Common;
using Xbim.IDS.Validator.Common.Interfaces;

namespace Xbim.IDS.Validator.Core.Configuration
{
    public class IdsValueMapProvider : IValueMapProvider
    {
        public void CreateMappings(IValueMapper mapper)
        {
            mapper.AddMap<IExpressValueType>(v => v.Value);

            mapper.AddMap<bool>(v => v);
            mapper.AddMap<string>(v => v);
            mapper.AddMap<double>(v => v);
        }
    }
}
