using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button exitButton;
    public Button continueButton;



    void Start()
    {
        startButton?.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Level1");
        });
        exitButton?.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

}