using System;
using System.Text.RegularExpressions;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using Guards;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class WindowMatcher : IWindowMatcher
    {
        public bool IsMatch(IWindowHandle window, WindowMatchParams matchParams)
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

            var matchExpression = matchParams.Title;

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

            var isExactExpression = IsSurroundedWithValue(matchExpression, "\"") || IsSurroundedWithValue(matchExpression, "'");
            var isRegexExpression = matchParams.IsRegex || IsSurroundedWithValue(matchExpression, "/");
            
            var trimmedExpression = matchExpression.Trim('\"', '\'', '/');

            if (isExactExpression)
            {
                var exactProcessPathMatch = MatchString(trimmedExpression, window.ProcessPath, true);
                if (exactProcessPathMatch)
                {
                    return true;
                }
            }
            else
            {
                var partialProcessPathMatch = isRegexExpression
                    ? MatchRegex(trimmedExpression, window.ProcessPath, false) 
                    : MatchString(trimmedExpression, window.ProcessPath, false);
                if (partialProcessPathMatch)
                {
                    return true;
                }
            }
            
            var processNameMatches = MatchString(trimmedExpression, window.ProcessName, true);
            if (processNameMatches)
            {
                return true;
            }

            if (isExactExpression)
            {
                var exactTitleMatches = MatchString(trimmedExpression, window.Title, true);
                if (exactTitleMatches)
                {
                    return true;
                }
            }
            else
            {
                var partialTitleMatches = isRegexExpression
                    ? MatchRegex(trimmedExpression, window.Title, false) 
                    : MatchString(trimmedExpression, window.Title, false);
                if (partialTitleMatches)
                {
                    return true;
                }
            }
            
            return false;   
        }

        private static bool IsSurroundedWithValue(string input, string value)
        {
            return input.StartsWith(value) && input.EndsWith(value);
        }
        
        private static bool MatchRegex(string regex, string haystack, bool exactMatch)
        {
            if (regex == null || haystack == null)
            {
                return false;
            }

            return Regex.IsMatch(haystack, regex, RegexOptions.IgnoreCase);
        }

        private static bool MatchString(string needle, string haystack, bool exactMatch)
        {
            if (needle == null || haystack == null)
            {
                return false;
            }
            return exactMatch 
                ? haystack?.Equals(needle, StringComparison.OrdinalIgnoreCase) ?? false
                : haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}