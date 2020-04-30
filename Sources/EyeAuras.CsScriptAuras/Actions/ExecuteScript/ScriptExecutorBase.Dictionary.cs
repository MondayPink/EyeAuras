using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DynamicData;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public abstract partial class ScriptExecutorBase
    {
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return AuraContext.Variables.Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            AuraContext[item.Key] = item.Value;
        }

        public void Clear()
        {
            AuraContext.Variables.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ContainsKey(item.Key) && AuraContext[item.Key] == item.Value;
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Remove(item.Key);
        }

        public int Count => AuraContext.Variables.Count;

        public bool IsReadOnly { get; } = false;

        public void Add(string key, object value)
        {
            AuraContext[key] = value;
        }

        public bool ContainsKey(string key)
        {
            return AuraContext.Variables.Lookup(key).HasValue;
        }

        public bool Remove(string key)
        {
            var result = false;
            AuraContext.Variables.Edit(x =>
            {
                if (!x.Lookup(key).HasValue)
                {
                    return;
                }

                result = true;
                x.Remove(key);
            });
            return result;
        }

        public bool TryGetValue(string key, out object value)
        {
            var result = AuraContext.Variables.Lookup(key);
            if (result.HasValue)
            {
                value = result.Value.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            if (!TryGetValue(key, out var untyped))
            {
                value = default;
                return false;
            }

            value = (T) untyped;
            return true;
        }

        public object this[string key]
        {
            get => AuraContext[key];
            set => AuraContext[key] = value;
        }

        public ICollection<string> Keys => AuraContext.Variables.Keys.ToImmutableList();
        
        public ICollection<object> Values => AuraContext.Variables.KeyValues.Select(x => x.Value).Select(x => x.Value).ToImmutableList();
    }
}