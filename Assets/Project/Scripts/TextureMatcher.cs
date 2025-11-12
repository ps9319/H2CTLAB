using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class TextureMatcher : MonoBehaviour
{
    [SerializeField] private List<VisualEffect> vfxList;
    [SerializeField] private string texturePropertyName = "Texture"; // VFX 속성 이름
    private Texture2D texture;
    private List<bool> vfxLoaded;

    void Start()
    {
        LoadTexturesToVFX();
        vfxLoaded = new List<bool>(new bool[vfxList.Count]);
    }

    private void Update()
    {
        // 텍스처나 VFX가 없으면 return
        if (vfxList == null || texture == null || vfxList.Count == 0)
            return;

        for (int i = 0; i < vfxList.Count; i++)
        {
            if (!vfxLoaded[i] && vfxList[i] != null && vfxList[i].gameObject.activeInHierarchy)
            {
                vfxList[i].SetTexture(texturePropertyName, texture);
                vfxLoaded[i] = true;
            }
        }
    }

    private void LoadTexturesToVFX()
    {
        string localFilePath = EventListener.Instance?.GetCurrentImagePath()
                               ?? Path.GetFullPath(Path.Combine(Application.dataPath, "Project", "Resource", "old",
                                   "002_00001_.png"));

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
            texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);


            Debug.Log($"Texture 로드 성공: {localFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"텍스처 로드 실패: {ex.Message}");
        }
    }
}