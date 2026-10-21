using System.Collections.Generic;

namespace BuildBuild.Meta
{
    /// <summary>Mot goi y ngu canh: id (luu vao save), tieu de, noi dung.</summary>
    public struct ContextTip
    {
        public string id;
        public string title;
        public string body;
    }

    /// <summary>
    /// Goi y ngu canh cho 5 phut dau (GDD muc 10.0 co hop thoai huong dan; game chua co buoc day nao ngoai
    /// nhiem vu Hanh trinh).
    ///
    /// Nguyen tac: moi y chi hien MOT lan trong doi acc (`SaveData.seenTips`), va chi hien khi TRANG THAI SAVE
    /// da toi muc can thiet — khong bao gio chen ngang mot tran dang danh hay mot man khac. Dieu kien doc
    /// thang tu save nen acc cu (da choi truoc khi co he nay) chi nhan dung nhung y con thieu.
    /// </summary>
    public static class ContextTips
    {
        public static bool Seen(string id) => SaveSystem.Data.seenTips.Contains(id);

        public static void MarkSeen(string id)
        {
            if (Seen(id)) return;
            SaveSystem.Data.seenTips.Add(id);
            SaveSystem.Save();
        }

        /// <summary>Goi y tiep theo chua xem va da dung luc; null = khong con gi de noi.</summary>
        public static ContextTip? Next()
        {
            var save = SaveSystem.Data;
            var seen = new HashSet<string>(save.seenTips);

            // 1. Ngay lan dau: tran tu dong — nguoi choi can biet minh chi can bam Fight.
            if (!seen.Contains("fight") && save.nextWave == 1)
                return Make("fight", "Your Line Holds",
                    "Tap FIGHT WAVE to begin. Heroes, soldiers and towers fight on their own — your job is to spend gold between waves.");

            // 2. Con o hero trong khi wave da bat dau kho: khong biet bam o trong la khong bao gio lap day san.
            if (!seen.Contains("slots") && save.nextWave >= 3)
                return Make("slots", "Fill The Slots",
                    "Tap an empty hero slot to place another hero. Every holder in the line slows the enemies down.");

            // 3. Du vang ma chua tieu: day la luc noi ro vang CHI dung de nang cap.
            if (!seen.Contains("gold") && save.gold >= 300)
                return Make("gold", "Gold Has One Job",
                    "Gold only buys upgrades — hero levels, cannon mounts and the fence tier. Winning waves is the only source of it.");

            // 4. Du vang quay 1 lan: gioi thieu cong Summon + ve mien phi moi 5 cap.
            if (!seen.Contains("summon") && save.gold >= 500)
                return Make("summon", "The Gate",
                    "Summon new units at the Gate. Every 5 player levels hands you one free single summon.");

            // 5. Da qua vai wave: mo Phó ban Chinh Phat de co them vang/manh/do.
            if (!seen.Contains("conquest") && save.nextWave >= 6)
                return Make("conquest", "Conquest",
                    "Conquest puts your line against an NPC castle. Gold, shards and gear drop from it — your own castle is untouched.");

            return null;
        }

        static ContextTip Make(string id, string title, string body) =>
            new ContextTip { id = id, title = title, body = body };
    }
}
