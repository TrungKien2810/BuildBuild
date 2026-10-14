using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Vat dung tren san ma vung chieu can do toi (than rong = nua be ngang tren mat dat).
    ///
    /// Vi sao co interface nay: `Battlefield` nam o lop Core, con `Unit` nam o lop Combat (Combat tham chieu
    /// Core, khong duoc nguoc lai). Hai phep do vung chieu "dat tren lan" nam o `Battlefield` nen can mot cai
    /// ten cho "thu dung tren san" ma khong phai keo `Unit` xuong Core.
    /// </summary>
    public interface IGroundBody
    {
        /// <summary>Chan unit tren san (toa do world).</summary>
        Vector3 GroundPosition { get; }

        /// <summary>Nua be ngang than tren mat dat (world).</summary>
        float BodyRadius { get; }
    }
}
