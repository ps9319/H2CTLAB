using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageCreator : MonoBehaviour
{
    private Collider spawnCollider;

    [Header("Prefab")]
    public GameObject imagePrefab; // 루트에 프레임 Renderer, 자식 "Cube"에 이미지 Renderer

    [Header("Materials")]
    public List<Material> frameMaterials = new List<Material>(); // 루트(PulseImage)에 적용
    public List<Material> imageMaterials = new List<Material>(); // 자식 Cube에 적용

    [Header("Spawn Settings")]
    public int maxImages = 10;
    public float maxSpawnInterval = 2f;
    public float imageLifetime = 5f;

    [Header("Scale Settings")]
    public float minScale = 0.5f;
    public float maxScale = 2f;

    [Header("Overlap Prevention")]
    public float minDistanceBetweenImages = 2f;
    public int maxSpawnAttempts = 30;

    private readonly List<GameObject> activeImages = new List<GameObject>();

    private void Awake()
    {
        spawnCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        StartCoroutine(SpawnImagesRoutine());
    }

    private IEnumerator SpawnImagesRoutine()
    {
        while (true)
        {
            if (activeImages.Count < maxImages && imagePrefab != null
                && frameMaterials.Count > 0 && imageMaterials.Count > 0)
            {
                SpawnRandomImage();
            }
            float randomInterval = Random.Range(0f, maxSpawnInterval);
            yield return new WaitForSeconds(randomInterval);
        }
    }

    private void SpawnRandomImage()
    {
        // 위치 찾기
        Vector3 pos;
        int attempts = 0;
        do
        {
            pos = GetRandom2DPositionInCollider();
            if (++attempts >= maxSpawnAttempts) return;
        } while (!IsPositionValid(pos));

        // 생성
        GameObject go = Instantiate(imagePrefab, pos, Quaternion.identity, transform);

        // 스케일 (z 얇게)
        float s = Random.Range(minScale, maxScale);
        go.transform.localScale = new Vector3(s, s, 0.01f);

        // ✅ 프레임/이미지 각각 랜덤 머티리얼 적용
        ApplyRandomMaterials(go);

        // 페이드 컴포넌트 (없으면 추가). 이제 부모와 모든 자식이 함께 페이드
        var effect = go.GetComponent<ImageEffect>();
        if (effect == null) effect = go.AddComponent<ImageEffect>();

        activeImages.Add(go);
        StartCoroutine(ManageImageLifetime(go));
    }

    private void ApplyRandomMaterials(GameObject go)
    {
        // 루트 = 프레임
        var frameRenderer = go.GetComponent<Renderer>();
        if (frameRenderer != null && frameMaterials.Count > 0)
        {
            frameRenderer.material = frameMaterials[Random.Range(0, frameMaterials.Count)];
        }

        // 자식 "Cube" = 이미지
        var cubeT = go.transform.Find("Cube");
        if (cubeT != null)
        {
            var imageRenderer = cubeT.GetComponent<Renderer>();
            if (imageRenderer != null && imageMaterials.Count > 0)
            {
                imageRenderer.material = imageMaterials[Random.Range(0, imageMaterials.Count)];
            }
        }
    }

    private bool IsPositionValid(Vector3 p)
    {
        foreach (var go in activeImages)
        {
            if (go == null) continue;
            if (Vector2.Distance((Vector2)p, (Vector2)go.transform.position) < minDistanceBetweenImages)
                return false;
        }
        return true;
    }

    private Vector3 GetRandom2DPositionInCollider()
    {
        if (spawnCollider == null)
        {
            var t = transform.position;
            return new Vector3(t.x, t.y, 0f);
        }

        if (spawnCollider is BoxCollider box)
        {
            var c = box.center; var sz = box.size;
            float x = Random.Range(-sz.x * 0.5f, sz.x * 0.5f);
            float y = Random.Range(-sz.y * 0.5f, sz.y * 0.5f);
            return box.transform.TransformPoint(c + new Vector3(x, y, 0f));
        }

        if (spawnCollider is SphereCollider sphere)
        {
            var c = sphere.center; float r = sphere.radius;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float rr = Random.Range(0f, r);
            return sphere.transform.TransformPoint(c + new Vector3(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr, 0f));
        }

        if (spawnCollider is CapsuleCollider cap)
        {
            var c = cap.center; float r = cap.radius; float h = cap.height;
            float half = (h * 0.5f) - r;
            float y = Random.Range(-half, half);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float rr = Random.Range(0f, r);
            var local = c + new Vector3(Mathf.Cos(ang) * rr, y, Mathf.Sin(ang) * rr);
            return cap.transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        }

        var p0 = spawnCollider.transform.position;
        return new Vector3(p0.x, p0.y, 0f);
    }

    private IEnumerator ManageImageLifetime(GameObject image)
    {
        float lifetime = imageLifetime;
        var effect = image.GetComponent<ImageEffect>();
        if (effect != null)
            lifetime = effect.fadeInDuration + effect.stayDuration + effect.fadeOutDuration;

        yield return new WaitForSeconds(lifetime);
        DestroyImage(image);
    }

    private void DestroyImage(GameObject image)
    {
        if (image == null) return;
        activeImages.Remove(image);
        Destroy(image);
    }

    public void RemoveImage(GameObject image) => DestroyImage(image);

    public void ClearAllImages()
    {
        foreach (var go in activeImages.ToArray())
            if (go != null) Destroy(go);
        activeImages.Clear();
    }

    private void OnDestroy() => ClearAllImages();
}