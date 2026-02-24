using System.Collections.Generic;

namespace ThanhHoang.Bomberman
{
    public class StackNavigator : Navigator
    {
        public Stack<IScreen> screenStack = new Stack<IScreen>();

        void Start()
        {
            
        }
        public override void GoBack()
        {
            if (screenStack.Count > 1)
            {
                IScreen lastScreen = screenStack.Pop();
                lastScreen.Exit();
                IScreen currentScreen = screenStack.Peek();
                currentScreen.Enter();
                CurrentScreen = currentScreen;
            }
        }

        public override void Navigate(string screenName, object param = null)
        {
            if (screens.TryGetValue(screenName, out IScreen screen))
            {
                if (screenStack.Count > 0)
                {
                    IScreen lastScreen = screenStack.Peek();
                    lastScreen.Exit();
                }
                screenStack.Push(screen);
                screen.Enter(param);
                CurrentScreen = screen;
            }
        }

        public override void Navigate<T>(object param = null)
        {
            IScreen screen = screensByType[typeof(T)];
            if (screenStack.Count > 0)
            {
                IScreen lastScreen = screenStack.Peek();
                lastScreen.Exit();
            }
            screenStack.Push(screen);
            screen.Enter(param);
            CurrentScreen = screen;
        }
    }
}