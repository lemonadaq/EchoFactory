using System;
using System.IO;
using System.Text;
using EchoFactory.Core;
using UnityEngine;
namespace EchoFactory.Runtime
{
    public static class LocalSave
    {
        public static string PathName { get { return Path.Combine(Application.persistentDataPath, "echo-factory-v1.json"); } }
        public static Campaign Load()
        {
            if (!File.Exists(PathName)) return new Campaign();
            var data = JsonUtility.FromJson<Campaign>(File.ReadAllText(PathName)); string reason = "Nieprawidłowy JSON.";
            if (data == null || !data.Valid(out reason)) throw new InvalidDataException("Zapis nie został nadpisany. " + reason);
            return data;
        }
        public static void Save(Campaign data)
        {
            string reason; if (!data.Valid(out reason)) throw new InvalidDataException(reason);
            Directory.CreateDirectory(Application.persistentDataPath);
            string temp = PathName + ".tmp"; byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak"); else File.Move(temp, PathName);
        }
        public static void RestoreBackup()
        {
            string backup = PathName + ".bak"; if (!File.Exists(backup)) throw new FileNotFoundException("Brak kopii zapasowej.");
            var data = JsonUtility.FromJson<Campaign>(File.ReadAllText(backup)); string reason = "Nieprawidłowa kopia.";
            if (data == null || !data.Valid(out reason)) throw new InvalidDataException(reason);
            if (File.Exists(PathName)) File.Copy(PathName, PathName + ".recovery-" + DateTime.UtcNow.Ticks, false);
            File.Copy(backup, PathName, true);
        }
    }
}
