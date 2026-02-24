
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ThanhHoang.Bomberman
{
    public abstract class Navigator : MonoBehaviour
    {
        // public List<IScreen> allScreens = new List<IScreen>();
        public IScreen CurrentScreen { get; protected set; }

        [ShowInInspector, ReadOnly] public Dictionary<string, IScreen> screens = new Dictionary<string, IScreen>();
        public Dictionary<Type, IScreen> screensByType = new Dictionary<Type, IScreen>();
        void Awake()
        {
            var screenComponents = GetComponentsInChildren<IScreen>(true);
            foreach (var screen in screenComponents) 
            {
                if (screen is BaseScreen baseScreen)
                {
                    screens[baseScreen.screenName] = screen;
                    screensByType[screen.GetType()] = screen;
                }
            }
        }

        public abstract void Navigate(string screenName, object param = null);
        public abstract void Navigate<T>(object param = null) where T : IScreen;
        public abstract void GoBack();
    }
}