using System;

namespace Stratton.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataExportTarget : Attribute
    {
        private readonly string _exportType;

        public string ExportType => _exportType;

        public DataExportTarget(string sourceType)
        {
            _exportType = sourceType;
        }
    }
}