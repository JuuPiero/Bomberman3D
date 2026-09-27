using System;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>
    /// A piece of UI (screen, dialog, overlay) that wraps an element from a UXML file.
    /// Missing elements throw right away with the element name, so UXML/code mismatches are obvious.
    /// </summary>
    public abstract class UIView
    {
        const string ViewClass = "view";
        const string EnterClass = "view--enter";

        public VisualElement Root { get; }
        public bool IsVisible { get; private set; }

        protected UIView(VisualElement root)
        {
            Root = root ?? throw new InvalidOperationException($"{GetType().Name}: root element not found in UXML.");
            Root.AddToClassList(ViewClass);
            Root.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            if (IsVisible) return;
            IsVisible = true;

            // Draw order comes from the UXML (overlays and toasts are declared last).
            Root.style.display = DisplayStyle.Flex;
            // Start from the "enter" style and let the USS transition animate to the normal style.
            Root.AddToClassList(EnterClass);
            Root.schedule.Execute(() => Root.RemoveFromClassList(EnterClass)).StartingIn(30);
            OnShow();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Root.style.display = DisplayStyle.None;
            OnHide();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected T Q<T>(string name) where T : VisualElement
        {
            T element = Root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"{GetType().Name}: element '{name}' ({typeof(T).Name}) not found in UXML.");
            return element;
        }

        protected Button BindButton(string name, Action onClick)
        {
            Button button = Q<Button>(name);
            button.focusable = false; // Space drops bombs; it must never "click" a focused button
            button.clicked += onClick;
            return button;
        }

        protected static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected static void SetPlaceholder(TextField field, string placeholder)
        {
            field.textEdition.placeholder = placeholder;
            field.textEdition.hidePlaceholderOnFocus = true;
        }
    }
}
