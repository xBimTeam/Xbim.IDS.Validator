using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.IDS.Validator.Core
{
    public class IdsParser
    {

        IModel _model;

        public IModel Model  => _model;

        public IdsParser(string ifcModelPath)
        {
            _model = LoadModel(ifcModelPath);
        }

        private IModel LoadModel(string ifcModelPath)
        {
            return IfcStore.Open(ifcModelPath);
        }

        public IEnumerable<IPersistEntity> GetProducts(string ifcTypeName)
        {
            var query = _model.Instances.OfType(ifcTypeName, true);
            

           //_model.Metadata.ExpressType("IfcWall").Properties.First().Value.

            return query;
        }

        public IEnumerable<IPersistEntity> GetProducts(string ifcTypeName, string predefinedType)
        {
            var query = _model.Instances.OfType(ifcTypeName, true);



            return query;
        }

        private Type GetTypeFromText(string ifcTypeName)
        {
            throw new NotImplementedException();
        }
    }
}