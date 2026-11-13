using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class IntroKitchenBuilder
{
    [MenuItem("Tools/Freezer Game/Create Playable Kitchen Title Scene")]
    public static void Build()
    {
        var path=EditorUtility.SaveFilePanelInProject("Create playable title scene","KitchenTitle","unity","Choose a new scene filename.");
        if(string.IsNullOrEmpty(path))return;
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)){EditorUtility.DisplayDialog("Choose a new scene","Use an unused scene filename.","OK");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cameraObject=new GameObject("Kitchen camera",typeof(Camera),typeof(AudioListener));
        cameraObject.tag="MainCamera";
        var title=new GameObject("Playable title").AddComponent<PlayableTitle>();
        title.kitchenCamera=cameraObject.GetComponent<Camera>();
        title.content=ScriptableObject.CreateInstance<IntroContent>();
        AssetDatabase.CreateAsset(title.content,AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.ChangeExtension(path,null)+" Content.asset"));
        EditorSceneManager.SaveScene(scene,path);Selection.activeObject=title.content;
    }
}
