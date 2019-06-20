using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace WhenTheVersion
{
    public class AssemblyInfoReader
    {
        private readonly string _fileNameWithPath;

        public AssemblyInfoReader(string fileNameWithPath)
        {
            _fileNameWithPath = fileNameWithPath;
        }

        public RevisionInfo GetRevisionInfo()
        {
            if (File.Exists(_fileNameWithPath) == false)
                throw new FileNotFoundException(_fileNameWithPath);

            //AsseemblyInfo file will be just couple of lines so no harm in reading entire file
            var fileContents = RemoveAllComments(File.ReadAllText(_fileNameWithPath));

            if (string.IsNullOrWhiteSpace(fileContents))
                return new RevisionInfo(0, 0, "File contents are empty");

            const string GROUP_VERSION_NUMBER = "VersionNumber";

            Regex extractAssemblyVersionLine = new Regex($@"Assembly(File)?Version\(""(?<{GROUP_VERSION_NUMBER}>.*)""\)"); //It will be case sensitive search

            Match lineMatch = extractAssemblyVersionLine.Match(fileContents); //Grab the first match only

            if (lineMatch.Success == false)
                return new RevisionInfo(0, 0, "Can't find any line with text 'AssemblyFileVersion' or 'AssemblyVersion'");

            var versionNumber = lineMatch.Groups[GROUP_VERSION_NUMBER].Value;
            var lastRevisionNumber = versionNumber.Split('.').LastOrDefault();

            if (int.TryParse(lastRevisionNumber, out var lastNumber))
                return new RevisionInfo(lastNumber, lastNumber + 1);

            return new RevisionInfo(0, 0, $"Can't parse {lastRevisionNumber} to int");
        }
        string RemoveAllComments(string contents)
        {
            if (string.IsNullOrWhiteSpace(contents))
                return contents;

            contents += Environment.NewLine;

            //https://stackoverflow.com/a/3524689
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            string noComments = Regex.Replace(contents, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings, me =>
            {
                if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    return me.Value.StartsWith("//") ? Environment.NewLine : "";
                // Keep the literal strings
                return me.Value;
            }, RegexOptions.Singleline);

            return noComments;
        }
    }
}
