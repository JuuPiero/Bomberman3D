using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThanhHoang.Bomberman
{
    public class TabView : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            public BaseScreen screen;
            public Sprite icon;
            public Color color;
            public Action OnSelected;
            public Action OnDeselected;
        }
        public List<Tab> tabs = new List<Tab>();
        public Color selectedColor = Color.white;

    }
}