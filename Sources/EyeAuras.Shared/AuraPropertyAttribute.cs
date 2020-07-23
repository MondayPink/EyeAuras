using System;

namespace EyeAuras.Shared
{
    /// <summary>
    ///   Marks Properties that will be tracked via INotifyChanged
    ///   When property changes that means that .Properties if this model must be invalidated
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AuraPropertyAttribute : Attribute
    {
    }
}