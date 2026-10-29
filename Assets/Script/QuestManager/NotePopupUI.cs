using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeroQuest.Quests.UI
{
    /// <summary>Popup de notes avec file d'attente : plusieurs notes déclenchées = affichées l'une après l'autre.</summary>
    public class NotePopupUI : MonoBehaviour
    {
        public static NotePopupUI Instance { get; private set; }

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button closeButton;

        readonly Queue<(string title, string text)> pending = new();

        /// <summary>true tant qu'une note est affichée : bloque les inputs du jeu dessus.</summary>
        public static bool IsOpen { get; private set; }
        public static event Action<bool> OnOpenChanged;

        void Awake()
        {
            Instance = this;
            panel.SetActive(false);
            closeButton.onClick.AddListener(Next);
        }

        public void Show(string title, string text)
        {
            pending.Enqueue((title, text));
            if (!IsOpen) Next();
        }

        void Next()
        {
            if (pending.Count == 0) { SetOpen(false); return; }
            var (t, b) = pending.Dequeue();
            titleText.text = t;
            bodyText.text = b;
            SetOpen(true);
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            panel.SetActive(open);
            OnOpenChanged?.Invoke(open);
        }
    }
}
