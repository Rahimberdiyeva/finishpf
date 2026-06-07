using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using UnityEngine.Networking;

public class PatternForgeImporter : EditorWindow
{
    [MenuItem("Assets/Import PatternForge PBR")]
    static void ShowWindow()
    {
        GetWindow<PatternForgeImporter>("Import PatternForge PBR");
    }

    private string zipPath = "";

    void OnGUI()
    {
        GUILayout.Label("Select PatternForge ZIP archive", EditorStyles.boldLabel);
        if (GUILayout.Button("Browse..."))
        {
            zipPath = EditorUtility.OpenFilePanel("Select PBR ZIP", "", "zip");
        }
        if (!string.IsNullOrEmpty(zipPath))
            EditorGUILayout.HelpBox(zipPath, MessageType.Info);

        if (GUILayout.Button("Import Textures") && !string.IsNullOrEmpty(zipPath))
        {
            Import(zipPath);
        }
    }

    void Import(string zipPath)
    {
        string extractPath = Path.Combine(Application.temporaryCachePath, "PatternForgeExtract");
        if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
        Directory.CreateDirectory(extractPath);
        ZipFile.ExtractToDirectory(zipPath, extractPath);

        string[] files = Directory.GetFiles(extractPath);
        string baseColor = null, normal = null, roughness = null, metallic = null, height = null, ao = null;

        foreach (string f in files)
        {
            string name = Path.GetFileNameWithoutExtension(f).ToLower();
            if (name.Contains("basecolor") || name.Contains("albedo")) baseColor = f;
            else if (name.Contains("normal")) normal = f;
            else if (name.Contains("roughness")) roughness = f;
            else if (name.Contains("metallic")) metallic = f;
            else if (name.Contains("height")) height = f;
            else if (name.Contains("ao")) ao = f;
        }

        if (baseColor == null)
        {
            EditorUtility.DisplayDialog("Error", "No base color map found", "OK");
            return;
        }

        // Копируем текстуры в проект
        string targetFolder = "Assets/PatternForgeTextures";
        if (!AssetDatabase.IsValidFolder(targetFolder))
            AssetDatabase.CreateFolder("Assets", "PatternForgeTextures");

        void ImportTexture(string src, string suffix)
        {
            if (src == null) return;
            string dest = Path.Combine(targetFolder, Path.GetFileName(src));
            File.Copy(src, dest, true);
            AssetDatabase.ImportAsset(dest);
            TextureImporter ti = AssetImporter.GetAtPath(dest) as TextureImporter;
            if (ti != null)
            {
                if (suffix == "Normal") ti.textureType = TextureImporterType.NormalMap;
                else ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = (suffix != "Normal" && suffix != "Roughness" && suffix != "Metallic" && suffix != "Height" && suffix != "AO");
                ti.SaveAndReimport();
            }
        }

        ImportTexture(baseColor, "BaseColor");
        ImportTexture(normal, "Normal");
        ImportTexture(roughness, "Roughness");
        ImportTexture(metallic, "Metallic");
        ImportTexture(height, "Height");
        ImportTexture(ao, "AO");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "Textures imported to " + targetFolder, "OK");

        Directory.Delete(extractPath, true);
    }
}
