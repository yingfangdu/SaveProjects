﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeneratePackages
{
    class Program
    {
        const string BAERepoPath = @"E:\Repos\BAE\";
        const string ProgramWXSPath = BAERepoPath + @"src\packaging\installer\msi\program.wxs";
        const string HTMLFolder = BAERepoPath + @"src\ui\HTMLUI\node_modules\@bingads-webui-bae\htmlui\dist\release";
        const string HTMLFolderCSS = BAERepoPath + @"src\ui\HTMLUI\node_modules\@bingads-webui-bae\htmlui\dist\release\static\css";
        const string HTMLFolderJS = BAERepoPath + @"src\ui\HTMLUI\node_modules\@bingads-webui-bae\htmlui\dist\release\static\js";
        const string HTMLFolderMedia = BAERepoPath + @"src\ui\HTMLUI\node_modules\@bingads-webui-bae\htmlui\dist\release\static\media";

        const string HTMLUIComponentIdStartTag = "<!-- HTML UI Package Id starts here -->";
        const string HTMLUIComponentIdEndTag = "<!-- HTML UI Package Id ends here -->";

        const string HTMLUIComponentItemsStartTag = "<!-- HTML UI Package items starts here -->";
        const string HTMLUIComponentItemsEndTag = "<!-- HTML UI Package items ends here -->";

        private class FileData
        {
            public string FileId { get; set; }
            public string FileName { get; set; }
        }

        static void Main(string[] args)
        {
            var html = GenerateStrippedFileNames(HTMLFolder);
            var css = GenerateStrippedFileNames(HTMLFolderCSS);
            var js = GenerateStrippedFileNames(HTMLFolderJS);
            var media = GenerateStrippedFileNames(HTMLFolderMedia);
            var ids = new List<FileData>();
            ids.AddRange(html);
            ids.AddRange(css);
            ids.AddRange(js);
            ids.AddRange(media);

            // Validate there is no dup.
            var distincts = ids.Distinct().ToList();
            if (ids.Count != distincts.Count)
            {
                throw new Exception("error");
            }

            StringBuilder sb = new StringBuilder();
            foreach (var fileId in ids)
            {
                sb.Append(FormatComponentId(fileId));
            }

            var componentIdsString = sb.ToString();

            sb.Clear();
            sb.Append("    <DirectoryRef Id=\"HTMLFOLDER\" FileSource=\"$(var.OUTPUTROOT)\\Client\\html\">\n\n");
            foreach (var fileData in html)
            {
                sb.Append(FormatComponent(fileData));
            }
            sb.Append("    </DirectoryRef>\n\n");

            sb.Append("    <DirectoryRef Id=\"HTMLSTATICCSSFOLDER\" FileSource=\"$(var.OUTPUTROOT)\\Client\\html\\static\\css\">\n\n");
            foreach (var fileData in css)
            {
                sb.Append(FormatComponent(fileData));
            }
            sb.Append("    </DirectoryRef>\n\n");

            sb.Append("    <DirectoryRef Id=\"HTMLSTATICJSFOLDER\" FileSource=\"$(var.OUTPUTROOT)\\Client\\html\\static\\js\">\n\n");
            foreach (var fileData in js)
            {
                sb.Append(FormatComponent(fileData));
            }
            sb.Append("    </DirectoryRef>\n\n");

            sb.Append("    <DirectoryRef Id=\"HTMLSTATICMEDIAFOLDER\" FileSource=\"$(var.OUTPUTROOT)\\Client\\html\\static\\media\">\n\n");
            foreach (var fileData in media)
            {
                sb.Append(FormatComponent(fileData));
            }
            sb.Append("    </DirectoryRef>\n");

            var componentDetail = sb.ToString();

            string fullContent = File.ReadAllText(ProgramWXSPath);

            var componentIDStartIndex = fullContent.IndexOf(HTMLUIComponentIdStartTag) + HTMLUIComponentIdStartTag.Length + 1; // include the Return.
            var componentIdEndIndex = fullContent.IndexOf(HTMLUIComponentIdEndTag);
            var componentItemsStartIndex = fullContent.IndexOf(HTMLUIComponentItemsStartTag) + HTMLUIComponentItemsStartTag.Length + 1; // include the Return.
            var componentItemsEndIndex = fullContent.IndexOf(HTMLUIComponentItemsEndTag);

            var part1 = fullContent.Substring(0, componentIDStartIndex);
            var part2 = fullContent.Substring(componentIdEndIndex, componentItemsStartIndex - componentIdEndIndex);
            var part3 = fullContent.Substring(componentItemsEndIndex);

            var newFullString = new StringBuilder();
            newFullString.Append(part1);
            newFullString.Append(componentIdsString);
            newFullString.Append(part2);
            newFullString.Append(componentDetail);
            newFullString.Append(part3);
            File.WriteAllText(ProgramWXSPath, newFullString.ToString());
        }

        private static string FormatComponentId(FileData fileData)
        {
            return $"      <ComponentRef Id=\"{fileData.FileId}_c\" />\n";
        }

        private static string FormatComponent(FileData fileData)
        {
            return string.Format("      <Component Id =\"{0}_c\" Guid=\"*\">\n        <File Id=\"{0}_f\" Checksum=\"no\" KeyPath=\"yes\" Name=\"{1}\" />\n      </Component>\n\n", fileData.FileId, fileData.FileName);
        }

        private static IList<FileData> GenerateStrippedFileNames(string path)
        {
            IList<FileData> fileData = new List<FileData>();
            var files = GetFilePaths(path);
            foreach (var file in files)
            {
                fileData.Add(new FileData()
                {
                    FileId = GetStrippedFileName(file),
                    FileName = file,
                });
            }

            return fileData;
        }

        private static string GetStrippedFileName(string file)
        {
            var splitted = file.Split(new char[] { '.' }).ToList();

            string guidString = null;
            foreach(var split in splitted)
            {
                if(ContainsOnlyCharactersAndNumbers(split))
                {
                    if (guidString != null)
                    {
                        throw new NotSupportedException("matchedSplit is not null");
                    }

                    guidString = split;
                }
            }

            if (guidString != null)
            {
                splitted.Remove(guidString);
            }

            if (ContainsOnlyDigitals(splitted[0]))
            {
                splitted[0] = $"_{splitted[0]}";
            }

            return string.Join("_", splitted.Select(item => item.Replace('-', '_')).ToList().Select(item => item.Replace('~', '_')));
        }

        private static bool ContainsOnlyDigitals(string name)
        {
            for (int index = 0; index < name.Length; index++)
            {
                if (name[index] < '0' || name[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsOnlyCharactersAndNumbers(string name)
        {
            bool hasNumbers = false;

            if (name.Length < 8)
            {
                return false;
            }

            for(int index = 0; index < name.Length; index++)
            {
                if (name[index] >= '0' && name[index] <= '9' || name[index] >= 'a' || name[index] <= 'z')
                {
                    if (name[index] >= '0' && name[index] <= '9')
                    {
                        hasNumbers = true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return hasNumbers;
        }

        private static IList<string> GetFilePaths(string path)
        {
            IList<string> returnFiles = new List<string>();

            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                returnFiles.Add(Path.GetFileName(file));
            }

            return returnFiles;
        }
    }
}
