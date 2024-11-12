using System;
using Xbim.Common;
using Xbim.IDS.Validator.Common.Interfaces;

namespace Xbim.IDS.Validator.Core.Configuration
{
    public class IdsValueMapProvider : IValueMapProvider
    {
        public void CreateMappings(IValueMapper mapper)
        {

            mapper.AddMap<IExpressBooleanType>(b => b.Value.ToString().ToLowerInvariant());
            mapper.AddMap<IExpressLogicalType>(b => b.Value.ToString().ToLowerInvariant());

            mapper.AddMap<IExpressValueType>(v => v.Value);

            // Todo: Should primitives be implicitly mapped so we don't handle explicitly
            mapper.AddMap<bool>(v => v);
            mapper.AddMap<int>(v => v);
            mapper.AddMap<long>(v => v);
            mapper.AddMap<string>(v => v);
            mapper.AddMap<double>(v => v);
            mapper.AddMap<float>(v => v);

            mapper.AddMap<Enum> (v => v.ToString());
        }
    }
}
