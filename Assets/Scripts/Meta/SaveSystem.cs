using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildBuild.Meta
{
    [Serializable]
    public class OwnedUnit
    {
        public string id;
        public int level = 1;
        public int stars;
        public int shards;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int rollsSinceSR;
        public int rollsSinceSSR;
        public List<OwnedUnit> owned = new List<OwnedUnit>();

        /// <summary>Wave se danh o lan bam tiep theo — song qua viec doi sang man gacha va tat game.</summary>
        public int nextWave = 1;
        /// <summary>Doi hinh 9 o, theo thu tu o; moi phan tu la ten asset HeroDefinition, "" = o trong.</summary>
        public List<string> lineup = new List<string>();
        /// <summary>Phao tren 3 be phao dai, tu tren xuong; moi phan tu la ten asset CannonDefinition, "" = be trong.</summary>
        public List<string> cannons = new List<string>();

        public OwnedUnit Find(string id) => owned.Find(o => o.id == id);
    }

    /// <summary>
    /// Save JSON o `Application.persistentDataPath/save.json` (GDD muc 9, chot 2026-09-11).
    /// Thua KHONG xoa gi o day: vang, so huu, level, pity deu song qua lan thua (GDD muc 1).
    /// </summary>
    public static class SaveSystem
    {
        static SaveData _data;

        public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
        public static SaveData Data => _data ??= Load();

        public static void Save()
        {
            // Ghi ra file tam roi moi thay: tat game giua luc ghi thi van con ban cu nguyen ven.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        static SaveData Load()
        {
            if (!File.Exists(FilePath)) return new SaveData();
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
            }
            catch (Exception e)
            {
                string backup = FilePath + ".corrupt";
                File.Copy(FilePath, backup, true);
                Debug.LogError($"[SaveSystem] Doc save that bai ({e.Message}). Ban hong giu o {backup}, bat dau save moi.");
                return new SaveData();
            }
        }

        // Tat Domain Reload khi vao Play Mode thi bien static song qua cac lan Play — doc lai tu file.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _data = null;
    }
}
