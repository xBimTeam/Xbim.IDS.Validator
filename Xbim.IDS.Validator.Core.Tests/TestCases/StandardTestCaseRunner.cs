using System;
using Xbim.IDS.Validator.Tests.Common;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    /// <summary>
    /// A set of services to run IDS TestCases, configured for this unit test project's TestEnvironment
    /// </summary>
    [Collection(nameof(TestEnvironment))]
    public abstract class StandardTestCaseRunner : IdsTestCaseRunner
    {
        protected StandardTestCaseRunner(ITestOutputHelper output) : base(output)
        {
        }
    }
}
