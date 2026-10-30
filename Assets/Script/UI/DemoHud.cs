using System.Collections.Generic;
using UnityEngine;

// Temporary demo panel (IMGUI): action buttons + dice log. It only calls the TurnManager API.
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

        GUILayout.BeginArea(new Rect(10, 80, 260, 420), GUI.skin.box);

        if (turn.Phase == TurnPhase.HeroTurn && turn.CurrentHero != null)
        {
            Hero hero = turn.CurrentHero;
            GUILayout.Label("Round " + turn.Round + " - " + hero.heroName);
            GUILayout.Label("Body " + hero.currentBodyPoints + "/" + hero.MaxBodyPoints + "   Gold " + hero.gold);
            GUILayout.Label((turn.HasMoved ? "Move: done" : "Move: available") + "   |   " +
                            (turn.HasActed ? "Action: done" : "Action: available"));

            GUI.enabled = turn.CanMove;
            if (GUILayout.Button("Move (roll red dice)", GUILayout.Height(32))) turn.RequestMove();

            GUI.enabled = turn.CanAct;
            if (GUILayout.Button("Attack", GUILayout.Height(32))) turn.RequestAction(HeroActionType.Attack);
            if (GUILayout.Button("Search for treasure", GUILayout.Height(32))) turn.RequestAction(HeroActionType.SearchTreasure);
            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label("Zargon's turn...");
        }

        GUILayout.Space(8);
        GUILayout.Label("Dice:");
        foreach (string line in diceLog) GUILayout.Label(line);

        GUILayout.EndArea();
    }
}