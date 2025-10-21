// ...existing code...
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PulseScaleEase : MonoBehaviour
{
    [Tooltip("비워두면 이 컴포넌트를 붙인 오브젝트를 사용합니다.")]
    public Transform target;

    [Tooltip("자식 오브젝트 전체에 적용할지 여부")]
    public bool applyToChildren = false;

    [Tooltip("비활성(활성화되지 않은) 자식도 포함할지 여부")]
    public bool includeInactive = false;

    [Tooltip("기본 스케일(시작 스케일). Start에서 target의 스케일로 초기화됩니다.")]
    public Vector3 baseScale = Vector3.one;

    [Tooltip("확대/축소 비율 (예: 0.2 = ±20%)")]
    public float amplitude = 0.2f;

    [Tooltip("초당 반복 횟수 (주파수)")]
    public float frequency = 1f;

    [Tooltip("시작 위상을 랜덤화해서 여러 요소가 동시에 동일하게 동작하는 것을 방지")]
    public bool randomizePhase = true;

    [Tooltip("로컬 스케일을 균일하게 적용할지 여부")]
    public bool uniform = true;

    [Tooltip("애니메이션 커브로 펄스의 easing을 제어합니다. (0 -> 1 입력 범위)")]
    public AnimationCurve easeCurve;

    [Tooltip("씬 플레이 직후 바로 시작하지 않고 지연시킬 시간(초). 0이면 즉시 시작)")]
    public float startDelay = 0f;

    [Tooltip("true면 Start에서 자동으로 펄스 시작, false면 수동으로 Play() 호출해야 시작")]
    public bool autoStart = true;

    [Tooltip("알파(투명도)로 사라졌다 나타나는 효과 사용 여부")]
    public bool enableFade = false;

    [Tooltip("작아질 때 최소 알파값 (0 = 완전 투명)")]
    public float minAlpha = 0f;

    // 내부 상태: 현재 재생 중인지 여부
    bool running = false;

    // 대상 정보 구조체
    class TargetInfo
    {
        public Transform t;
        public Vector3 baseScale;
        public float phaseOffset;

        public CanvasGroup canvasGroup;
        public SpriteRenderer spriteRenderer;
        public Renderer meshRenderer;
        public Material runtimeMaterial;
    }

    List<TargetInfo> targets = new List<TargetInfo>();

    void Start()
    {
        if (target == null) target = transform;

        // 기본 커브 설정 (비어있으면)
        if (easeCurve == null || easeCurve.keys.Length == 0)
        {
            easeCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f)
            );
        }

        // 대상 수집
        targets.Clear();
        if (applyToChildren)
        {
            // [이 부분 수정] target의 '모든 자손' 대신 '직계 자식'만 반복합니다.
            for (int i = 0; i < target.childCount; i++)
            {
                Transform tr = target.GetChild(i);

                // '비활성 자식 포함' 옵션에 따라 비활성화된 오브젝트를 건너뜁니다.
                if (!includeInactive && !tr.gameObject.activeSelf) continue;

                AddTarget(tr);
            }
        }
        else
        {
            // 단일 대상: target 자체
            AddTarget(target);
        }

        // 자동 시작
        if (autoStart)
            StartCoroutine(StartWithDelay());
    }

    void AddTarget(Transform tr)
    {
        var info = new TargetInfo();
        info.t = tr;
        info.baseScale = tr.localScale;
        info.phaseOffset = randomizePhase ? Random.Range(0f, 2f * Mathf.PI) : 0f;

        if (enableFade)
        {
            // 페이드용 컴포넌트 검색 (CanvasGroup > SpriteRenderer > Renderer)
            info.canvasGroup = tr.GetComponent<CanvasGroup>();
            if (info.canvasGroup == null)
                info.canvasGroup = tr.GetComponentInChildren<CanvasGroup>();

            if (info.canvasGroup == null)
            {
                info.spriteRenderer = tr.GetComponent<SpriteRenderer>();
                if (info.spriteRenderer == null)
                    info.spriteRenderer = tr.GetComponentInChildren<SpriteRenderer>();
            }

            if (info.canvasGroup == null && info.spriteRenderer == null)
            {
                info.meshRenderer = tr.GetComponent<Renderer>();
                if (info.meshRenderer == null)
                    info.meshRenderer = tr.GetComponentInChildren<Renderer>();

                if (info.meshRenderer != null)
                {
                    // 런타임에서 머티리얼 인스턴스 생성(주의: 많은 오브젝트에 사용하면 메모리 영향)
                    info.runtimeMaterial = info.meshRenderer.material;
                }
            }

            // 초기 알파 유지
            ApplyAlphaToInfo(info, 1f);
        }

        targets.Add(info);
    }

    IEnumerator StartWithDelay()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);
        running = true;
    }

    public void Play()
    {
        running = true;
    }

    public void Stop()
    {
        running = false;
        // 리셋
        foreach (var info in targets)
        {
            if (info.t != null) info.t.localScale = info.baseScale;
            if (enableFade) ApplyAlphaToInfo(info, 1f);
        }
    }

    void Update()
    {
        if (!running) return;

        float time = Time.time;

        foreach (var info in targets)
        {
            if (info.t == null) continue;

            float p = 0.5f * (1f + Mathf.Sin((time * frequency * 2f * Mathf.PI) + info.phaseOffset));
            float eased = easeCurve.Evaluate(p);
            float scaleFactor = 1f + (eased * 2f - 1f) * amplitude;

            if (uniform)
                info.t.localScale = info.baseScale * scaleFactor;
            else
                info.t.localScale = new Vector3(info.baseScale.x * scaleFactor, info.baseScale.y * scaleFactor, info.baseScale.z * scaleFactor);

            if (enableFade)
            {
                float alpha = Mathf.Lerp(minAlpha, 1f, eased);
                ApplyAlphaToInfo(info, alpha);
            }
        }
    }

    void ApplyAlphaToInfo(TargetInfo info, float a)
    {
        if (info.canvasGroup != null)
        {
            info.canvasGroup.alpha = a;
            return;
        }

        if (info.spriteRenderer != null)
        {
            Color c = info.spriteRenderer.color;
            c.a = a;
            info.spriteRenderer.color = c;
            return;
        }

        if (info.runtimeMaterial != null)
        {
            if (info.runtimeMaterial.HasProperty("_Color"))
            {
                Color c = info.runtimeMaterial.color;
                c.a = a;
                info.runtimeMaterial.color = c;
            }
            return;
        }
    }
}
// ...existing code...