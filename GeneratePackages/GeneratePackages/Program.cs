using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratePackages
{
    class Program
    {
        const string cef_redist_x64_path = @"E:\Repos\BAE\packages\cef.redist.x64.3.3239.1723\CEF";
        const string cef_redist_x64_locales_path = @"E:\Repos\BAE\packages\cef.redist.x64.3.3239.1723\CEF\locales";
        const string cef_redist_x64_swiftshader_path = @"E:\Repos\BAE\packages\cef.redist.x64.3.3239.1723\CEF\swiftshader";

        const string cef_redist_x86_path = @"E:\Repos\BAE\packages\cef.redist.x86.3.3239.1723\CEF";
        const string cef_redist_x86_locales_path = @"E:\Repos\BAE\packages\cef.redist.x86.3.3239.1723\CEF\locales";
        const string cef_redist_x86_swiftshader_path = @"E:\Repos\BAE\packages\cef.redist.x86.3.3239.1723\CEF\swiftshader";

        const string cef_common_x64_path = @"E:\Repos\BAE\packages\CefSharp.Common.63.0.3\CefSharp\x64";
        const string cef_common_x86_path = @"E:\Repos\BAE\packages\CefSharp.Common.63.0.3\CefSharp\x86";
        const string cef_winForm_x64_path = @"E:\Repos\BAE\packages\CefSharp.WinForms.63.0.3\CefSharp\x64";
        const string cef_winForm_x86_path = @"E:\Repos\BAE\packages\CefSharp.WinForms.63.0.3\CefSharp\x86";

        static IList<string> exclusions = new List<string>() { ".pdb", ".xml", ".XML" };

        class FileData
        {
            public FileData(string fileName, string fileId, string relativeLocation, string componentRef, string componentDetail)
            {
                if (string.IsNullOrEmpty(fileName) ||
                    string.IsNullOrEmpty(fileId) ||
                    string.IsNullOrEmpty(relativeLocation) ||
                    string.IsNullOrEmpty(componentRef) ||
                    string.IsNullOrEmpty(componentDetail))
                {
                    throw new Exception("error");
                }

                this.FileName = fileName;
                this.FileID = fileId;
                this.FileRelativeLocation = relativeLocation;
                this.FileComponentRef = componentRef;
                this.FileComponentDetail = componentDetail;
            }

            public string FileName { get; private set; }

            public string FileRelativeLocation { get; private set; }

            public string FileID { get; private set; }

            public string FileComponentRef { get; private set; }

            public string FileComponentDetail { get; private set; }
        }

        static void Main(string[] args)
        {
            var ids = new List<FileData>();

            ids.AddRange(GenerateFileComponent(cef_redist_x64_path, true, null));
            ids.AddRange(GenerateFileComponent(cef_redist_x64_locales_path, true, "locales"));
            ids.AddRange(GenerateFileComponent(cef_redist_x64_swiftshader_path, true, "swiftshader"));

            ids.AddRange(GenerateFileComponent(cef_redist_x86_path, false, null));
            ids.AddRange(GenerateFileComponent(cef_redist_x86_locales_path, false, "locales"));
            ids.AddRange(GenerateFileComponent(cef_redist_x86_swiftshader_path, false, "swiftshader"));

            ids.AddRange(GenerateFileComponent(cef_common_x64_path, true, null));
            ids.AddRange(GenerateFileComponent(cef_common_x86_path, false, null));

            ids.AddRange(GenerateFileComponent(cef_winForm_x64_path, true, null));

            ids.AddRange(GenerateFileComponent(cef_winForm_x86_path, false, null));

            var fileIds = ids.Select(item => item.FileID).ToList();

            if (fileIds.Count != fileIds.Distinct().Count())
            {
                throw new Exception("error");
            }

            WriteToFile(ids.Select(item => item.FileRelativeLocation).ToList(), ",\n", "SetupBase");

            WriteToFile(ids.Select(item => item.FileComponentRef).ToList(), "\n", "Program_ComponentRefs");

            WriteToFile(ids.Select(item => item.FileComponentDetail).ToList(), "\n\n", "Program_Components");
        }

        private static IList<FileData> GenerateFileComponent(string path, bool is64, string fix)
        {
            IList<FileData> fileData = new List<FileData>();

            var files = GetFilePaths(path);
            foreach (var file in files)
            {
                string fileId = null;
                string relativeLocation = null;
                if (string.IsNullOrEmpty(fix))
                {
                    fileId = string.Format("{0}_{1}_c", file, is64 ? "x64" : "x86");
                    relativeLocation = string.Format("@\"{0}\\{1}\"", is64 ? "x64" : "x86", file);
                }
                else
                {
                    fileId = string.Format("{0}_{1}_{2}_c", file, is64 ? "x64" : "x86", fix);
                    relativeLocation = string.Format("@\"{0}\\{1}\\{2}\"", is64 ? "x64" : "x86", fix, file);
                }

                string componentRef = string.Format("<ComponentRef Id=\"{0}\" />", fileId);

                string componentDetail = string.Format("<Component Id =\"{0}\" Guid=\"*\">\n  <File Id=\"{0}\" Checksum=\"no\" keyPath=\"yes\" Name=\"{1}\" />\n</Component>", fileId, file);

                fileData.Add(new FileData(file, fileId, relativeLocation, componentRef, componentDetail));
            }

            return fileData;
        }

        private static IList<string> GetFilePaths(string path)
        {
            IList<string> returnFiles = new List<string>();

            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (exclusions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                returnFiles.Add(Path.GetFileName(file));
            }

            return returnFiles;
        }

        private static void WriteToFile(IList<string> items, string splitter, string fileName)
        {
            var asString = string.Join(splitter, items);
            File.WriteAllText(@"C:\Users\yingfand\Desktop\SafeToDelete\" + fileName + ".txt", asString);
        }
    }
}
