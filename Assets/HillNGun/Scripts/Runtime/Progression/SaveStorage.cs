using System;
using System.IO;
using UnityEngine;

namespace HillNGun.Progression
{
    /// <summary>Where the save lives. Offline file now; a cloud save would be another implementation (external stub, M-C).</summary>
    public interface ISaveStorage
    {
        string Location { get; }
        bool TryRead(out string json);
        void Write(string json);
        void Delete();
    }

    /// <summary>JSON file in Application.persistentDataPath, written atomically (temp file then move).</summary>
    public class FileSaveStorage : ISaveStorage
    {
        private readonly string _path;
        public string Location => _path;

        public FileSaveStorage(string fileName)
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public bool TryRead(out string json)
        {
            json = null;
            try
            {
                if (!File.Exists(_path)) return false;
                json = File.ReadAllText(_path);
                return !string.IsNullOrEmpty(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Read failed {_path}: {e.Message}");
                return false;
            }
        }

        public void Write(string json)
        {
            try
            {
                string tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Write failed {_path}: {e.Message}");
            }
        }

        public void Delete()
        {
            try { if (File.Exists(_path)) File.Delete(_path); } catch { }
        }
    }
}
