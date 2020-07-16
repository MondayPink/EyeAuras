using EyeAuras.UI.Core.Models;

namespace EyeAuras.UI.MainWindow.Models
{
    internal interface IAuraSerializer
    {
        string Serialize(object data);

        OverlayAuraProperties[] Deserialize(string content);
    }
}