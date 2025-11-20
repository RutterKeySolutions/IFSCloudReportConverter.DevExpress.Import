using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.XtraReports.Import.Services {
    public interface IReportConverterService {
        void ConvertCrystalReport(string sourcePath, string destinationPath);
    }
}
