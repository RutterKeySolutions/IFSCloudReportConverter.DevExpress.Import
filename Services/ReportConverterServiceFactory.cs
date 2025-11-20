using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.XtraReports.Import.Services {
    public class ReportConverterServiceFactory {
        private static readonly Lazy<ReportConverterServiceFactory> _lazyInstance = new Lazy<ReportConverterServiceFactory>(() => new ReportConverterServiceFactory());
        private ReportConverterServiceFactory() {
        }
        public static ReportConverterServiceFactory Instance {
            get { return _lazyInstance.Value; }
        }
        public IReportConverterService GetReportConverterService() {
            return ReportConverterService.Instance;
        }
    }
}
