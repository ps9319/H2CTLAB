using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class TextureMatcher : MonoBehaviour
{
    [SerializeField] private List<VisualEffect> vfxList;
    [SerializeField] private string texturePropertyName = "Texture"; // VFX 속성 이름
    private List<Texture2D> textureList = new List<Texture2D>();
    private bool isLoaded = false;

    void Start()
    {
        LoadTexturesToVFX();
    }

    private void Update()
    {
        // VFX에 텍스처 설정
        if (!isLoaded && vfxList != null && vfxList[0].gameObject.activeSelf && textureList.Count == vfxList.Count)
        {
            for (int i = 0; i < vfxList.Count; i++)
            {
                if (vfxList[i] != null && vfxList[i].gameObject.activeSelf)
                {
                    vfxList[i].SetTexture(texturePropertyName, textureList[i]);
                }
            }

            isLoaded = true;
        }
    }

    private void LoadTexturesToVFX()
    {  
        string localFilePath = EventListener.Instance?.GetCurrentImagePath()
                               ?? Path.GetFullPath(Path.Combine(Application.dataPath, "Project", "Resource", "old", "002_00001_.png"));
        
        if (string.IsNullOrEmpty(localFilePath))
        {
            Debug.LogError("EventListener에서 이미지 경로를 가져올 수 없습니다.");
            return;
        }

        if (!File.Exists(localFilePath))
        {
            Debug.LogError($"파일을 찾을 수 없습니다: {localFilePath}");
            return;
        }

        try
        {
            // 로컬 파일을 Texture2D로 로드
            byte[] fileData = File.ReadAllBytes(localFilePath);

            // VFX 개수만큼 텍스처 생성
            for (int i = 0; i < vfxList.Count; i++)
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                textureList.Add(texture);
            }

            Debug.Log($"Texture 로드 성공: {localFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"텍스처 로드 실패: {ex.Message}");
        }
    }
}