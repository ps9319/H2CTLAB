using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AutoMaterialCreator : AssetPostprocessor
{
    // EditorPrefs에서 경로/옵션을 불러오는 속성
    private static string SOURCE_TEXTURE_FOLDER
    {
        get { return EditorPrefs.GetString("AutoMaterialCreator_SourceFolder", "Assets/Textures/Source"); }
    }
    
    private static string TARGET_MATERIAL_FOLDER
    {
        get { return EditorPrefs.GetString("AutoMaterialCreator_TargetFolder", "Assets/Materials/Generated"); }
    }

    private static string NAME_PREFIX
    {
        get { return EditorPrefs.GetString("AutoMaterialCreator_NamePrefix", ""); }
    }

    private static bool AUTO_CREATE_ON_IMPORT
    {
        get { return EditorPrefs.GetBool("AutoMaterialCreator_AutoCreateOnImport", false); }
    }

    private static bool CONVERT_TO_SPRITE
    {
        get { return EditorPrefs.GetBool("AutoMaterialCreator_ConvertToSprite", false); }
    }

    // 에셋이 임포트된 후 자동으로 호출되는 함수 (자동 생성 옵션이 켜져있을 때만 동작)
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!AUTO_CREATE_ON_IMPORT) return;

        foreach (string assetPath in importedAssets)
        {
            if (assetPath.StartsWith(SOURCE_TEXTURE_FOLDER) && IsTextureFile(assetPath))
            {
                CreateMaterialFromTexture(assetPath);
            }
        }
    }

    // 텍스처 파일인지 확인
    private static bool IsTextureFile(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg" ||
               extension == ".tga" || extension == ".psd" || extension == ".tiff";
    }

    // 텍스처로부터 머티리얼 생성 (개별 경로)
    public static void CreateMaterialFromTexture(string texturePath)
    {
        string targetFolder = TARGET_MATERIAL_FOLDER;

        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            CreateFolderRecursive(targetFolder);
        }

        // 옵션: Sprite로 변환 설정되어 있으면 임포터 설정 변경 및 재임포트
        if (CONVERT_TO_SPRITE)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError($"텍스처를 로드할 수 없습니다: {texturePath}");
            return;
        }

        // 머티리얼 이름 생성: prefix가 설정되어 있으면 prefix+index, 아니면 텍스처 이름 기반
        string materialName;
        string prefix = NAME_PREFIX;
        if (!string.IsNullOrEmpty(prefix))
        {
            int nextIndex = GetNextIndexForPrefix(targetFolder, prefix);
            materialName = prefix + nextIndex.ToString();
        }
        else
        {
            materialName = Path.GetFileNameWithoutExtension(texturePath);
        }

        string materialPath = Path.Combine(targetFolder, materialName + ".mat").Replace('\\', '/');

        // 같은 경로에 머티리얼이 이미 있는지 확인
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
        {
            Debug.Log($"머티리얼이 이미 존재합니다: {materialPath}");
            return;
        }

        // 같은 텍스처를 사용하는 머티리얼이 이미 있는지 확인
        if (IsDuplicateTextureMaterial(targetFolder, texture))
        {
            Debug.Log($"같은 텍스처를 사용하는 머티리얼이 이미 존재합니다: {texture.name}");
            return;
        }

        // URP Lit 셰이더 사용 (없으면 Standard로 폴백)
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
            Debug.LogWarning("URP Lit 셰이더를 찾을 수 없어 Standard 셰이더를 사용합니다.");
        }
        
        Material newMaterial = new Material(shader);
        newMaterial.mainTexture = texture;
        newMaterial.name = materialName;

        AssetDatabase.CreateAsset(newMaterial, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"머티리얼 생성 완료: {materialPath}");
    }

    // 폴더의 모든 텍스처를 Sprite로 변환 (수동 실행용)
    public static void ConvertFolderToSprites(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("오류", $"폴더가 존재하지 않습니다:\n{folder}", "확인");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("이미지 없음", $"폴더에 이미지가 없습니다:\n{folder}", "확인");
            return;
        }

        int converted = 0;
        int total = guids.Length;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                converted++;
                Debug.Log($"[{converted}/{total}] Sprite로 변환: {path}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"총 {total}개 중 {converted}개를 Sprite로 변환했습니다.", "확인");
    }

    // 폴더의 모든 텍스처로 머티리얼 생성 (수동 실행용)
    public static void CreateMaterialsFromFolder(string sourceFolder)
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            EditorUtility.DisplayDialog("오류", $"소스 폴더가 없습니다:\n{sourceFolder}", "확인");
            return;
        }

        string targetFolder = TARGET_MATERIAL_FOLDER;
        if (!AssetDatabase.IsValidFolder(targetFolder))
            CreateFolderRecursive(targetFolder);

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("이미지 없음", $"폴더에 이미지가 없습니다:\n{sourceFolder}", "확인");
            return;
        }

        int created = 0;
        foreach (string guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null) continue;

            // 옵션: ConvertToSprite가 켜져 있으면 임포터 재설정
            if (CONVERT_TO_SPRITE)
            {
                var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
            }

            string matName;
            if (!string.IsNullOrEmpty(NAME_PREFIX))
            {
                int idx = GetNextIndexForPrefix(targetFolder, NAME_PREFIX);
                matName = NAME_PREFIX + idx.ToString();
            }
            else
            {
                matName = Path.GetFileNameWithoutExtension(texPath);
            }

            string matPath = Path.Combine(targetFolder, matName + ".mat").Replace('\\', '/');
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
            {
                Debug.Log($"스킵(이미 존재함): {matPath}");
                continue;
            }

            // 같은 텍스처를 사용하는 머티리얼이 이미 있는지 확인
            if (IsDuplicateTextureMaterial(targetFolder, tex))
            {
                Debug.Log($"스킵(같은 텍스처 사용): {tex.name}");
                continue;
            }

            // URP Lit 셰이더 사용 (없으면 Standard로 폴백)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                Debug.LogWarning("URP Lit 셰이더를 찾을 수 없어 Standard 셰이더를 사용합니다.");
            }

            Material mat = new Material(shader);
            mat.mainTexture = tex;
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, matPath);
            created++;
            Debug.Log($"Created Material: {matPath} (from {texPath})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"{created}개의 머티리얼을 생성했습니다.", "확인");
    }

    // 같은 텍스처를 사용하는 머티리얼이 폴더에 이미 있는지 확인
    private static bool IsDuplicateTextureMaterial(string folder, Texture2D texture)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return false;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (existingMat != null && existingMat.mainTexture == texture)
            {
                return true; // 같은 텍스처를 사용하는 머티리얼 발견
            }
        }
        return false;
    }

    // 주어진 prefix에 대해 target 폴더에서 가장 큰 인덱스 찾아 +1 반환
    private static int GetNextIndexForPrefix(string folder, string prefix)
    {
        int maxIndex = 0;
        if (!AssetDatabase.IsValidFolder(folder))
            return 1;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith(prefix))
            {
                string suffix = name.Substring(prefix.Length);
                if (int.TryParse(suffix, out int idx))
                {
                    if (idx > maxIndex) maxIndex = idx;
                }
            }
        }
        return maxIndex + 1;
    }

    // 폴더를 재귀적으로 생성
    private static void CreateFolderRecursive(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        
        string[] folders = path.Split('/');
        string currentPath = folders[0];
        
        for (int i = 1; i < folders.Length; i++)
        {
            string newPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = newPath;
        }
        AssetDatabase.Refresh();
    }

    // 메뉴에서 수동으로 실행할 수 있는 기능 (레거시 유지)
    [MenuItem("Tools/Auto Material Creator/Create Materials from Source Folder")]
    public static void ManualCreateMaterials()
    {
        CreateMaterialsFromFolder(SOURCE_TEXTURE_FOLDER);
    }

    // 설정 변경을 위한 메뉴
    [MenuItem("Tools/Auto Material Creator/Settings")]
    public static void OpenSettings()
    {
        AutoMaterialCreatorSettingsWindow.ShowWindow();
    }
}

