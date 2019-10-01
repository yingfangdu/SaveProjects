using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetMissingApiErrorCodes
{
    class Program
    {
        static void Main(string[] args)
        {

            var apiErrorCodes = GetApiErrorCodes();
            var baeErrorCodes = GetBAESyncStrings();

            var notinBAE = apiErrorCodes.Except(baeErrorCodes).ToList();

            if (notinBAE.Count() > 0)
            {
                Console.WriteLine("generated sync strings saved at " + ExtractMissingSyncStrings(notinBAE));
            }
            else
            {
                Console.WriteLine("no differences found between the API error codes and BAE SyncStrings");
            }

            Console.ReadLine();
        }


        private static List<int> GetApiErrorCodes()
        {
            // TODO : Step 1
            /// get latest api error code 
            /// from https://msasg.visualstudio.com/Bing_Ads/_git/AdsAppsMT?path=%2Fprivate%2FCampaign%2FMT%2FSource%2FAPI%2FMergedAPI%2FV12%2FShared%2FTranslation%2FApiErrorCode.cs&version=GBmaster
            /// and paste it into ErrorCodes.cs file 

            return typeof(Microsoft.AdCenter.Shared.Api.ErrorCodes).GetFields().Select(p => int.Parse(p.GetValue(null).ToString())).ToList();
        }

        private static List<int> GetBAESyncStrings()
        {
            /// TODO : Step 2
            /// get latest file from 
            /// https://msasg.visualstudio.com/DefaultCollection/Bing_Ads/_git/BAE?path=%2Fsrc%2Fui%2FResources%2FSyncStrings.resx&version=GBmaster
            /// and paste it into BAESyncStrings.xml

            var xmldocument = new System.Xml.XmlDocument();
            xmldocument.Load("BAESyncStrings.xml");
            var regex = new System.Text.RegularExpressions.Regex(@"SyncError_?(\d*)");

            List<int> errorcodes = new List<int>();

            foreach (System.Xml.XmlNode node in xmldocument.SelectNodes("./root/data"))
            {
                var name = node.Attributes["name"] == null ? string.Empty : node.Attributes["name"].Value.ToString();
                if (name != null && name.Contains("SyncError"))
                {
                    int value = int.Parse(regex.Match(name).Groups[1].Value);
                    errorcodes.Add(value);
                }
            }

            return errorcodes;

        }

        private static string ExtractMissingSyncStrings(List<int> errorCodes)
        {
            /// TODO : Step 3
            /// get latest api error message from 
            /// https://msasg.visualstudio.com/Bing_Ads/_git/AdsAppsMT?path=%2Fprivate%2FCampaign%2FMT%2FSource%2FAPI%2FMergedAPI%2FV13%2FShared%2FTranslation%2FMasterApiErrorCodeList.xml&version=GBmaster
            /// and paste it into MasterApiErrorCodeList.xml

            var xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.Load("MasterApiErrorCodeList.xml");
            StringBuilder stringBuilder = new StringBuilder();

            System.Xml.XmlNamespaceManager ns = new System.Xml.XmlNamespaceManager(xmlDocument.NameTable);
            ns.AddNamespace("my", "http://adcenter.microsoft.com/advertiser/v5/XMLSchema");

            foreach (var item in errorCodes)
            {
                var nodelist = xmlDocument.SelectNodes("//my:ApiErrorCodes/my:ErrorCodesByCategory/my:category/my:error[@code=\'" + item + "\']", ns);

                var node = nodelist[0];
                var value = string.Format("<data name=\"SyncError_{0}\" xml:space=\"preserve\">\n" +
                    "   <value>{1}</value>\n" +
                    "   <comment> ApI Error: UA Please review </comment>\n" +
                    "</data>", item, node.Attributes["message"].Value);

                stringBuilder.AppendLine(value);
            }

            string filename = "GeneratedSyncStrings.xml";
            System.IO.File.WriteAllText(filename, stringBuilder.ToString());

            var info = new System.IO.FileInfo(filename);

            return info.FullName;
        }
    }
}
