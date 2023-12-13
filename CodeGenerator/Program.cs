
using CodeGenerator;

Console.WriteLine("Code Generation started...");
var destPath = new DirectoryInfo(@"..\..\..\..\");
string dest;

Console.WriteLine("Executing measure helper generator ...");
var measureCode = MeasureHelperGenerator.Execute();
dest = Path.Combine(destPath.FullName, @"Xbim.IDS.Validator.Core\Helpers\MeasureHelpers.gen.cs");
File.WriteAllText(dest, measureCode);


Console.WriteLine("Completed.");
