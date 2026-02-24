using UnityEngine;

namespace ThanhHoang.Bomberman
{

    public static class GameObjectExtension
    {
        public static void Toggle(this GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}