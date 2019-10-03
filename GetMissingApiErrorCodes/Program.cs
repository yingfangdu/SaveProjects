namespace GetMissingApiErrorCodes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    internal class ErrorNode
    {
        public string Name { get; set; }

        public string CodeInText { get; set; }

        public int CodeInInt { get; set; }

        public string Message { get; set; }

        // Save the message from last check in.
        public string OriginalMessage { get; set; }
    }

    class Program
    {
        static string SyncStringDirectory = null;
        static string DesignFile = null;
        static string ResxFile = null;

        static void Main(string[] args)
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var repoPath = exePath.Substring(0, exePath.IndexOf(@"\tools\"));
            SyncStringDirectory = Path.Combine(repoPath, @"src\ui\Resources");
            DesignFile = Path.Combine(SyncStringDirectory, "SyncStrings.Designer.cs");
            ResxFile = Path.Combine(SyncStringDirectory, "SyncStrings.resx");

            if (!File.Exists(DesignFile) || !File.Exists(ResxFile))
            {
                Console.WriteLine("Cannot find the resource files. Please check the file path.");
                return;
            }

            // Error Codes from MT API table.
            var apiErrorCodes = GetApiErrorCodesFromMT();
            if (apiErrorCodes.Count != apiErrorCodes.Distinct().Count())
            {
                throw new InvalidOperationException("It has duplicate error codes in MT table");
            }

            // Error messages from MT string table.
            var errorMessagesFromMT = GetErrorMessagesFromMTTable();

            var mtErrorCodes = errorMessagesFromMT.Select(item => item.CodeInInt).ToList();
            if (errorMessagesFromMT.Count != errorMessagesFromMT.Distinct().Count())
            {
                throw new InvalidOperationException("It has duplicate error codes in MT error string table");
            }

            // Error Codes from MAE.
            var errorMessagesFromMAE = GetErrorMessagesFromMAETable();

            // Only return SyncError_*
            var syncErrorMessagesFromMAE = errorMessagesFromMAE.Where(item => item.Name.StartsWith("SyncError_")).ToList();
            var maeErrorCodes = syncErrorMessagesFromMAE.Select(item => item.CodeInInt).ToList();

            if (maeErrorCodes.Count != maeErrorCodes.Distinct().Count())
            {
                throw new InvalidOperationException("It has duplicate Sync error codes in MAE error string table");
            }

            // For newly added strings and updated strings, we always put ApI Error: UA Please review to indicate that these need review from UX team.
            // And we always put the string in comment too. So that we can compare it for next run.
            const string MAESyncStringFormat = "<data name=\"SyncError_{0}\" xml:space=\"preserve\">\n" +
                                               "    <value>{1}</value>\n" +
                                               "    <comment>ApI Error: UA Please review. [{1}]</comment>\n" +
                                               "</data>";

            StringBuilder output = new StringBuilder();

            // Missing Error Codes.
            var notinMAE = apiErrorCodes.Except(maeErrorCodes).ToList();
            output.AppendLine($"Missing Error Codes: {notinMAE.Count} count \n");
            if (notinMAE.Count() > 0)
            {
                // This is the order of VS's resx file.
                notinMAE = notinMAE.OrderByDescending(item => item.ToString()).Reverse().ToList();
                foreach (var item in notinMAE)
                {
                    var errorNodeMT = errorMessagesFromMT.Where(error => error.CodeInInt == item).FirstOrDefault();

                    if (errorNodeMT == null)
                    {
                        throw new InvalidOperationException("This looks not correct in MT error string table.");
                    }

                    output.AppendLine(string.Format(MAESyncStringFormat, item, errorNodeMT.Message));
                }
            }

            // Update Existing Error Codes.
            var staleErrors = new List<string>();
            var updatedNodes = new List<ErrorNode>();
            foreach (var item in syncErrorMessagesFromMAE)
            {
                var errorNodeMT = errorMessagesFromMT.Where(error => error.CodeInInt == item.CodeInInt).FirstOrDefault();
                if (errorNodeMT == null)
                {
                    // MT has removed this error and MAE will remove it too.
                    staleErrors.Add(item.Name);
                    continue;
                }

                // Same messages from MT and MAE error table.
                if (errorNodeMT.Message.Equals(item.Message))
                {
                    continue;
                }
                // Check whether UX has scrubbed the string, if so MAE does not want to change this time.
                else if (!string.IsNullOrEmpty(item.OriginalMessage) && errorNodeMT.Message.Equals(item.OriginalMessage))
                {
                    continue;
                }

                // Need to update and UX scrub for this item.
                updatedNodes.Add(new ErrorNode()
                {
                    Name = item.Name,
                    CodeInInt = item.CodeInInt,
                    Message = errorNodeMT.Message,
                });
            }

            output.AppendLine($"\nUpdate Error Codes: {updatedNodes.Count} count\n");

            foreach (var item in updatedNodes)
            {
                output.AppendLine(string.Format(MAESyncStringFormat, item.CodeInInt, item.Message));
            }

            output.AppendLine($"\nStale Error Codes: {staleErrors.Count} count");
            foreach (var item in staleErrors)
            {
                output.AppendLine(item);
            }

            // Write the output for review.
            string filename = "GeneratedSyncStrings.xml";
            File.WriteAllText(filename, output.ToString());

            // Summary.
            Console.WriteLine($"There are totally:\n");
            Console.WriteLine($"Missed: {notinMAE.Count}");
            Console.WriteLine($"Updated: {updatedNodes.Count}");
            Console.WriteLine($"Deleted: {staleErrors.Count}");
            Console.WriteLine($"Check the result in {filename}");

            // TODO: you need to manually add new items.
            // and below code will automatically delete the stale items and update the existing ones from SyncString.resx.
            Console.WriteLine("Going to automatically update the existing items and delete the stale items......\n");
            UpdateExistingMAESyncStringTable(updatedNodes, staleErrors);

            Console.WriteLine("Auto update is done.");
            Console.WriteLine("Please manually add the missing items.");
            Console.ReadLine();
        }


        private static List<int> GetApiErrorCodesFromMT()
        {
            // TODO : Step 1
            /// get latest api error code 
            /// from https://msasg.visualstudio.com/Bing_Ads/_git/AdsAppsMT?path=%2Fprivate%2FCampaign%2FMT%2FSource%2FAPI%2FMergedAPI%2FV13%2FSharedCore%2FTranslation%2FApiErrorCode.cs&version=GBmaster
            /// and paste it into ErrorCodes.cs file 

            return typeof(Microsoft.AdCenter.Shared.Api.V13.ErrorCodes).GetFields().Select(p => int.Parse(p.GetValue(null).ToString())).ToList();
        }

        private static List<ErrorNode> GetErrorMessagesFromMTTable()
        {
            /// TODO : Step 2
            /// get latest api error message from
            /// https://msasg.visualstudio.com/Bing_Ads/_git/AdsAppsMT?path=%2Fprivate%2FCampaign%2FMT%2FSource%2FAPI%2FMergedAPI%2FV13%2FSharedCore%2FTranslation%2FMasterApiErrorCodeList.xml&version=GBmaster
            /// and paste it into MasterApiErrorCodeList.xml

            var xmlDocument = XDocument.Load("MasterApiErrorCodeList.xml");
            var allNodes = xmlDocument.Descendants();

            // Now only reads the <error /> nodes.
            var errorNodes = allNodes.Where(x => x.Name.LocalName.Equals("error")).ToList();

            var errorNodesList = new List<ErrorNode>();
            foreach (var errorNode in errorNodes)
            {
                errorNodesList.Add(new ErrorNode()
                {
                    Name = errorNode.Attribute("name").Value,
                    CodeInInt = Convert.ToInt32(errorNode.Attribute("code").Value),
                    CodeInText = errorNode.Attribute("errorcode").Value,
                    Message = errorNode.Attribute("message").Value
                });
            }

            // Sort as as string because this is VS's resx file's sort order.
            errorNodesList = errorNodesList.OrderByDescending(item => item.CodeInInt.ToString()).ToList();

            // want ascending.
            errorNodesList.Reverse();

            return errorNodesList;
        }

        private static List<ErrorNode> GetErrorMessagesFromMAETable()
        {
            var xmlDocument = XDocument.Load(ResxFile);
            var allNodes = xmlDocument.Descendants();
            var errorNodes = allNodes.Where(x => x.Name.LocalName.Equals("data")).ToList();

            var regexErrorCode = new Regex(@"(Sync[a-z,A-Z]*|Ksp)(Error|Warning)_(\d*)");
            var regexOriginalMessage = new Regex(@"\[(.*)\]");

            var errorNodesList = new List<ErrorNode>();

#if DEBUG
            var group1 = new List<string>();
            var group2 = new List<string>();
#endif//DEBUG

            foreach (var errorNode in errorNodes)
            {
                var errorCodeName = errorNode.Attribute("name").Value;
                var valueNode = errorNode.Descendants().Where(x => x.Name.LocalName.Equals("value")).FirstOrDefault();
                var errorCodeInInt = int.Parse(regexErrorCode.Match(errorCodeName).Groups[3].Value);
                string originalMessage = null;

                var commentNode = errorNode.Descendants().Where(x => x.Name.LocalName.Equals("comment")).FirstOrDefault();
                if (commentNode != null)
                {
                    var match = regexOriginalMessage.Match(commentNode.Value);
                    if (match.Success)
                    {
                        originalMessage = match.Groups[1].Value;
                    }
                }
#if DEBUG
                group1.Add(regexErrorCode.Match(errorCodeName).Groups[1].Value);
                group2.Add(regexErrorCode.Match(errorCodeName).Groups[2].Value);
#endif//DEBUG
                errorNodesList.Add(new ErrorNode()
                {
                    Name = errorCodeName,
                    CodeInInt = errorCodeInInt,
                    CodeInText = errorCodeInInt.ToString(),
                    Message = valueNode.Value,
                    OriginalMessage = originalMessage,
                });
            }

#if DEBUG
            Console.WriteLine("Group 1: " + string.Join(", ", group1.Distinct().ToArray()) + "\n\n");
            Console.WriteLine("Group 2: " + string.Join(", ", group2.Distinct().ToArray()) + "\n\n");
#endif//DEBUG

            // Sort as as string because this is VS's resx file's sort order.
            errorNodesList = errorNodesList.OrderByDescending(item => item.Name).ToList();

            // want ascending.
            errorNodesList.Reverse();
            return errorNodesList;
        }

        private static void UpdateExistingMAESyncStringTable(List<ErrorNode> updatedNodes, List<string> staleErrors)
        {
            UpdateSyncDesignerFile(DesignFile, updatedNodes, staleErrors);
            UpdateSyncRESXFile(ResxFile, updatedNodes, staleErrors);
        }

        private static void UpdateSyncDesignerFile(string designFile, List<ErrorNode> updatedNodes, List<string> staleNodes)
        {
            const string SearchFormat = "public static string {0}";
            const string StartOfSummary = "/// <summary>";
            const string EndOfSummary = "/// </summary>";
            var full = File.ReadAllText(designFile);

            // This is to update.
            foreach (var updatedNode in updatedNodes)
            {
                string searchTerm = string.Format(SearchFormat, updatedNode.Name);
                var endIndex = full.IndexOf(searchTerm);
                if (endIndex < 0)
                {
                    throw new InvalidOperationException($"Something wrong in the {designFile} file: {searchTerm}");
                }
                var startIndex = full.LastIndexOf(StartOfSummary, endIndex, endIndex);
                endIndex = full.LastIndexOf(EndOfSummary, endIndex, endIndex);

                var oldStr = full.Substring(startIndex, endIndex - startIndex);

                string newStr = "/// <summary>\r\n" +
                                     $"        ///   Looks up a localized string similar to {updatedNode.Message}.\r\n" +
                                      "        ";
                full = full.Replace(oldStr, newStr);
            }

            // This is to delete.
            foreach (var stale in staleNodes)
            {
                string searchTerm = string.Format(SearchFormat, stale); ;

                var endIndex = full.IndexOf(searchTerm);
                if (endIndex < 0)
                {
                    throw new InvalidOperationException($"Something wrong in the {designFile} file: {searchTerm}");
                }

                var startIndexOfItsSummary = full.LastIndexOf(StartOfSummary, endIndex, endIndex);
                startIndexOfItsSummary -= 8; // delete the eight spaces.

                var startIndexOfNextSummary = full.IndexOf(StartOfSummary, endIndex);
                if (startIndexOfNextSummary < 0)
                {
                    throw new InvalidOperationException($"Edge case is not handled {stale}. Please manually delete it first.");
                }
                startIndexOfNextSummary -= 8; // exclude the eight spaces.

                var oldStr = full.Substring(startIndexOfItsSummary, startIndexOfNextSummary - startIndexOfItsSummary);
                full = full.Replace(oldStr, string.Empty);
            }

            File.WriteAllText(designFile, full);
        }

        private static void UpdateSyncRESXFile(string resxFile, List<ErrorNode> updatedNodes, List<string> staleNodes)
        {
            var xmlDocument = XDocument.Load(resxFile);
            var allNodes = xmlDocument.Descendants().Where(x => x.Name.LocalName.Equals("data"));
            List<XElement> toDeleted = new List<XElement>();

            foreach (var node in allNodes)
            {
                var nodeName = node.Attribute("name").Value;
                var updatedNode = updatedNodes.Where(update => update.Name.Equals(nodeName)).FirstOrDefault();

                // This is to update the node.
                if (updatedNode != null)
                {
                    node.Descendants().Where(x => x.Name.LocalName.Equals("value")).FirstOrDefault().Value = updatedNode.Message;
                    var commentNode = node.Descendants().Where(x => x.Name.LocalName.Equals("comment")).FirstOrDefault();
                    if (commentNode == null)
                    {
                        commentNode = new XElement("comment");
                        node.Add(commentNode);
                        node.Add(Environment.NewLine);
                    }

                    commentNode.Value = $"ApI Error: UA Please review. [{updatedNode.Message}]";

                    continue;
                }

                var deletedNode = staleNodes.Where(stale => stale.Equals(nodeName)).FirstOrDefault();
                if (deletedNode != null)
                {
                    toDeleted.Add(node);

                    continue;
                }

                // Not update nore delete.
            }

            // Delete nodes here, you cannot do that in the above enumeration.
            toDeleted.ForEach(item => item.Remove());

            xmlDocument.Save(resxFile);
        }
    }
}
