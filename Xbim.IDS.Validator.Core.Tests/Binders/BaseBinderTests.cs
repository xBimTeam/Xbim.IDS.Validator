using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.Ifc;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    /// <summary>
    /// Supports testing of <see cref="IFacetBinder"/>s
    /// </summary>
    [Collection(nameof(TestEnvironment))]
    public abstract class BaseBinderTests
    {

        private static Lazy<IModel> lazyIfc4Model = new Lazy<IModel>(()=> BuildIfc4Model());
        private static Lazy<IModel> lazyIfc2x3Model = new Lazy<IModel>(() => BuildIfc2x3Model());

        private BinderContext _context = new BinderContext();
        
      

        public virtual IModel Model
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

        protected readonly ITestOutputHelper output;
        private XbimSchemaVersion _schema;
        protected readonly ILogger logger;



        public BaseBinderTests(ITestOutputHelper output, XbimSchemaVersion schema = XbimSchemaVersion.Ifc4)
        {
            this.output = output;
            _schema = schema;
            query = new IfcQuery();
            
            logger = TestEnvironment.GetXunitLogger<BaseBinderTests>(output);
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


        protected ILogger<T> GetLogger<T>()
        {
            return TestEnvironment.GetXunitLogger<T>(output);
        }

        protected static FacetGroup BuildGroup(IFacet facet)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>()
            {
                facet.BuildCardinality(RequirementCardinalityOptions.Cardinality.Expected)
            };
            group.Facets.Add(facet);
            return group;
        }

        protected IValueMapper GetValueMapper()
        {
            return new IdsValueMapper(new[] { new IdsValueMapProvider() });
        }


        public enum ConstraintType
        {
            Exact,
            Pattern,
            Range,
            Structure
        }


        protected void AssertIfcTypeFacetQuery(IfcTypeFacetBinder typeFacetBinder, string ifcType, int expectedCount, Type[] expectedTypes, string predefinedType = "",
            ConstraintType ifcTypeConstraint = ConstraintType.Exact, ConstraintType preConstraint = ConstraintType.Exact, bool includeSubTypes = true)
        {
            IfcTypeFacet facet = BuildIfcTypeFacetFromCsv(ifcType, predefinedType, includeSubTypes, ifcTypeConstraint, preDefConstraint: preConstraint);

            // Act
            var expression = typeFacetBinder.BindSelectionExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, Model);

            result.Should().HaveCount(expectedCount);

            if (expectedCount > 0)
            {
                result.Should().AllSatisfy(t =>
                    expectedTypes.Where(e => e.IsAssignableFrom(t.GetType()))
                    .Should().ContainSingle($"Found {t.GetType().Name}, and expected one of {string.Join(',', expectedTypes.Select(t => t.Name))}"));

            }
        }

        protected void AssertIfcPropertyFacetQuery(PsetFacetBinder psetFacetBinder, string psetName, string propName, object propValue, int expectedCount, ConstraintType psetConstraint, ConstraintType propConstraint, ConstraintType valueConstraint)
        {
            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint(),
                PropertyName = new ValueConstraint(),
                PropertyValue = new ValueConstraint()
            };
            switch (psetConstraint)
            {
                case ConstraintType.Exact:
                    propFacet.PropertySetName.AddAccepted(new ExactConstraint(psetName));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertySetName.AddAccepted(new PatternConstraint(psetName));
                    break;

            }
            switch (propConstraint)
            {
                case ConstraintType.Exact:
                    propFacet.PropertyName.AddAccepted(new ExactConstraint(propName));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertyName.AddAccepted(new PatternConstraint(propName));
                    break;

            }
            if (propValue != null)
            {
                SetPropertyValue(propValue, valueConstraint, propFacet);
            }

            // Act
            var expression = psetFacetBinder.BindSelectionExpression(query.InstancesExpression, propFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);
        }

        protected static void SetPropertyValue(object propValue, ConstraintType valueConstraint, IfcPropertyFacet propFacet)
        {
            switch (valueConstraint)
            {
                case ConstraintType.Exact:
                    if (propValue is bool)
                    {
                        // By convention bools are upper case. May need to review XIDS on this.
                        propFacet.PropertyValue.AddAccepted(new ExactConstraint(propValue.ToString().ToLowerInvariant()));
                        break;
                    }
                    propFacet.PropertyValue.AddAccepted(new ExactConstraint(propValue.ToString()));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertyValue.AddAccepted(new PatternConstraint(propValue.ToString()));
                    break;

                case ConstraintType.Range:
                    propFacet.PropertyValue.BaseType = NetTypeName.Double;      // TODO: Revisit. Needed until XIDS can support ranges of undefined type
                    propFacet.PropertyValue.AddAccepted(new RangeConstraint("0", false, propValue.ToString(), true));
                    break;


            }
        }

        private static IfcTypeFacet BuildIfcTypeFacetFromCsv(string ifcTypeCsv, string predefinedTypeCsv = "", bool includeSubTypes = false,
            ConstraintType ifcConstraint = ConstraintType.Exact, ConstraintType preDefConstraint = ConstraintType.Exact)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(),
                PredefinedType = new ValueConstraint(),
                IncludeSubtypes = includeSubTypes,
            };

            var ifcValues = ifcTypeCsv.Split(',');
            foreach (var ifcVal in ifcValues)
            {
                if (string.IsNullOrEmpty(ifcVal)) continue;
                if (ifcConstraint == ConstraintType.Pattern)
                    facet.IfcType.AddAccepted(new PatternConstraint(ifcVal));
                else
                    facet.IfcType.AddAccepted(new ExactConstraint(ifcVal));
            }

            var pdTypes = predefinedTypeCsv.Split(',');
            foreach (var predef in pdTypes)
            {
                if (string.IsNullOrEmpty(predef)) continue;
                if (preDefConstraint == ConstraintType.Pattern)
                    facet.PredefinedType.AddAccepted(new PatternConstraint(predef));
                else
                    facet.PredefinedType.AddAccepted(new ExactConstraint(predef));
            }
            return facet;
        }

    }
}
