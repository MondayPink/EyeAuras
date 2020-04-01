using System;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using Guards;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class WindowMatcher : IWindowMatcher
    {
        public bool IsMatch(WindowHandle window, WindowMatchParams matchParams)
        {
            Guard.ArgumentNotNull(window, nameof(window));

            if (string.IsNullOrEmpty(matchParams.Title))
            {
                return false;
            }

            var matchExpression = matchParams.Title.Trim('\"', '\'');
            
            var processPathMatches = window.ProcessPath?.Equals(matchExpression, StringComparison.OrdinalIgnoreCase) ?? false;
            if (processPathMatches)
            {
                return true;
            }
            
            var processNameMatches = window.ProcessName?.Equals(matchExpression, StringComparison.OrdinalIgnoreCase) ?? false;
            if (processNameMatches)
            {
                return true;
            }
            
            var titleMatches = window.Title?.Contains(matchExpression, StringComparison.OrdinalIgnoreCase) ?? false;
            if (titleMatches)
            {
                return true;
            }

            return false;   
        }
    }
}