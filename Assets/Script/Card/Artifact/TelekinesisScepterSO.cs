using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Artifact/Telekinesis Scepter")]
public class TelekinesisScepterSO : ArtifactCardSO
{
    public bool Activate(Monster target, IArtifactGame game, IRandomPicker randomPicker)
    {
        for(int i = 0; i < target.mindPoints; i++)
        {
            if (randomPicker.Pick(6) + 1 == 6) return false;

        }

        game.SkipNextTurn(target);
        return true;
    }
}