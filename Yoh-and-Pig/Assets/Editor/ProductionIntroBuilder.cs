using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProductionIntroBuilder
{
    [MenuItem("Tools/Freezer Game/Create Production Intro Scene")]
    static void Create()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create opening scene","ProductionIntro","unity","Choose a new scene file.");
        if (string.IsNullOrEmpty(path)) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
        {
            EditorUtility.DisplayDialog("Choose a new file","Select an unused filename for the opening scene.","OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Black camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=0;
        var intro=new GameObject("Production intro").AddComponent<ProductionIntro>();
        EditorSceneManager.SaveScene(scene,path);Selection.activeGameObject=intro.gameObject;
    }
}
