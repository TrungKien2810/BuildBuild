using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Nut ma mot buoc huong dan tro toi. `Meta` khong biet UI: lop `UI/BattleHud` doi enum nay sang
    /// Transform cua nut tuong ung trong scene, roi dua cho `UI/TipPanel` nhap nhay.
    /// </summary>
    public enum TutorialTarget
    {
        None,
        Fight,
        Upgrade,
        Chest,
        Quests,
        Gate,
        Conquest,
    }

    /// <summary>Mot buoc huong dan tan thu: `id` de bao "da lam xong", chu tren the, nut can chi.</summary>
    public readonly struct TutorialStep
    {
        public readonly string id;
        public readonly string title;
        public readonly string body;
        public readonly TutorialTarget target;

        public TutorialStep(string id, string title, string body, TutorialTarget target = TutorialTarget.None)
        {
            this.id = id;
            this.title = title;
            this.body = body;
            this.target = target;
        }
    }

    /// <summary>
    /// Day huong dan tan thu cua acc moi: MOT danh sach CO THU TU, moi luc chi mot buoc dang hien
    /// (`UI/TipPanel`), tien len khi nguoi choi lam dung viec do hoac bam "Got it".
    ///
    /// Vi sao la day co thu tu chu khong phai moi goi y mot dieu kien rieng (ban cu `ContextTips`):
    /// - Nguoi moi chi lam duoc MOT viec mot luc — hai the cung luc la hai chi dan tranh nhau;
    /// - "1 / 8" chi co nghia khi co thu tu, va no cho nguoi choi biet con bao xa;
    /// - Bo qua duoc tung buoc ("Got it") va ca day ("Skip"), va choi lai duoc tu man Cai dat.
    ///
    /// Tien do nam trong save (`SaveData.tutorialStep`): dang choi do thi mo lai van dung buoc do.
    /// Save tao TRUOC khi co he nay duoc danh dau XONG ca day (`SaveSystem.Migrate`) — nguoi choi cu
    /// khong phai tan thu nua, va ho co nut "Replay Guide" neu muon xem lai.
    /// </summary>
    public static class Tutorial
    {
        /// <summary>
        /// Thu tu la thu tu DAY. `body` phai ngan: the chi rong 600x100 px cho chu co 24 (~3 dong,
        /// ~150 ky tu) — dai hon la tran ra ngoai khung, khong co loi nao bao.
        /// </summary>
        public static readonly TutorialStep[] Steps =
        {
            new TutorialStep("fight", "Your First Line",
                "Your starters are already on the field. Tap FIGHT WAVE — heroes and cannons fight on their own. "
                + "Between waves is when you spend Gold.",
                TutorialTarget.Fight),

            // Buoc 2 dung ngay sau wave dau: day la luc nguoi choi vua co vang va chua biet tieu vao dau.
            new TutorialStep("upgrade", "Gold Buys Upgrades",
                "Gold only buys upgrades. This button raises the fence: more cannon mounts, more shots, "
                + "a tougher wall.",
                TutorialTarget.Upgrade),

            // Hero phai DAT TAY xuong o — khong tu ra san nhu linh cua Bonecaller.
            new TutorialStep("slots", "Fill The Slots",
                "Only heroes standing on the field fight. Tap an empty hero slot to place another one — "
                + "the wall holds longer with more of them."),

            // Bai linh chi la cho dung, phai keo tay ra san; khong noi thi ca he trieu hoi coi nhu khong ton tai.
            new TutorialStep("soldiers", "Soldiers Need A Hand",
                "Soldiers never march on their own. When a squad post behind the wall is ready, "
                + "drag a squad from it onto the field."),

            new TutorialStep("chest", "The Chest Fills Itself",
                "The chest stores Gold while you are away, up to 12 hours. Tap it to claim — "
                + "even while it is still filling.",
                TutorialTarget.Chest),

            new TutorialStep("quests", "Journey",
                "Journey objectives pay Gold, Summon rolls and units. The dot on the button means "
                + "something is waiting to be claimed.",
                TutorialTarget.Quests),

            new TutorialStep("gate", "The Gate",
                "New units come from the Gate. Every 5 player levels hands you one free single summon.",
                TutorialTarget.Gate),

            new TutorialStep("conquest", "Conquest",
                "Conquest sends your line against an NPC castle for Shards and gear. "
                + "Your own castle is not at risk.",
                TutorialTarget.Conquest),
        };

        /// <summary>Co bao nhieu buoc — de hien "1 / 8" ma khong phai dem tay o lop UI.</summary>
        public static int Count => Steps.Length;

        /// <summary>Buoc dang hien (0..Count). Bang `Count` = da xong / da bo qua.</summary>
        public static int Index => Mathf.Clamp(SaveSystem.Data.tutorialStep, 0, Count);

        public static bool Finished => Index >= Count;

        /// <summary>Buoc dang hien; null = khong con gi de noi (xong, da Skip, hoac save cu).</summary>
        public static TutorialStep? Current => Finished ? (TutorialStep?)null : Steps[Index];

        /// <summary>
        /// Nguoi choi vua LAM XONG buoc `id`. Chi buoc DANG HIEN moi tien duoc: lam mot viec cua buoc
        /// sau (vd. mo ruong khi dang o buoc 2) khong nhay coc, va buoc do se van hien ra sau — noi
        /// "Got it" la qua. Doi lai: khong bao gio co hai the cung luc, va tien do la mot con so.
        /// </summary>
        public static void Complete(string id)
        {
            if (Finished || Steps[Index].id != id) return;
            SetIndex(Index + 1);
        }

        /// <summary>Nut "Got it": bo qua buoc dang hien du chua lam (nguoi choi tu biet roi).</summary>
        public static void Dismiss() => SetIndex(Index + 1);

        /// <summary>Bo ca day huong dan (nut "Skip guide") — tu day khong the nao hien lai.</summary>
        public static void Skip() => SetIndex(Count);

        /// <summary>
        /// Choi lai tu buoc 1 (man Cai dat, nut "Replay Guide"). Khong tu ve scene: `BattleHud` doc co
        /// `RestartPending` roi moi hien the, vi the khong duoc hien khi dang danh.
        /// </summary>
        public static void Restart()
        {
            SetIndex(0);
            _restart = true;
        }

        /// <summary>Vua bam "Replay Guide" va the van chua duoc hien lai (co the dang danh / dang mo Cai dat).</summary>
        public static bool RestartPending => _restart;

        public static void ConsumeRestart() => _restart = false;

        static bool _restart;

        // Tat Domain Reload khi vao Play Mode thi bien static song qua — tra lai trang thai sach.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _restart = false;

        static void SetIndex(int value)
        {
            var save = SaveSystem.Data;
            value = Mathf.Clamp(value, 0, Count);
            if (save.tutorialStep == value) return;
            save.tutorialStep = value;
            SaveSystem.Save();
        }
    }
}
