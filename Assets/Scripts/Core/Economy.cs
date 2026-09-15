using System;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Vi vang. Tach rieng vi day la thu DUY NHAT song sot qua mot lan thua: thua thi choi lai tu
    /// wave 1 nhung giu vang + vat pham + level + pity (GAME_DESIGN.md muc 1).
    ///
    /// So vang nam trong save JSON (`SaveSystem`), khong nam trong component — nen man tran va man
    /// gacha moi scene mot Economy van thay cung mot so du.
    /// </summary>
    public class Economy : MonoBehaviour
    {
        public int Gold => SaveSystem.Data.gold;
        public event Action<int> Changed;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            SaveSystem.Data.gold += amount;
            SaveSystem.Save();
            Changed?.Invoke(Gold);
        }

        /// <summary>Tra ve false va khong tru neu khong du — nguoi goi tu quyet dinh bao loi the nao.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            SaveSystem.Data.gold -= amount;
            SaveSystem.Save();
            Changed?.Invoke(Gold);
            return true;
        }

        // Test gacha ma khong phai danh 6 wave de gom 500 vang: chuot phai component -> menu nay.
        [ContextMenu("Dev: +5000 vang")]
        void DevAddGold() => Add(5000);
    }
}
