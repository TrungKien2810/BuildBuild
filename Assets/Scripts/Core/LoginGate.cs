using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuildBuild.Core
{
    /// <summary>
    /// Chan scene tran khi chua dang nhap: bam Play thang vao Battle tu Editor (bo qua scene `Login`) thi bi
    /// day ve man dang nhap, de khong ai choi ma tien do roi vao file `save.json` ma khong biet.
    /// Da co phien (`Accounts.Current`) thi khong lam gi — 99% so lan chay.
    /// </summary>
    public class LoginGate : MonoBehaviour
    {
        void Awake()
        {
            if (Accounts.IsLoggedIn) return;
            SceneManager.LoadScene(SceneNames.Login);
        }
    }
}
