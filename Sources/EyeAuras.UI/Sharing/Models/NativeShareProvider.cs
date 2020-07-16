using System.Threading.Tasks;
using EyeAuras.UI.Sharing.Services;

namespace EyeAuras.UI.Sharing.Models
{
    internal class NativeShareProvider : IShareProvider
    {
        public string Name { get; } = "EyeAuras";
        
        public Task Upload(string data)
        {
            throw new System.NotImplementedException();
        }
    }
}