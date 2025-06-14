using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(RuleTileScriptableObject))]
public class RuleTileSOEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        VisualElement myInspector = new VisualElement();

        // Load and clone a visual tree from UXML
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/3D Rule Tiles/Scripts/Editor/RuleTileSO_UXML.uxml");
        visualTree.CloneTree(myInspector);

        // Return the finished inspector UI
        return myInspector;
    }
}
