using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>Mot tai khoan trong `accounts.json`. Khong bao gio chua mat khau tho — chi muoi + hash.</summary>
    [Serializable]
    public class AccountRecord
    {
        public string username;
        /// <summary>Muoi ngau nhien 16 byte, base64.</summary>
        public string salt;
        /// <summary>PBKDF2-HMAC-SHA256(mat khau, muoi, 100.000 vong), 32 byte, base64.</summary>
        public string hash;
        /// <summary>Giay Unix luc tao — chi de doc file khi can debug.</summary>
        public long createdAt;
    }

    [Serializable]
    public class AccountBook
    {
        public int version = 1;
        /// <summary>Ten acc dang nhap gan nhat — man dang nhap dien san vao o ten (khong tu dang nhap).</summary>
        public string lastUser = "";
        public List<AccountRecord> accounts = new List<AccountRecord>();
    }

    /// <summary>
    /// Tai khoan cuc bo (chot 2026-10-27: nguoi dung chon "local", khong co server). MOT file `accounts.json`
    /// nam canh cac file save trong `Application.persistentDataPath`; moi acc mot dong gom muoi + hash
    /// PBKDF2-HMAC-SHA256, mat khau tho khong duoc ghi ra dia.
    ///
    /// Ten acc chinh la TEN FILE SAVE (`save-&lt;ten&gt;.json`, xem `SaveSystem.FilePath`) nen bi gioi han
    /// chu/so/gach duoi 3..16 ky tu va ha ve chu thuong: khong the co `/`, `..` hay dau cach trong duong dan.
    ///
    /// Phien dang nhap = mot khoa PlayerPrefs (`SessionKey`) giu ten acc, nen mo lai game la vao thang acc cu
    /// (nguoi dung chon "tu dang nhap lai acc gan nhat" 2026-10-27); `Logout` xoa khoa do.
    /// </summary>
    public static class Accounts
    {
        public const string SessionKey = "BuildBuild.Session";
        const string BookFile = "accounts.json";

        const int MinUsername = 3;
        const int MaxUsername = 16;
        const int MinPassword = 4;
        const int MaxPassword = 32;
        // Du cho mot game offline: ~150 ms moi lan dang nhap, du lau de do hash hang tram nghin lan.
        const int Iterations = 100000;
        const int SaltBytes = 16;
        const int HashBytes = 32;

        static AccountBook _book;

        /// <summary>File goc cua toan bo tai khoan — cung thu muc voi cac file save.</summary>
        public static string BookPath => Path.Combine(Application.persistentDataPath, BookFile);

        static AccountBook Book => _book ??= LoadBook();

        /// <summary>Acc dang dang nhap; "" = chua dang nhap (chi xay ra truoc lan dang ky dau tien hoac sau Logout).</summary>
        public static string Current => PlayerPrefs.GetString(SessionKey, "");
        public static bool IsLoggedIn => Current.Length > 0;

        /// <summary>Ten acc cua lan dang nhap gan nhat de man dang nhap dien san — "" = may chua co acc nao.</summary>
        public static string LastUser => Book.lastUser;

        public static bool HasAccount(string username)
        {
            username = Normalize(username);
            return Find(username) != null;
        }

        /// <summary>
        /// Tao acc moi va dang nhap luon. `confirm` do man hinh gui xuong de kiem tra hai o mat khau khop nhau —
        /// lop du lieu van phai tu kiem, khong tin UI.
        /// </summary>
        public static bool Register(string username, string password, string confirm, out string error)
        {
            username = Normalize(username);
            if (!CheckUsername(username, out error)) return false;
            if (password != confirm)
            {
                error = "Passwords do not match";
                return false;
            }
            if (!CheckPassword(password, out error)) return false;
            if (Find(username) != null)
            {
                error = "That name is already taken";
                return false;
            }

            bool firstOnDevice = !Book.accounts.Exists(a => a != null && a.username != SaveSystem.AdminAccount);
            byte[] salt = NewSalt();
            Book.accounts.Add(new AccountRecord
            {
                username = username,
                salt = Convert.ToBase64String(salt),
                hash = Convert.ToBase64String(Derive(password, salt)),
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
            WriteBook();

            // May da choi truoc khi co dang nhap (`save.json` cua acc "main" cu): chep sang acc DAU TIEN cua NGUOI
            // CHOI de khong mat tien do. Acc seed `admin` khong tinh — no duoc tao truoc khi ai dang ky.
            if (firstOnDevice) SaveSystem.AdoptSave(SaveSystem.FileFor(SaveSystem.MainAccount), SaveSystem.FileFor(username));

            SetSession(username);
            error = "";
            return true;
        }

        public static bool Login(string username, string password, out string error)
        {
            username = Normalize(username);
            var record = Find(username);
            if (record == null)
            {
                error = "No account with that name";
                return false;
            }
            if (!SameHash(Derive(password, Convert.FromBase64String(record.salt)), Convert.FromBase64String(record.hash)))
            {
                error = "Wrong password";
                return false;
            }
            SetSession(username);
            error = "";
            return true;
        }

        /// <summary>
        /// Tao acc seed `admin` neu may chua co (goi luc mo man dang nhap, chi trong Editor / Development Build —
        /// xem `LoginScreen.Start`). KHONG doi phien dang nhap va KHONG ghi `lastUser`: acc nay la acc dev, nguoi
        /// choi that khong duoc tu dong vao.
        ///
        /// Tra ve true khi vua tao moi (goi y de log), false khi da co tu truoc.
        /// </summary>
        public static bool EnsureSeedAccount(string username, string password)
        {
            username = Normalize(username);
            if (Find(username) != null) return false;
            if (!CheckPassword(password, out _)) return false;

            byte[] salt = NewSalt();
            Book.accounts.Add(new AccountRecord
            {
                username = username,
                salt = Convert.ToBase64String(salt),
                hash = Convert.ToBase64String(Derive(password, salt)),
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
            WriteBook();
            return true;
        }

        /// <summary>Ra man dang nhap ma khong xoa gi: save cua acc van nam trong file cua no.</summary>
        public static void Logout()
        {
            PlayerPrefs.DeleteKey(SessionKey);
            PlayerPrefs.Save();
            SaveSystem.Invalidate();
        }

        /// <summary>
        /// Man dang nhap hoi luc mo scene: co phien va acc do CON trong so thi vao thang game. Phien tro vao
        /// acc da bi xoa (`accounts.json` bi xoa / doi may) thi don phien cu va o lai form.
        ///
        /// Acc dev (`main` / `test` / `fresh` / `admin`) khong nam trong so nhung van duoc giu phien: chung chi
        /// duoc dat tu `AdminPanel` (F1), va doi lai thi lan mo game sau AdminPanel khong con tro dung acc do nua.
        /// </summary>
        public static bool TryResumeSession()
        {
            if (!IsLoggedIn) return false;
            // Acc dev chi co o Editor / Development Build: ban release ma tin phien `admin` trong PlayerPrefs la vao
            // thang acc 250k vang khong can mat khau.
            if (HasAccount(Current) || (Debug.isDebugBuild && SaveSystem.IsDevAccount(Current))) return true;
            Logout();
            return false;
        }

        /// <summary>
        /// Dat acc dang dung. Duong that la <see cref="Login"/> / <see cref="Register"/>; `AdminPanel` (F1) goi
        /// thang vao day de doi acc dev ("main" / "test" / "fresh") ma khong can mat khau.
        /// </summary>
        public static void SetSession(string username)
        {
            username = Normalize(username);
            PlayerPrefs.SetString(SessionKey, username);
            PlayerPrefs.Save();
            // Chi nho ten de dien san khi ten do la acc that; acc dev khong ton tai trong so nen khong ghi.
            if (Find(username) != null)
            {
                Book.lastUser = username;
                WriteBook();
            }
            SaveSystem.Invalidate();
        }

        static AccountRecord Find(string username)
        {
            foreach (var record in Book.accounts)
                if (record != null && record.username == username) return record;
            return null;
        }

        static string Normalize(string username) => (username ?? "").Trim().ToLowerInvariant();

        static bool CheckUsername(string username, out string error)
        {
            // Ten acc dev ("main"/"test"/"fresh"/"admin") da co hanh vi rieng trong `SaveSystem` (acc test/admin so
            // huu moi unit, acc fresh xoa moi lan chon) — khong cho nguoi choi dang ky trung.
            if (SaveSystem.IsDevAccount(username))
            {
                error = "That name is reserved";
                return false;
            }
            if (username.Length < MinUsername || username.Length > MaxUsername)
            {
                error = $"Name must be {MinUsername}-{MaxUsername} characters";
                return false;
            }
            foreach (char c in username)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                {
                    error = "Only letters, digits and _ allowed";
                    return false;
                }
            }
            error = "";
            return true;
        }

        static bool CheckPassword(string password, out string error)
        {
            if (password == null || password.Length < MinPassword || password.Length > MaxPassword)
            {
                error = $"Password must be {MinPassword}-{MaxPassword} characters";
                return false;
            }
            error = "";
            return true;
        }

        static byte[] NewSalt()
        {
            var salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return salt;
        }

        static byte[] Derive(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return pbkdf2.GetBytes(HashBytes);
        }

        static bool SameHash(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];   // so sanh khong thoat som theo byte lech
            return diff == 0;
        }

        static AccountBook LoadBook()
        {
            if (!File.Exists(BookPath)) return new AccountBook();
            try
            {
                return JsonUtility.FromJson<AccountBook>(File.ReadAllText(BookPath)) ?? new AccountBook();
            }
            catch (Exception e)
            {
                string backup = BookPath + ".corrupt";
                File.Copy(BookPath, backup, true);
                Debug.LogError($"[Accounts] Doc accounts.json that bai ({e.Message}). Ban hong giu o {backup}, bat dau so moi.");
                return new AccountBook();
            }
        }

        // Ghi ra file tam roi moi thay: mat dien giua luc ghi thi danh sach acc cu van con nguyen ven.
        static void WriteBook()
        {
            string tmp = BookPath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Book, true));
            // `File.Replace` thay NGUYEN TU (khong co khoang thoi gian khong ton tai file nao): xoa roi move thi
            // mat dien dung giay giua hai buoc la mat ca so acc lan save cua moi acc.
            if (File.Exists(BookPath)) File.Replace(tmp, BookPath, null);
            else File.Move(tmp, BookPath);
        }

        // Tat Domain Reload khi vao Play Mode thi bien static song qua cac lan Play — doc lai tu file.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _book = null;
    }
}
