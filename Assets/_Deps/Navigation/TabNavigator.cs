namespace ThanhHoang.Bomberman
{
    public class TabNavigator : Navigator
    {
        private IScreen currentTab;
        private IScreen previousTab;
        public override void GoBack()
        {
            if (previousTab != null)
            {
                currentTab.Exit();
                currentTab = previousTab;
                currentTab.Enter(null);
                CurrentScreen = currentTab;
            }
        }
        public override void Navigate(string screenName, object param = null)
        {
            if (screens.TryGetValue(screenName, out IScreen screen))
            {
                if (currentTab != null)
                {
                    previousTab = currentTab;
                    currentTab.Exit();
                }
                currentTab = screen;
                currentTab.Enter(param);
                CurrentScreen = currentTab;
            }
        }
        public override void Navigate<T>(object param = null)
        {
            IScreen screen = screensByType[typeof(T)];
            if (currentTab != null)
            {
                previousTab = currentTab;
                currentTab.Exit();
            }
            currentTab = screen;
            currentTab.Enter(param);
            CurrentScreen = currentTab;
        }
    }
}