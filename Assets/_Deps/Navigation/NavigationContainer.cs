using System.Collections.Generic;
using UnityEngine;
namespace ThanhHoang.Bomberman
{
    public class NavigationContainer : MonoBehaviour 
    {
        // public List<Navigator> navigators = new List<Navigator>();
        private StackNavigator _stack;
        public StackNavigator Stack
        {
            get
            {
                if (_stack == null)
                {
                    _stack= GetComponentInChildren<StackNavigator>();
                    if (_stack != null) return _stack;
                    GameObject stackObj = new GameObject("StackNavigator");
                    stackObj.transform.SetParent(transform);
                    _stack = stackObj.AddComponent<StackNavigator>();
                }
                return _stack;
            }
        }
        public TabNavigator Tab { get; private set; }

        void Awake()
        {
            Service.Register(this);
        }
    }
}