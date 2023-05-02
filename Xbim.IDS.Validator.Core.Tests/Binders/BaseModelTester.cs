using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    [Collection(nameof(TestEnvironment))]
    public abstract class BaseModelTester
    {

        private static Lazy<IModel> lazyIfc4Model = new Lazy<IModel>(()=> BuildIfc4Model());
        private static Lazy<IModel> lazyIfc2x3Model = new Lazy<IModel>(() => BuildIfc2x3Model());

        private BinderContext _context = new BinderContext();
        
      

        public IModel Model
        {
            get
            {
                if (_schema == XbimSchemaVersion.Ifc2X3)
                {
                    return lazyIfc2x3Model.Value;
                }
                else
                {
                    return lazyIfc4Model.Value;
                }
            }
        }

        public BinderContext BinderContext
        {
            get
            {
                _context.Model = Model;
                return _context;
            }
        }

        protected IfcQuery query;

        private readonly ITestOutputHelper output;
        private XbimSchemaVersion _schema;
        protected readonly ILogger logger;



        public BaseModelTester(ITestOutputHelper output, XbimSchemaVersion schema = XbimSchemaVersion.Ifc4)
        {
            this.output = output;
            _schema = schema;
            query = new IfcQuery();
            
            logger = TestEnvironment.GetXunitLogger<BaseModelTester>(output);
        }

       

        private static IModel BuildIfc4Model()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }

        private static IModel BuildIfc2x3Model()
        {
            var filename = @"TestModels\Dormitory-ARC.ifczip";
            return IfcStore.Open(filename);
        }


        internal ILogger<T> GetLogger<T>()
        {
            return TestEnvironment.GetXunitLogger<T>(output);
        }

        protected static FacetGroup BuildGroup(IFacet facet)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>();
            group.RequirementOptions.Add(RequirementCardinalityOptions.Expected);
            group.Facets.Add(facet);
            return group;
        }


        public enum ConstraintType
        {
            Exact,
            Pattern,
            Range,
            Structure
        }
        
    }
}
