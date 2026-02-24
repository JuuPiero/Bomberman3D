using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman
{

    public static class UIElementExtensions
    {
        /// <summary>
        /// Lấy phần tử con từ VisualElement theo tên.
        /// </summary>
        public static T GetElementByName<T>(this VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name);
        }

        /// <summary>
        /// Get fisrt child from VisualElement by class.
        /// </summary>
        public static T GetElementByClassName<T>(this VisualElement root, params string[] classNames) where T : VisualElement
        {
            var query = root.Query<T>();
            foreach (var className in classNames)
            {
                query = query.Class(className);
            }
            return query.First();
        }


        /// <summary>
        /// Get fisrt child from VisualElement by class (.classname) or name (#name).
        /// </summary>
        public static T QuerySelector<T>(this VisualElement root, string selector) where T : VisualElement
        {
            if (selector.StartsWith("#"))
            {
                return root.Q<T>(selector.Substring(1));
            }
            else if (selector.StartsWith("."))
            {
                var classNames = selector.Substring(1).Split('.');
                return root.GetElementByClassName<T>(classNames);
            }
            return null;
        }

        public static List<T> QuerySelectorAll<T>(this VisualElement root, string selector) where T : VisualElement
        {
            var classNames = selector.Substring(1).Split('.');
            var query = root.Query<T>();
            foreach (var className in classNames)
            {
                query = query.Class(className);
            }
            return query.ToList();
        }


        public static void RemoveClasslistAll<T>(this List<VisualElement> list, string className) where T : VisualElement
        {
            list.ForEach(item =>
            {
                item.RemoveFromClassList(className);
            });
        }

        public static void ToggleClasslistAll<T>(this List<VisualElement> list, string className) where T : VisualElement
        {
            list.ForEach(item =>
            {
                item.ToggleInClassList(className);
            });
        }

    }

}