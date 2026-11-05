using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man dang nhap / dang ky (chot 2026-10-27: tai khoan luu cuc bo, xem `Meta/Accounts`). Scene `Login` la
    /// scene dau tien trong Build Settings; vao thang Battle khi may con phien dang nhap tu lan choi truoc
    /// (nguoi dung chon "tu dang nhap lai acc gan nhat"), nen man nay chi hien o lan dau va sau khi Log out.
    ///
    /// MOT form, HAI che do: dang nhap (ten + mat khau) va tao acc (them o nhap lai mat khau). Man hinh chi
    /// gui chuoi xuong `Accounts`; moi kiem tra (trung ten, sai mat khau, do dai...) do lop du lieu tra ve
    /// chu loi hien o `messageText`.
    /// </summary>
    public class LoginScreen : MonoBehaviour
    {
        [SerializeField] Text titleText;
        [SerializeField] InputField userField;
        [SerializeField] InputField passwordField;
        [SerializeField] InputField confirmField;
        [SerializeField] GameObject confirmRow;
        [SerializeField] Text messageText;
        [SerializeField] Button submitButton;
        [SerializeField] Text submitLabel;
        [SerializeField] Button modeButton;
        [SerializeField] Text modeLabel;

        bool _registerMode;

        void Start()
        {
            // Acc seed cho dev/QA: tao TRUOC khi hoi phien, de lan dau mo game da dang nhap duoc ngay bang
            // `admin` (xem `SaveSystem.AdminAccount`). Build that khong co acc nay.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Accounts.EnsureSeedAccount(SaveSystem.AdminAccount, SaveSystem.AdminPassword))
                Debug.Log($"[Login] Da tao acc seed '{SaveSystem.AdminAccount}' / mat khau '{SaveSystem.AdminPassword}'.");
#endif
            submitButton.onClick.AddListener(Submit);
            modeButton.onClick.AddListener(() => SetMode(!_registerMode));
            // Enter khong can chuot: o Ten nhay xuong o Mat khau, o Mat khau (va Nhap lai) gui luon.
            userField.onSubmit.AddListener(_ => passwordField.Select());
            passwordField.onSubmit.AddListener(_ => Submit());
            confirmField.onSubmit.AddListener(_ => Submit());
            // Mo lai game sau khi da dang nhap: vao thang, khong bat nhap lai mat khau.
            if (Accounts.TryResumeSession())
            {
                EnterGame();
                return;
            }
            userField.text = Accounts.LastUser;
            SetMode(Accounts.LastUser.Length == 0);
        }

        void SetMode(bool register)
        {
            _registerMode = register;
            confirmRow.SetActive(register);
            confirmField.text = "";
            passwordField.text = "";
            messageText.text = "";
            titleText.text = register ? "Create account" : "Sign in";
            submitLabel.text = register ? "Create account" : "Sign in";
            modeLabel.text = register ? "I already have one" : "Create account";
            userField.Select();
        }

        void Submit()
        {
            string error;
            bool ok = _registerMode
                ? Accounts.Register(userField.text, passwordField.text, confirmField.text, out error)
                : Accounts.Login(userField.text, passwordField.text, out error);
            if (!ok)
            {
                messageText.text = error;
                Sfx.Play("ui-error");
                // Tra con tro ve o vua bi xoa: dang nhap lai bang ban phim, khong phai bam chuot vao o mot lan nua.
                passwordField.text = "";
                passwordField.Select();
                return;
            }
            EnterGame();
        }

        void EnterGame()
        {
            Sfx.Play("ui-open");
            GameSpeed.Resume();   // phong khi scene truoc de lai timeScale 0 (mo tu man Cai dat)
            SceneManager.LoadScene(SceneNames.Battle);
        }
    }
}
