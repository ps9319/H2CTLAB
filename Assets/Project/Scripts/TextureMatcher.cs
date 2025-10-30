using System.IO;
using UnityEngine;
using UnityEngine.VFX;

public class TextureMatcher : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private string texturePropertyName = "Texture"; // VFX 속성 이름
    [SerializeField] private string localFileName = "latest_island.png"; // 다운로드된 파일 이름

    private string localFilePath;

    void OnEnable()
    {
        localFilePath = Path.Combine(Application.persistentDataPath, localFileName);
        LoadTextureToVFX();
    }

    private void LoadTextureToVFX()
    {
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
                Debug.Log("VFX 텍스처 설정 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"텍스처 로드 실패: {ex.Message}");
        }
    }
}