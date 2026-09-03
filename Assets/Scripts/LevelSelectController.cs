using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class LevelSelectController : MonoBehaviour
{
    [SerializeField] private UIDocument levelSelectDocument;
    [SerializeField] private float scrollStep = 120f;

    private ScrollView levelScrollView;
    private Button scrollUpButton;
    private Button scrollDownButton;
    private Button backButton;
    private Button levelOneButton;

    private void Start()
    {
        VisualElement root = levelSelectDocument.rootVisualElement;
        levelScrollView = root.Q<ScrollView>("LevelScrollView");
        scrollUpButton = root.Q<Button>("ScrollUpButton");
        scrollDownButton = root.Q<Button>("ScrollDownButton");
        backButton = root.Q<Button>("BackButton");
        levelOneButton = root.Q<Button>("LevelOneButton");
        if (scrollUpButton != null) scrollUpButton.clicked += ScrollUp;
        if (scrollDownButton != null) scrollDownButton.clicked += ScrollDown;
        if (backButton != null) backButton.clicked += OpenMainMenu;
        if (levelOneButton != null) levelOneButton.clicked += OpenLevelOne;
    }

    private void OnDestroy()
    {
        if (scrollUpButton != null) scrollUpButton.clicked -= ScrollUp;
        if (scrollDownButton != null) scrollDownButton.clicked -= ScrollDown;
        if (backButton != null) backButton.clicked -= OpenMainMenu;
        if (levelOneButton != null) levelOneButton.clicked -= OpenLevelOne;
    }

    private void ScrollUp() => ScrollBy(-scrollStep);
    private void ScrollDown() => ScrollBy(scrollStep);
    private void OpenMainMenu() => SceneManager.LoadScene("MainMenu");
    private void OpenLevelOne() => SceneManager.LoadScene("SampleScene");

    private void ScrollBy(float amount)
    {
        if (levelScrollView == null) return;
        Vector2 offset = levelScrollView.scrollOffset;
        levelScrollView.scrollOffset = new Vector2(offset.x, Mathf.Max(0f, offset.y + amount));
    }
}