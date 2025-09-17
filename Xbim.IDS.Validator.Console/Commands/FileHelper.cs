using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.IDS.Validator.Console.Internal;

namespace Xbim.IDS.Validator.Console.Commands
{
    public abstract class TokenCommandBase
    {
        private readonly IdsConfig config;

        public IdsConfig Config => config;

        protected TokenCommandBase(IOptions<IdsConfig> config)
        {
            this.config = config.Value;
        }

        protected FileInfo BuildOutputFile(FileInfo templateFile, DirectoryInfo? targetFolder = null, string relativePath = "")
        {
            FileInfo outputFile;
            var fileName = templateFile.FullName;
            var ext = Path.GetExtension(fileName);
            var path = Path.GetDirectoryName(fileName);
            var trunk = Path.GetFileNameWithoutExtension(fileName);

            var outFile = DetokeniseFileName(trunk, config.Tokens);
            if (trunk == outFile && (targetFolder is null || targetFolder.FullName == path))
            {
                outFile += "-out";  // Don't overwrite original
            }
            outFile += ext;
            if (targetFolder is null)
            {
                // In place / Put in target folder
                outFile = Path.Combine(path, outFile);
            }
            else
            {
                // Copy to Target
                var outFolder = Path.Combine(targetFolder.FullName, relativePath);
                outFile = Path.Combine(outFolder, outFile);
                var newTargetFolder = new DirectoryInfo(outFolder);
                newTargetFolder.Create();
            }
            outputFile = new FileInfo(outFile);
            return outputFile;
        }

        private string DetokeniseFileName(string outFile, Dictionary<string, string> tokens)
        {
            foreach (var pair in tokens)
            {
                var key = "{{" + pair.Key + "}}";
                outFile = outFile.Replace(key, pair.Value);
            }
            return MakeSafeFile(outFile);
        }

        private static string MakeSafeFile(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '-');
            }
            if (fileName.EndsWith("."))
                fileName = fileName.Substring(0, fileName.Length - 1);
            return fileName;
        }

        protected void WriteConfig(ConsoleLogger console)
        {
            var tokenKeys = string.Join("\n\t", Config.Tokens.Select(kv => $"{{{kv.Key}}} => {kv.Value}"));
            console.WriteInfoLine("Replacing IDS tokens:");
            foreach (var kv in Config.Tokens)
            {
                console.WriteInfo(ConsoleColor.Green, $"  {{{kv.Key}}}");
                console.WriteInfo(ConsoleColor.White, " : ");
                console.WriteInfo(ConsoleColor.Cyan, $"{kv.Value}\n");

            }
        }

    }
}
