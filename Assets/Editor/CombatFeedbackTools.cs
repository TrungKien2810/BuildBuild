using BuildBuild.Combat;
using UnityEditor;
using UnityEngine;

namespace BuildBuild.Editor
{
    public static class CombatFeedbackTools
    {
        const string MenuPath = "BuildBuild/Combat/Enhanced feedback (A-B)";

        [MenuItem(MenuPath)]
        static void Toggle() => CombatFeedback.Enhanced = !CombatFeedback.Enhanced;

        [MenuItem("BuildBuild/Combat/Preview heavy camera impact")]
        static void Preview() => CombatCameraImpulse.Kick(Vector2.right, 0.065f);

        [MenuItem("BuildBuild/Combat/Preview heavy camera impact", true)]
        static bool CanPreview() => EditorApplication.isPlaying && CombatFeedback.Enhanced && Time.timeScale > 0f;

        [MenuItem(MenuPath, true)]
        static bool Validate()
        {
            Menu.SetChecked(MenuPath, CombatFeedback.Enhanced);
            return EditorApplication.isPlaying;
        }
    }
}
