using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Data/Game Data")]
public class GameDataSO : ScriptableObject
{
    public int bombCount = 1;
    public int explosionRange = 1;


    public int lifeCountLeft = 5;
    // public int highScore;
    public int currentStage;


    public void ResetData()
    {
        bombCount = 1;
        lifeCountLeft = 5;
        currentStage = 0;
        explosionRange = 0;
    }
}