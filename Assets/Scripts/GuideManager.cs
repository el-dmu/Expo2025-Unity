using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class GuideStyle
{
    [TextArea(4, 10)]
    public string text;
    public Vector2 panelSize;
    public float fontSize;
    public float displayTime = 5.0f;
}

public class GuideManager : MonoBehaviour
{
    // --- UI 요소 ---
    public TextMeshProUGUI textMesh;
    public CanvasGroup canvasGroup;
    public RectTransform panelRectTransform;

    // ★ 고정 안내문 관련 UI 요소들
    [Header("다음 단계 안내문")]
    public GameObject fixedGuideObject;
    public CanvasGroup fixedGuideCanvasGroup; // ★ 고정 안내문의 CanvasGroup

    // --- 시간 설정 ---
    [Header("시간 설정")]
    public float fadeTime = 1.0f;
    public float delayBetweenTexts = 1.5f;

    // --- 안내 문구 및 스타일 ---
    [Header("안내 문구 및 스타일")]
    public GuideStyle[] guideStyles;


    void Start()
    {
        // 시작할 때 고정 안내문이 꺼져있는지 확인
        if (fixedGuideObject != null)
        {
            fixedGuideObject.SetActive(false);
        }
        StartCoroutine(StartGuide());
    }

    IEnumerator StartGuide()
    {
        canvasGroup.alpha = 0;
        yield return new WaitForSeconds(2f);

        foreach (GuideStyle style in guideStyles)
        {
            // 스타일 적용
            textMesh.text = style.text;
            panelRectTransform.sizeDelta = style.panelSize;
            textMesh.fontSize = style.fontSize;

            // 페이드 인/아웃
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, fadeTime));
            yield return new WaitForSeconds(style.displayTime);
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, fadeTime));
            yield return new WaitForSeconds(delayBetweenTexts);
        }

        // 모든 순차 안내가 끝나면
        // 1. 현재 안내 패널을 비활성화
        // gameObject.SetActive(false);

        // 2. ★ 다음 고정 안내 패널을 페이드인으로 활성화
        if (fixedGuideObject != null && fixedGuideCanvasGroup != null)
        {
            fixedGuideCanvasGroup.alpha = 0; // 시작은 투명하게
            fixedGuideObject.SetActive(true); // 오브젝트를 켠 다음
            yield return StartCoroutine(FadeCanvasGroup(fixedGuideCanvasGroup, 1f, fadeTime)); // 페이드인 실행
        }
    }

    // ★ 어떤 CanvasGroup이든 페이드 효과를 줄 수 있도록 수정
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0;
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = targetAlpha;
    }
}