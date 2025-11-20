using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.XtraReports.Import.Services {
    internal sealed class ReportConverterService : IReportConverterService {

        private static readonly Lazy<ReportConverterService> _lazyInstance = new Lazy<ReportConverterService>(() => new ReportConverterService());
        private ReportConverterService() {            
        }
        public static IReportConverterService Instance {
            get { return _lazyInstance.Value; }
        }

        public void ConvertCrystalReport(string sourcePath, string destinationPath) {
            try {
                ConfigureTracer();
                ConverterBase converter = CreateConverter(Path.GetExtension(sourcePath), new Dictionary<string, string>(), destinationPath);
                ConversionResult conversionResult = converter.Convert(sourcePath);
                conversionResult.TargetReport.SaveLayoutToXml(destinationPath);
            } catch (Exception ex) {
                Console.WriteLine(ex.GetBaseException().Message + Environment.NewLine);
                Console.WriteLine();
                if (!(ex is ArgumentCommandLineException)) {
                    Console.WriteLine();
                }
                WriteInfo();
            }
        }
        static void WriteInfo() {
            string[] infos = new string[] {
                    "Imports report files of different types into an XtaReport class file.\r\n",
                    "Usage:",
                    "ReportsImport /in:path1 /out:path2\r\n",
                    "              path1 Specifies the input file's location and type.",
#if Access
                    "                    *.mdb or *.mde file matches MS Access reports.",
                    "              /access:ReportIndex=[Number]",
                    "              /access:ReportName=[String]",
#endif
#if Active
                    "                    *.rpx file matches ActiveReports.",
#endif
#if Crystal
                    "                    *.rpt file matches Crystal Reports.",
                    "              /crystal:UnrecognizedFunctionBehavior=Ignore",
#endif
                    "                    *.rdl or *.rdlc file matches MS SQL Server Reporting Services.",
                    "              /ssrs:UnrecognizedFunctionBehavior=Ignore",
                    "",
                    "              path2 Specifies the output file's location.\r\n",
                    @"For more information, see https://github.com/DevExpress/Reporting.Import"
                };
            foreach (string s in infos)
                Console.WriteLine(s);
        }
        static ConverterBase CreateConverter(string extension, Dictionary<string, string> argDictionary, string outputPath) {

            Func<string, bool> ExtensionEquals = (extensionCompare)
                => string.Equals(extension, extensionCompare, StringComparison.InvariantCultureIgnoreCase);

#if Access
            if(ExtensionEquals(".mdb") || ExtensionEquals(".mde")) {
                AccessReportSelectionForm.AccessIconResourceName = typeof(AccessConverter).Namespace + ".Import.AccessReport.bmp";
                Dictionary<string, string> accessProperties = CreateSubArg(argDictionary, "/access");
                string reportName;
                accessProperties.TryGetValue("ReportName", out reportName);
                string reportIndexStr;
                int? reportIndex = null;
                if(accessProperties.TryGetValue("ReportIndex", out reportIndexStr)) {
                    int reportIndexLocal;
                    if(int.TryParse(reportIndexStr, out reportIndexLocal))
                        reportIndex = reportIndexLocal;
                }
                return new AccessConverter(reportName, reportIndex);
            }
#endif
#if Active
            if(ExtensionEquals(".rpx"))
                return new ActiveReportsConverter();
#endif
#if Crystal
            if (ExtensionEquals(".rpt")) {
                Dictionary<string, string> crystalProperties = CreateSubArg(argDictionary, "/crystal");
                string unrecognizedFunctionBehavior;
                if (crystalProperties.TryGetValue("UnrecognizedFunctionBehavior", out unrecognizedFunctionBehavior)) {
                    CrystalConverter.UnrecognizedFunctionBehavior = string.Equals(unrecognizedFunctionBehavior, nameof(UnrecognizedFunctionBehavior.Ignore))
                        ? UnrecognizedFunctionBehavior.Ignore
                        : UnrecognizedFunctionBehavior.InsertWarning;
                }
                var crystalConverter = new CrystalConverter();
                crystalConverter.SubreportGenerated += (_, e) => Converter_SubreportGenerated(outputPath, e);
                return crystalConverter;
            }
#endif
            if (ExtensionEquals(".rdl") || ExtensionEquals(".rdlc")) {
                Dictionary<string, string> ssrsProperties = CreateSubArg(argDictionary, "/ssrs");
                string unrecognizedFunctionBehavior;
                var reportingServicesConverter = new ReportingServicesConverter();
                if (ssrsProperties.TryGetValue("UnrecognizedFunctionBehavior", out unrecognizedFunctionBehavior)) {
                    reportingServicesConverter.UnrecognizedFunctionBehavior = string.Equals(unrecognizedFunctionBehavior, nameof(UnrecognizedFunctionBehavior.Ignore))
                        ? UnrecognizedFunctionBehavior.Ignore
                        : UnrecognizedFunctionBehavior.InsertWarning;
                }
                string ignoreQueryValidation;
                if (ssrsProperties.TryGetValue("IgnoreQueryValidation", out ignoreQueryValidation)) {
                    reportingServicesConverter.IgnoreQueryValidation = bool.Parse(ignoreQueryValidation);
                }
                string useTablixStaticGroups;
                if (ssrsProperties.TryGetValue("UseTablixStaticGroups", out useTablixStaticGroups)) {
                    reportingServicesConverter.UseTablixStaticGroups = bool.Parse(useTablixStaticGroups);
                }
                string saveTablixGroups;
                if (ssrsProperties.TryGetValue("SaveTablixGroups", out saveTablixGroups)) {
                    reportingServicesConverter.SaveTablixGroups = bool.Parse(saveTablixGroups);
                }
                return reportingServicesConverter;
            }
            throw new ArgumentCommandLineException($"File extension '{extension}' is not supported.");
        }

        static Dictionary<string, string> CreateSubArg(Dictionary<string, string> argDictionary, string key) {
            string subArgumentsString;
            if (!argDictionary.TryGetValue(key, out subArgumentsString))
                return new Dictionary<string, string>();
            Dictionary<string, string> subArgDictionary = subArgumentsString
                .Split(';')
                .Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(x => x[0], x => x.Length == 2 ? x[1] : null, StringComparer.OrdinalIgnoreCase);
            return subArgDictionary;
        }
        static void ConfigureTracer() {
            var traceSource = XtraPrinting.Tracer.GetSource("DXperience.Reporting", System.Diagnostics.SourceLevels.Error | System.Diagnostics.SourceLevels.Warning);
            var listener = new System.Diagnostics.ConsoleTraceListener();
            traceSource.Listeners.Add(listener);
        }
        static void Converter_SubreportGenerated(string outputFile, CrystalConverterSubreportGeneratedEventArgs e) {
            var subreportFile = Path.Combine(
                Path.GetDirectoryName(outputFile),
                Path.GetFileNameWithoutExtension(outputFile) + "_" + EscapeFileName(e.OriginalSubreportName) + Path.GetExtension(outputFile));
            e.SubReport.SaveLayoutToXml(subreportFile);
            e.SubreportControl.ReportSourceUrl = subreportFile;
        }
        static string EscapeFileName(string originalSubreportName) {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                originalSubreportName = originalSubreportName.Replace(invalidChar, '_');
            return originalSubreportName;
        }
    }
}
