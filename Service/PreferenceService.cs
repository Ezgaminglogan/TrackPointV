using System.Collections.Generic;

namespace TrackPointV.Service
{
    public class PreferenceService : IPreferenceService
    {
        private readonly Dictionary<string, object> _preferences = new Dictionary<string, object>();

        public void Set<T>(string key, T value)
        {
            if (_preferences.ContainsKey(key))
            {
                _preferences[key] = value;
            }
            else
            {
                _preferences.Add(key, value);
            }
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (_preferences.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return defaultValue;
        }

        public bool ContainsKey(string key)
        {
            return _preferences.ContainsKey(key);
        }

        public void Remove(string key)
        {
            if (_preferences.ContainsKey(key))
            {
                _preferences.Remove(key);
            }
        }

        public void Clear()
        {
            _preferences.Clear();
        }
    }
} 