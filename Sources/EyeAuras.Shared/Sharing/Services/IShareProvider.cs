using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EyeAuras.Shared.Sharing.Services
{
    public interface IShareProvider
    {
        string Name { get; }
        
        Task<Uri> UploadProperties(IAuraProperties[] data);
        
        Task<IAuraProperties[]> DownloadProperties(Uri uri);
    }
}