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
            public FileData(string fileName, string componentId, string fileId, string relativeLocation, string componentRef, string componentDetail)
            {
                if (string.IsNullOrEmpty(fileName) ||
                    string.IsNullOrEmpty(componentId) ||
                    string.IsNullOrEmpty(fileId) ||
                    string.IsNullOrEmpty(relativeLocation) ||
                    string.IsNullOrEmpty(componentRef) ||
                    string.IsNullOrEmpty(componentDetail))
                {
                    throw new Exception("error");
                }

                this.FileName = fileName;
                this.ComponentID = componentId;
                this.FileID = fileId;
                this.FileRelativeLocation = relativeLocation;
                this.FileComponentRef = componentRef;
                this.FileComponentDetail = componentDetail;
            }

            public string FileName { get; private set; }

            public string FileRelativeLocation { get; private set; }

            public string ComponentID { get; private set; }

            public string FileID { get; private set; }

            public string FileComponentRef { get; private set; }

            public string FileComponentDetail { get; private set; }
        }

        static void Main(string[] args)
        {
            //GenerateBAE();
            //GenerateResource();
            GenerateCEFPackages();
        }

        private static void GenerateBAE()
        {
            IList<string> dllNames = new List<string>()
            {
                "Ninja.WebSockets.dll",
                "Microsoft.IO.RecyclableMemoryStream.dll",
                "Microsoft.VisualStudio.Threading.dll",
                "Microsoft.VisualStudio.Validation.dll",
                "StreamJsonRpc.dll",
            };

            IList<FileData> fileData = new List<FileData>();

            foreach(var file in dllNames)
            {
                string fileNameWithoutExtension = file.Substring(0, file.LastIndexOf(".dll"));

                string componentId = string.Format("{0}_c", fileNameWithoutExtension);

                string fileId = string.Format("{0}_f", file);

                string componentRef = string.Format("<ComponentRef Id=\"{0}\" />", componentId);

                string componentDetail = string.Format("<Component Id =\"{0}\" Guid=\"*\">\n  <File Id=\"{1}\"\n        Assembly=\".net\"\n        AssemblyApplication=\"Microsoft.AdvertisingDesktop.BingAdsEditor.exe.config_f\"\n        AssemblyManifest=\"{1}\"\n        Checksum=\"no\"\n        KeyPath=\"yes\"\n        Name=\"{2}\" />\n</Component>", componentId, fileId, file);

                fileData.Add(new FileData(fileNameWithoutExtension, componentId, fileId, "NotUsed", componentRef, componentDetail));
            }

            WriteToFile(fileData.Select(item => item.FileComponentRef).ToList(), "\n", "Program_ComponentRefsBAE");

            WriteToFile(fileData.Select(item => item.FileComponentDetail).ToList(), "\n\n", "Program_ComponentsBAE");
        }

        private static void GenerateResource()
        {
            IList<string> resourceDllNames = new List<string>()
            {
                @"cs\Microsoft.VisualStudio.Threading.resources.dll",
                @"cs\Microsoft.VisualStudio.Validation.resources.dll",
                @"cs\StreamJsonRpc.resources.dll",
                @"de\Microsoft.VisualStudio.Threading.resources.dll",
                @"de\Microsoft.VisualStudio.Validation.resources.dll",
                @"de\StreamJsonRpc.resources.dll",
                @"es\Microsoft.VisualStudio.Validation.resources.dll",
                @"es\Microsoft.VisualStudio.Threading.resources.dll",
                @"es\StreamJsonRpc.resources.dll",
                @"fr\Microsoft.VisualStudio.Threading.resources.dll",
                @"fr\Microsoft.VisualStudio.Validation.resources.dll",
                @"fr\StreamJsonRpc.resources.dll",
                @"it\Microsoft.VisualStudio.Threading.resources.dll",
                @"it\Microsoft.VisualStudio.Validation.resources.dll",
                @"it\StreamJsonRpc.resources.dll",
                @"ja\Microsoft.VisualStudio.Threading.resources.dll",
                @"ja\Microsoft.VisualStudio.Validation.resources.dll",
                @"ja\StreamJsonRpc.resources.dll",
                @"ko\Microsoft.VisualStudio.Threading.resources.dll",
                @"ko\Microsoft.VisualStudio.Validation.resources.dll",
                @"ko\StreamJsonRpc.resources.dll",
                @"pl\Microsoft.VisualStudio.Threading.resources.dll",
                @"pl\Microsoft.VisualStudio.Validation.resources.dll",
                @"pl\StreamJsonRpc.resources.dll",
                @"pt-BR\Microsoft.VisualStudio.Threading.resources.dll",
                @"pt-BR\Microsoft.VisualStudio.Validation.resources.dll",
                @"pt-BR\StreamJsonRpc.resources.dll",
                @"ru\Microsoft.VisualStudio.Threading.resources.dll",
                @"ru\Microsoft.VisualStudio.Validation.resources.dll",
                @"ru\StreamJsonRpc.resources.dll",
                @"tr\Microsoft.VisualStudio.Threading.resources.dll",
                @"tr\Microsoft.VisualStudio.Validation.resources.dll",
                @"tr\StreamJsonRpc.resources.dll",
                @"zh-Hans\Microsoft.VisualStudio.Threading.resources.dll",
                @"zh-Hans\Microsoft.VisualStudio.Validation.resources.dll",
                @"zh-Hans\StreamJsonRpc.resources.dll",
                @"zh-Hant\Microsoft.VisualStudio.Threading.resources.dll",
                @"zh-Hant\Microsoft.VisualStudio.Validation.resources.dll",
                @"zh-Hant\StreamJsonRpc.resources.dll",
            };

            IList<FileData> fileData = new List<FileData>();

            foreach (var file in resourceDllNames)
            {
                var indexOf = file.IndexOf('\\');
                string folderName = file.Substring(0, indexOf);

                string fileNameWithoutExtension = file.Substring(indexOf + 1, file.LastIndexOf(".dll") - (indexOf + 1));
                string fileName = file.Substring(indexOf + 1);

                string componentId = string.Format("{0}_{1}_c", folderName, fileNameWithoutExtension);

                string fileId = string.Format("{0}_{1}_f", folderName, fileName);

                string componentRef = string.Format("<ComponentRef Id=\"{0}\" />", componentId);

                string componentDetail = string.Format("<Component Id =\"{0}\" Guid=\"*\">\n  <File Id=\"{1}\"\n        Assembly=\".net\"\n        AssemblyApplication=\"Microsoft.AdvertisingDesktop.BingAdsEditor.exe.config_f\"\n        AssemblyManifest=\"{1}\"\n        Checksum=\"no\"\n        KeyPath=\"yes\"\n        Name=\"{2}\"\n        Vital=\"yes\" />\n</Component>", componentId, fileId, fileName);

                fileData.Add(new FileData(fileNameWithoutExtension, componentId, fileId, "NotUsed", componentRef, componentDetail));
            }

            WriteToFile(fileData.Select(item => item.FileComponentRef).ToList(), "\n", "Sate_ComponentRefsBAE");

            WriteToFile(fileData.Select(item => item.FileComponentDetail).ToList(), "\n\n", "Sate_ComponentsBAE");
        }

        private static void GenerateCEFPackages()
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
            var files = GetFilePaths(path);

            return GenerateFileComponent(files, is64, fix);
        }

        private static IList<FileData> GenerateFileComponent(IList<string> files, bool is64, string fix)
        {
            IList<FileData> fileData = new List<FileData>();

            foreach (var file in files)
            {
                string componentId = null;
                string fileId = null;
                string relativeLocation = null;
                if (string.IsNullOrEmpty(fix))
                {
                    componentId = string.Format("{0}_{1}_c", file, is64 ? "x64" : "x86");
                    fileId = string.Format("{0}_{1}_f", file, is64 ? "x64" : "x86");
                    relativeLocation = string.Format("@\"{0}\\{1}\"", is64 ? "x64" : "x86", file);
                }
                else
                {
                    componentId = string.Format("{0}_{1}_{2}_c", file, is64 ? "x64" : "x86", fix);
                    fileId = string.Format("{0}_{1}_{2}_f", file, is64 ? "x64" : "x86", fix);
                    relativeLocation = string.Format("@\"{0}\\{1}\\{2}\"", is64 ? "x64" : "x86", fix, file);
                }

                string componentRef = string.Format("<ComponentRef Id=\"{0}\" />", componentId);

                string componentDetail = string.Format("<Component Id =\"{0}\" Guid=\"*\">\n  <File Id=\"{1}\" Checksum=\"no\" keyPath=\"yes\" Name=\"{2}\" />\n</Component>", componentId, fileId, file);

                fileData.Add(new FileData(file, componentId, fileId, relativeLocation, componentRef, componentDetail));
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
