using UnityEngine;

[CreateAssetMenu(fileName = "RuleTileScriptableObject", menuName = "ScriptableObjects/MGL Rule Tile")]
public class RuleTileScriptableObject : ScriptableObject
{
    public Material material;
    public Rule[] rules;
}

[System.Serializable]
public class Rule
{
    //public int Above;
    //public int Below;

    public int TR;
    public int T;
    public int TL;

    public int R;
    public int L;

    public int BR;
    public int B;
    public int BL;

    public float rotation;
    public Vector3 position_offset;
    public GameObject tile;
}
