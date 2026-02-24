using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman
{
    public class BaseUIScreen : BaseScreen
    {
        protected UIDocument _uiDocument;
        public UIDocument UIDocument
        {
            get
            {
                if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
                return _uiDocument;
            }
        }
        protected VisualElement Root => UIDocument.rootVisualElement;
        public override void Enter(object param = null)
        {
            UIDocument.enabled = true;
        }

        public override void Exit()
        {
            UIDocument.enabled = false;
        }
    }
}