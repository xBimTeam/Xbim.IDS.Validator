using Xbim.CobieExpress.Interfaces;
using Xbim.CobieExpress;
using Xbim.IDS.Validator.Common.Interfaces;

namespace Xbim.IDS.Validator.Extensions.COBie.Configuration
{
    public class COBieValueMapProvider : IValueMapProvider
    {
        public void CreateMappings(IValueMapper mapper)
        {
            mapper.AddMap<ICobiePickValue>(v => v.Value);

            mapper.AddMap<CobieExternalObject>(v => v.Name);
            mapper.AddMap<CobieExternalSystem>(v => v.Name);

            mapper.AddMap<ICobieAsset>(v => v.Name);
        }
    }
}
