using System.Text;
using UnityEngine;

namespace HeroQuest.Quests
{
    public static class QuestUtil
    {
        /// <summary>Same normalisation as MonsterManager.FindData: "Chaos Warrior" == "chaoswarrior" == "Chaos Warriors".</summary>
        public static string Normalize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder();
            foreach (char c in name) if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
            string s = sb.ToString();
            if (s.EndsWith("ies")) s = s.Substring(0, s.Length - 3) + "y";
            else if (s.EndsWith("s")) s = s.Substring(0, s.Length - 1);
            return s;
        }

        public static bool SameName(string a, string b)
        {
            string na = Normalize(a);
            return na.Length > 0 && na == Normalize(b);
        }

        /// <summary>Shows a note popup (null-safe: logs if the popup is not in the scene).</summary>
        public static void Note(string title, string text)
        {
            if (UI.NotePopupUI.Instance != null) UI.NotePopupUI.Instance.Show(title, text);
            else Debug.Log("[Quest note] " + title + " : " + text);
        }
    }
}
