using System.Threading.Tasks;

namespace EyeAuras.UI.Sharing.Services
{
    internal interface IShareProvider
    {
        string Name { get; }
        
        Task Upload(string data);
    }
}