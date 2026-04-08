using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VRSceneIntroPanel : MonoBehaviour
{
    [Header("Text")]
    public string title = "Scene Info";

    [TextArea(4, 10)]
    public string body = "Edit this message in the Inspector.";

    public Font chineseFont;
    public int titleFontSize = 28;
    public int bodyFontSize = 18;

    [Header("Placement")]
    public float forwardDistance = 1.8f;
    public float verticalOffset = -0.12f;
    public float visibleSeconds = 5f;
    public Vector2 panelSize = new Vector2(900f, 360f);
    public float worldScale = 0.001f;

    [Header("Style")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.82f);
    public Color titleColor = Color.white;
    public Color bodyColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Timing")]
    public float startupDelay = 0.25f;
    public int cameraSearchFrames = 120;
    public bool destroyAfterHide = false;

    private GameObject panelRoot;

    private IEnumerator Start()
    {
        if (startupDelay > 0f)
        {
            yield return new WaitForSeconds(startupDelay);
        }

        Transform viewAnchor = null;

        for (int i = 0; i < cameraSearchFrames; i++)
        {
            viewAnchor = ResolveViewAnchor();

            if (viewAnchor != null)
            {
                break;
            }

            yield return null;
        }

        if (viewAnchor == null)
        {
            Debug.LogWarning("[VRSceneIntroPanel] Could not find CenterEyeAnchor or MainCamera.");
            yield break;
        }

        BuildPanel();
        PositionPanel(viewAnchor);

        if (visibleSeconds > 0f)
        {
            yield return new WaitForSeconds(visibleSeconds);
            HidePanel();
        }
    }

    private Transform ResolveViewAnchor()
    {
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            return centerEye.transform;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        var taggedCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (taggedCamera != null)
        {
            return taggedCamera.transform;
        }

        return null;
    }

    private void BuildPanel()
    {
        if (panelRoot != null)
        {
            return;
        }

        panelRoot = new GameObject("IntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        panelRoot.transform.SetParent(transform, false);
        panelRoot.layer = gameObject.layer;

        var rectTransform = panelRoot.GetComponent<RectTransform>();
        rectTransform.sizeDelta = panelSize;

        var canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

        var scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;

        panelRoot.transform.localScale = Vector3.one * worldScale;

        var background = CreateUIObject<Image>("Background", panelRoot.transform);
        var backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.color = panelColor;

        var titleText = CreateUIObject<Text>("TitleText", background.transform);
        ConfigureText(titleText, title, titleFontSize, titleColor, TextAnchor.MiddleLeft);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(panelSize.x - 80f, 56f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);

        var bodyText = CreateUIObject<Text>("BodyText", background.transform);
        ConfigureText(bodyText, body, bodyFontSize, bodyColor, TextAnchor.UpperLeft);
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(panelSize.x - 80f, panelSize.y - 130f);
        bodyRect.anchoredPosition = new Vector2(0f, -22f);
    }

    private void PositionPanel(Transform viewAnchor)
    {
        var flatForward = Vector3.ProjectOnPlane(viewAnchor.forward, Vector3.up);

        if (flatForward.sqrMagnitude < 0.001f && viewAnchor.parent != null)
        {
            flatForward = Vector3.ProjectOnPlane(viewAnchor.parent.forward, Vector3.up);
        }

        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        var panelPosition = viewAnchor.position + (flatForward * forwardDistance);
        panelPosition.y = viewAnchor.position.y + verticalOffset;

        transform.position = panelPosition;

        // World-space UI faces opposite of transform.forward, so point forward away from the player.
        transform.rotation = Quaternion.LookRotation(transform.position - viewAnchor.position, Vector3.up);
    }

    private void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (destroyAfterHide)
        {
            Destroy(gameObject);
        }
    }

    private void ConfigureText(Text textComponent, string content, int fontSize, Color color, TextAnchor alignment)
    {
        var fontToUse = chineseFont != null ? chineseFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        textComponent.text = content;
        textComponent.font = fontToUse;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.supportRichText = true;
    }

    private static T CreateUIObject<T>(string objectName, Transform parent) where T : Graphic
    {
        var child = new GameObject(objectName, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
    }
}
