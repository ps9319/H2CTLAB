using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TextureArrayCreator : EditorWindow
{
    private List<Texture2D> textureList = new List<Texture2D>();

    [MenuItem("Tools/Texture2DArray Creator")]
    public static void ShowWindow()
    {
        GetWindow<TextureArrayCreator>("Texture2DArray Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Texture2DArray 생성기", EditorStyles.boldLabel);

        // 항상 마지막에 None 슬롯이 있도록 관리
        if (textureList.Count == 0 || textureList[textureList.Count - 1] != null)
        {
            textureList.Add(null);
        }

        // 텍스처 리스트 표시 및 삭제
        for (int i = 0; i < textureList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            textureList[i] = (Texture2D)EditorGUILayout.ObjectField(textureList[i], typeof(Texture2D), false);

            // 마지막 None 슬롯이 아니고, 값이 있을 때만 삭제 버튼 표시
            if (textureList[i] != null && i != textureList.Count - 1)
            {
                if (GUILayout.Button("삭제", GUILayout.Width(40)))
                {
                    textureList.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // 중복 제거 및 None 슬롯 정리
        for (int i = 0; i < textureList.Count - 1; i++)
        {
            for (int j = i + 1; j < textureList.Count; j++)
            {
                if (textureList[i] != null && textureList[i] == textureList[j])
                {
                    textureList[j] = null;
                }
            }
        }

        // 마지막 None 슬롯이 아닌 곳에 None이 있으면 삭제
        for (int i = textureList.Count - 2; i >= 0; i--)
        {
            if (textureList[i] == null)
            {
                textureList.RemoveAt(i);
            }
        }

        EditorGUILayout.Space();

        // 선택된 텍스처 추가
        if (GUILayout.Button("선택한 텍스처 추가"))
        {
            foreach (var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets))
            {
                if (!textureList.Contains(tex))
                    textureList.Insert(textureList.Count - 1, tex);
            }
        }

        EditorGUILayout.Space();

        // 생성 버튼
        bool canCreate = textureList.Count > 1 && textureList.Exists(t => t != null);
        GUI.enabled = canCreate;
        if (GUILayout.Button("Texture2DArray 생성"))
        {
            CreateTextureArray();
        }
        GUI.enabled = true;
    }

    void CreateTextureArray()
    {
        // null이 아닌 텍스처만 추출
        List<Texture2D> validTextures = textureList.FindAll(t => t != null);

        if (validTextures.Count == 0)
        {
            Debug.LogError("텍스처가 없습니다!");
            return;
        }

        int width = validTextures[0].width;
        int height = validTextures[0].height;
        TextureFormat targetFormat = TextureFormat.RGBA32; // 원하는 포맷으로 고정

        // 모든 텍스처를 동일 포맷으로 변환
        List<Texture2D> convertedTextures = new List<Texture2D>();
        foreach (var tex in validTextures)
        {
            Texture2D converted = tex;
            if (tex.format != targetFormat || !tex.isReadable)
            {
                RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                converted = new Texture2D(width, height, targetFormat, false);
                converted.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                converted.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }
            convertedTextures.Add(converted);
        }

        Texture2DArray textureArray = new Texture2DArray(width, height, convertedTextures.Count, targetFormat, false);

        for (int i = 0; i < convertedTextures.Count; i++)
        {
            Graphics.CopyTexture(convertedTextures[i], 0, 0, textureArray, i, 0);
        }

        textureArray.Apply();

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Texture2DArray",
            "NewTextureArray",
            "asset",
            "저장할 위치와 파일명을 선택하세요.",
            "Assets"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(textureArray, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Texture2DArray 에셋 생성 완료! " + path);
        }
        else
        {
            Debug.LogWarning("저장이 취소되었습니다.");
        }
    }
}
