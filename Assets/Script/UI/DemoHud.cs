using System.Collections.Generic;
using UnityEngine;

public class DemoHud : MonoBehaviour
{
    private readonly List<string> diceLog = new List<string>();
    private bool subscribed;

    private void Update()
    {
        if (subscribed || ActionManager.Instance == null) return;
        ActionManager.Instance.Rolled += OnRolled;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && ActionManager.Instance != null) ActionManager.Instance.Rolled -= OnRolled;
    }

    private void OnRolled(string text)
    {
        diceLog.Add(text);
        if (diceLog.Count > 8) diceLog.RemoveAt(0);
    }

    private void OnGUI()
    {
        TurnManager turn = TurnManager.Instance;
        if (turn == null || turn.Phase == TurnPhase.NotStarted) return;

        float width = 400f;
        float height = 140f;

        Rect area = new Rect(Screen.width - width - 20f, (Screen.height - height) / 2f - 50f, width, height);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(270f));

        if (turn.Phase == TurnPhase.HeroTurn && turn.CurrentHero != null)
        {
            Hero hero = turn.CurrentHero;
            GUILayout.Label("Round " + turn.Round + " - " + hero.heroName);
            GUILayout.Label("Body " + hero.currentBodyPoints + "/" + hero.MaxBodyPoints + "   Gold " + hero.gold);
            GUILayout.Label((turn.HasMoved ? "Move: done" : "Move: available") + " | " +
                            (turn.HasActed ? "Action: done" : "Action: available"));

            GUILayout.BeginHorizontal();

            GUI.enabled = turn.CanMove;
            if (GUILayout.Button("Move", GUILayout.Height(30))) turn.RequestMove();

            GUI.enabled = turn.CanAct;
            if (GUILayout.Button("Attack", GUILayout.Height(30))) turn.RequestAction(HeroActionType.Attack);
            if (GUILayout.Button("Treasure", GUILayout.Height(30))) turn.RequestAction(HeroActionType.SearchTreasure);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Zargon's turn...");
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Dice:");

        int start = Mathf.Max(0, diceLog.Count - 5);
        for (int i = start; i < diceLog.Count; i++)
        {
            GUILayout.Label(diceLog[i]);
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}