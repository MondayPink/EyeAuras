using System;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using Guards;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class WindowMatcher : IWindowMatcher
    {
        public bool IsMatch(WindowHandle window, WindowMatchParams matchParams)
        {
            Guard.ArgumentNotNull(window, nameof(window));

            if (matchParams.Handle != IntPtr.Zero && window.Handle == matchParams.Handle)
            {
                return true;
            }
            
            if (string.IsNullOrEmpty(matchParams.Title))
            {
                return false;
            }
            var matchExpression = matchParams.Title.Trim('\"', '\'');

            if (matchExpression.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                matchExpression.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
            {
                var windowHandle = matchExpression.ToIntOrDefault();
                if (windowHandle != null)
                {
                    var windowHandleMatches = window.Handle.ToInt64() == windowHandle;
                    if (windowHandleMatches)
                    {
                        return true;
                    }
                }
            }
            
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