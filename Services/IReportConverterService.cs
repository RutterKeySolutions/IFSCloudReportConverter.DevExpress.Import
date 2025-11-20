using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.XtraReports.Import.Services {
    public interface IReportConverterService {
        Exception ConvertCrystalReport(string sourcePath, string destinationPath, Dictionary<string, string> argDictionary, TraceListener traceListener);
        Exception ConvertSsrsReport(string sourcePath, string destinationPath, Dictionary<string, string> argDictionary, TraceListener traceListener);
    }
}
