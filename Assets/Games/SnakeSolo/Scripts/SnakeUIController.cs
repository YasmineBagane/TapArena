using UnityEngine;
using UnityEngine.UIElements;

public sealed class SnakeUIController : MonoBehaviour
{
    private Label scoreLabel;
    private Label timerLabel;
    private Label finalScoreLabel;
    private Label reasonLabel;
    private VisualElement gameOverPanel;
    private Button restartButton;

    public static SnakeUIController Create()
    {
        GameObject uiObject = new GameObject("Snake UI");
        SnakeUIController controller = uiObject.AddComponent<SnakeUIController>();
        controller.Build();
        return controller;
    }

    public void UpdateHud(int score, float timeRemaining)
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Score: {score}";
        }

        if (timerLabel != null)
        {
            timerLabel.text = $"Time: {Mathf.CeilToInt(timeRemaining)}";
        }
    }

    public void ShowGameOver(int score, string reason)
    {
        if (gameOverPanel == null)
        {
            return;
        }

        gameOverPanel.style.display = DisplayStyle.Flex;
        finalScoreLabel.text = $"Score is {score}";
        reasonLabel.text = reason;
        restartButton.Focus();
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.style.display = DisplayStyle.None;
        }
    }

    private void Build()
    {
        UIDocument document = gameObject.AddComponent<UIDocument>();
        PanelSettings panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.name = "SnakeRuntimePanelSettings";
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1280, 720);

        // A ThemeStyleSheet is required for UI Toolkit to render text/styling at runtime.
        // Create a PanelSettings asset in the project (Assets > Create > UI Toolkit > Panel
        // Settings) - it auto-includes the default theme - drop it in a "Resources" folder
        // as "SnakeUIPanelSettings", and we'll pull its theme from there.
        PanelSettings themedTemplate = Resources.Load<PanelSettings>("SnakeUIPanelSettings");
        if (themedTemplate != null && themedTemplate.themeStyleSheet != null)
        {
            panelSettings.themeStyleSheet = themedTemplate.themeStyleSheet;
        }

        document.panelSettings = panelSettings;

        VisualElement root = document.rootVisualElement;
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.right = 0;
        root.style.top = 0;
        root.style.bottom = 0;
        root.style.flexDirection = FlexDirection.Column;
        root.pickingMode = PickingMode.Ignore;

        VisualElement hud = new VisualElement();
        hud.name = "TopCenterHud";
        hud.style.position = Position.Absolute;
        hud.style.top = 14;
        hud.style.left = 0;
        hud.style.right = 0;
        hud.style.alignItems = Align.Center;
        hud.style.justifyContent = Justify.Center;
        hud.pickingMode = PickingMode.Ignore;
        root.Add(hud);

        scoreLabel = new Label("Score: 0");
        scoreLabel.name = "ScoreLabel";
        scoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        scoreLabel.style.fontSize = 30;
        scoreLabel.style.color = new Color(0.68f, 1f, 0.42f, 1f);
        scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        scoreLabel.style.textShadow = new TextShadow { offset = new Vector2(2f, 2f), blurRadius = 2f, color = Color.black };
        hud.Add(scoreLabel);

        timerLabel = new Label("Time: 90");
        timerLabel.name = "TimerLabel";
        timerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        timerLabel.style.fontSize = 18;
        timerLabel.style.color = new Color(0.75f, 0.92f, 1f, 1f);
        timerLabel.style.marginTop = 2;
        hud.Add(timerLabel);

        gameOverPanel = new VisualElement();
        gameOverPanel.name = "GameOverOverlay";
        gameOverPanel.style.position = Position.Absolute;
        gameOverPanel.style.left = 0;
        gameOverPanel.style.right = 0;
        gameOverPanel.style.top = 0;
        gameOverPanel.style.bottom = 0;
        gameOverPanel.style.alignItems = Align.Center;
        gameOverPanel.style.justifyContent = Justify.Center;
        gameOverPanel.style.backgroundColor = new Color(0f, 0f, 0f, 0.68f);
        gameOverPanel.pickingMode = PickingMode.Position;
        root.Add(gameOverPanel);

        VisualElement card = new VisualElement();
        card.style.width = 460;
        card.style.paddingTop = 34;
        card.style.paddingBottom = 34;
        card.style.paddingLeft = 42;
        card.style.paddingRight = 42;
        card.style.alignItems = Align.Center;
        card.style.borderTopLeftRadius = 18;
        card.style.borderTopRightRadius = 18;
        card.style.borderBottomLeftRadius = 18;
        card.style.borderBottomRightRadius = 18;
        card.style.borderTopWidth = 2;
        card.style.borderRightWidth = 2;
        card.style.borderBottomWidth = 2;
        card.style.borderLeftWidth = 2;
        card.style.borderTopColor = new Color(0f, 0.85f, 1f, 1f);
        card.style.borderRightColor = new Color(0f, 0.85f, 1f, 1f);
        card.style.borderBottomColor = new Color(0f, 0.85f, 1f, 1f);
        card.style.borderLeftColor = new Color(0f, 0.85f, 1f, 1f);
        card.style.backgroundColor = new Color(0.02f, 0.05f, 0.12f, 0.96f);
        gameOverPanel.Add(card);

        Label title = new Label("Game Over");
        title.name = "GameOverTitle";
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.fontSize = 54;
        title.style.color = new Color(1f, 0.26f, 0.25f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        card.Add(title);

        finalScoreLabel = new Label("Score is 0");
        finalScoreLabel.name = "FinalScoreLabel";
        finalScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        finalScoreLabel.style.fontSize = 30;
        finalScoreLabel.style.marginTop = 18;
        finalScoreLabel.style.color = Color.white;
        card.Add(finalScoreLabel);

        reasonLabel = new Label("Reason");
        reasonLabel.name = "GameOverReasonLabel";
        reasonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        reasonLabel.style.fontSize = 18;
        reasonLabel.style.marginTop = 8;
        reasonLabel.style.color = new Color(0.65f, 0.9f, 1f, 1f);
        card.Add(reasonLabel);

        restartButton = new Button(() => SnakeGameManager.Instance.ResetGame());
        restartButton.name = "RestartButton";
        restartButton.text = "Restart (R)";
        restartButton.style.marginTop = 24;
        restartButton.style.width = 180;
        restartButton.style.height = 46;
        restartButton.style.fontSize = 19;
        restartButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        card.Add(restartButton);

        HideGameOver();
    }
}