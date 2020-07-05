using EyeAuras.Shared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Models
{
    internal interface IAuraCore : IAuraModel
    {
        string Name { get; }
        
        string Description { get; }
        
        IAuraModelController ModelController { get; set; }
    }
}