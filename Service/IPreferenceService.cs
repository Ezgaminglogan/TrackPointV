namespace TrackPointV.Service
{
    public interface IPreferenceService
    {
        void Set<T>(string key, T value);
        T Get<T>(string key, T defaultValue);
        bool ContainsKey(string key);
        void Remove(string key);
        void Clear();
    }
} 