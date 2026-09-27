using Photon.Pun;
using UnityEngine;
public class Portal : MonoBehaviour
{
    public string level;


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player == null || !player.IsLocal || player.isDead) return;

        if (string.IsNullOrEmpty(level))
        {
            NetworkController.Instance.LeaveToMenu();
        }
        else
        {
            PhotonNetwork.LoadLevel(level);
        }
    }
}
