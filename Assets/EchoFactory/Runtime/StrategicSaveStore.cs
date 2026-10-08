using System;
using System.IO;
using System.Text;
using EchoFactory.Core;
using UnityEngine;
namespace EchoFactory.Runtime
{
    // Disk storage of the strategic layer; same temp-file + .bak scheme as LocalSave.
    public static class StrategicSaveStore
    {
        public static string PathName { get { return Path.Combine(Application.persistentDataPath, "echo-factory-strategy-v1.json"); } }
        public static bool Exists() { return File.Exists(PathName); }
        public static StrategicState Load()
        {
            var state = Read(PathName);
            if (state != null) return state;
            state = Read(PathName + ".bak");
            if (state == null) throw new InvalidDataException("Zapis strategii jest uszkodzony i nie ma poprawnej kopii.");
            return state;
        }
        private static StrategicState Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = JsonUtility.FromJson<StrategicSave>(File.ReadAllText(path)); string reason;
                return data != null && data.Valid(out reason) ? data.ToState() : null;
            }
            catch (Exception) { return null; }
        }
        public static void Save(StrategicState state)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string temp = PathName + ".tmp"; byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(StrategicSave.From(state), true));
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak"); else File.Move(temp, PathName);
        }
    }
}
