using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PoeShared.Native;

namespace EyeAuras.CsScriptAuras.Resources
{
    public class PreloadedHighlightingsManager
    {
        public IEnumerable<string> GetAvailableHighlightings()
        {
            return ResourceReader.TryToLoadResourcesByName(Assembly.GetExecutingAssembly(), new Regex($@"{GetType().Namespace}.*\.xml"))
                .Select(x => Encoding.Default.GetString(x))
                .Where(x => !string.IsNullOrEmpty(x));
        }

    }
}