using System.Collections.Generic;
using UnityEngine;
public class SoundManager : MonoBehaviour
{
    public const string PLAYER_DEATH = "PlayerDeath";
    public const string CLICK = "Click";
    public const string EXPLOSION = "Explosion";


    [SerializeField] private List<AudioClip> _sounds = new();

}