// 설정 창
public class AutoMaterialCreatorSettingsWindow : EditorWindow
{
    private string sourceFolderPath;
    private string targetFolderPath;
    private string namePrefix;
    private bool convertToSprite;
    private bool autoCreateOnImport;

    public static void ShowWindow()
    {
        GetWindow<AutoMaterialCreatorSettingsWindow>("Auto Material Creator Settings");
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        sourceFolderPath = EditorPrefs.GetString("AutoMaterialCreator_SourceFolder", "Assets/Textures/Source");
        targetFolderPath = EditorPrefs.GetString("AutoMaterialCreator_TargetFolder", "Assets/Materials/Generated");
        namePrefix = EditorPrefs.GetString("AutoMaterialCreator_NamePrefix", "");
        convertToSprite = EditorPrefs.GetBool("AutoMaterialCreator_ConvertToSprite", false);
        autoCreateOnImport = EditorPrefs.GetBool("AutoMaterialCreator_AutoCreateOnImport", false);
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Material Creator 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "여기서 설정한 경로/옵션이 실제로 사용됩니다.\n'변환' 버튼을 눌러 수동으로 처리하세요.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 소스 폴더 설정
        GUILayout.Label("소스 텍스처 폴더 (이미지를 넣을 폴더):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        sourceFolderPath = EditorGUILayout.TextField(sourceFolderPath);
        if (GUILayout.Button("찾기", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFolderPanel("소스 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    sourceFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (AssetDatabase.IsValidFolder(sourceFolderPath))
            EditorGUILayout.HelpBox("✓ 폴더 존재함", MessageType.None);
        else
            EditorGUILayout.HelpBox("✗ 폴더가 없습니다. '폴더 생성' 버튼을 눌러주세요.", MessageType.Warning);
        
        EditorGUILayout.Space();
        
        // 타겟 폴더 설정
        GUILayout.Label("타겟 머티리얼 폴더 (머티리얼이 생성될 폴더):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        targetFolderPath = EditorGUILayout.TextField(targetFolderPath);
        if (GUILayout.Button("찾기", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFolderPanel("타겟 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    targetFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (AssetDatabase.IsValidFolder(targetFolderPath))
            EditorGUILayout.HelpBox("✓ 폴더 존재함", MessageType.None);
        else
            EditorGUILayout.HelpBox("✗ 폴더가 없습니다. '폴더 생성' 버튼을 눌러주세요.", MessageType.Warning);

        EditorGUILayout.Space();

        // Prefix 입력 추가
        GUILayout.Label("머티리얼 이름 Prefix (비우면 텍스처명 사용):", EditorStyles.boldLabel);
        namePrefix = EditorGUILayout.TextField(namePrefix);
        EditorGUILayout.HelpBox("예: 'gogh'로 설정하면 gogh1, gogh2 ... 형식으로 생성됩니다.", MessageType.Info);

        EditorGUILayout.Space();

        // Sprite 변환 토글
        convertToSprite = EditorGUILayout.Toggle("Import as Sprite (자동 변환)", convertToSprite);
        EditorGUILayout.HelpBox("체크하면 수동/자동 생성 시 텍스처 임포트를 Sprite로 설정합니다.", MessageType.Info);

        // 자동 생성 토글
        autoCreateOnImport = EditorGUILayout.Toggle("Auto-create materials on import", autoCreateOnImport);
        EditorGUILayout.HelpBox("체크하면 Source 폴더로 파일을 넣었을 때 자동으로 머티리얼이 생성됩니다. (권장: 끄고 수동으로 사용)", MessageType.Info);
        
        EditorGUILayout.Space();

        // 수동 실행 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("폴더 생성", GUILayout.Height(28)))
        {
            CreateFolders();
        }
        if (GUILayout.Button("Convert Sprites Now", GUILayout.Height(28)))
        {
            CreateFolderIfNotExists(sourceFolderPath);
            AutoMaterialCreator.ConvertFolderToSprites(sourceFolderPath);
        }
        if (GUILayout.Button("Create Materials Now", GUILayout.Height(28)))
        {
            CreateFolderIfNotExists(targetFolderPath);
            AutoMaterialCreator.CreateMaterialsFromFolder(sourceFolderPath);
        }
        if (GUILayout.Button("Convert + Create", GUILayout.Height(28)))
        {
            CreateFolderIfNotExists(sourceFolderPath);
            CreateFolderIfNotExists(targetFolderPath);
            AutoMaterialCreator.ConvertFolderToSprites(sourceFolderPath);
            AutoMaterialCreator.CreateMaterialsFromFolder(sourceFolderPath);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        // 저장/초기화
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("설정 저장", GUILayout.Height(30)))
        {
            SaveSettings();
        }
        if (GUILayout.Button("기본값으로 초기화", GUILayout.Height(30)))
        {
            ResetToDefault();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 현재 설정 표시
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("현재 저장된 설정:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("소스 폴더:", EditorPrefs.GetString("AutoMaterialCreator_SourceFolder", "설정되지 않음"));
        EditorGUILayout.LabelField("타겟 폴더:", EditorPrefs.GetString("AutoMaterialCreator_TargetFolder", "설정되지 않음"));
        EditorGUILayout.LabelField("Prefix:", EditorPrefs.GetString("AutoMaterialCreator_NamePrefix", "없음"));
        EditorGUILayout.LabelField("Convert to Sprite:", EditorPrefs.GetBool("AutoMaterialCreator_ConvertToSprite", false).ToString());
        EditorGUILayout.LabelField("Auto-create on import:", EditorPrefs.GetBool("AutoMaterialCreator_AutoCreateOnImport", false).ToString());
    }

    private void CreateFolders()
    {
        CreateFolderIfNotExists(sourceFolderPath);
        CreateFolderIfNotExists(targetFolderPath);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "폴더가 생성되었습니다.", "확인");
        Repaint();
    }

    private void CreateFolderIfNotExists(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets"))
        {
            Debug.LogError("유효하지 않은 경로입니다: " + path);
            return;
        }
        
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }

    private void SaveSettings()
    {
        if (!sourceFolderPath.StartsWith("Assets") || !targetFolderPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("오류", 
                "경로는 'Assets/'로 시작해야 합니다.", 
                "확인");
            return;
        }
        
        EditorPrefs.SetString("AutoMaterialCreator_SourceFolder", sourceFolderPath);
        EditorPrefs.SetString("AutoMaterialCreator_TargetFolder", targetFolderPath);
        EditorPrefs.SetString("AutoMaterialCreator_NamePrefix", namePrefix);
        EditorPrefs.SetBool("AutoMaterialCreator_ConvertToSprite", convertToSprite);
        EditorPrefs.SetBool("AutoMaterialCreator_AutoCreateOnImport", autoCreateOnImport);
        EditorUtility.DisplayDialog("저장 완료", 
            "설정이 저장되었습니다.", 
            "확인");
        Repaint();
    }

    private void ResetToDefault()
    {
        sourceFolderPath = "Assets/Textures/Source";
        targetFolderPath = "Assets/Materials/Generated";
        namePrefix = "";
        convertToSprite = false;
        autoCreateOnImport = false;
        SaveSettings();
        EditorUtility.DisplayDialog("초기화 완료", "기본 설정으로 초기화되었습니다.", "확인");
    }
}