using UnityEngine;

namespace ThanhHoang.Bomberman
{
    public enum TransactionType
    {
        None,
        FadeIn,
        FadeOut,

    }



    public class BaseScreen : MonoBehaviour, IScreen
    {
        public string screenName;

        protected Navigator navigator;

        protected virtual void Awake()
        {
            navigator = GetComponentInParent<Navigator>();
        }

        #if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrEmpty(screenName))
                screenName = gameObject.name;
        }
        #endif

        public virtual void Enter(object param = null)
        {
            gameObject.SetActive(true);
        }

        public virtual void Exit()
        {
            gameObject.SetActive(false);
        }

        public void Navigate(string screenName, object param = null)
        {
            navigator.Navigate(screenName, param);
        }

        public void Navigate<T>(object param = null) where T : IScreen
        {
            navigator.Navigate<T>(param);
        }
    }
}