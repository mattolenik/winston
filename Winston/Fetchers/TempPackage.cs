using System;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Fetchers
{
    public class TempPackage : IDisposable
    {
        public Package Package { get; set; }
        public IDisposablePathItem PackageItem { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }

#pragma warning disable CC0029 // Disposables Should Call Suppress Finalize
        public void Dispose() => PackageItem?.Dispose();
#pragma warning restore CC0029 // Disposables Should Call Suppress Finalize
    }
}