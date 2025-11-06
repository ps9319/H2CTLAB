using System.IO;
using UnityEngine;
using UnityEngine.VFX;

public class TextureMatcher : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private string texturePropertyName = "Texture"; // VFX 속성 이름

    void OnEnable()
    {
        LoadTextureToVFX();
    }

    private void LoadTextureToVFX()
    {
        // EventListener에서 현재 이미지 경로 가져오기
        string localFilePath = EventListener.Instance.GetCurrentImagePath();
        
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
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);

            // VFX에 텍스처 설정
            if (vfx != null)
            {
                vfx.SetTexture(texturePropertyName, texture);
                Debug.Log($"VFX 텍스처 설정 완료: {localFilePath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"텍스처 로드 실패: {ex.Message}");
        }
    }
}