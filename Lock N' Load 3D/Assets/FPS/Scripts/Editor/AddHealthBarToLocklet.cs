using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.FPS.UI;
using Unity.FPS.Game;

public class AddHealthBarToLocklet : EditorWindow
{
    [MenuItem("Tools/Add Health Bar to Locklet")]
    static void AddHealthBar()
    {
        // load the yellowlocklet prefab
        string prefabPath = "Assets/ModAssets/Prefabs/LockletPrefabs/YellowLocklet.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError("Could not find YellowLocklet prefab at: " + prefabPath);
            return;
        }

        // create a prefab instance to modify
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        // check if worldspacehealthbar already exists
        if (instance.GetComponent<WorldspaceHealthBar>() != null)
        {
            Debug.Log("WorldspaceHealthBar already exists on YellowLocklet");
            DestroyImmediate(instance);
            return;
        }

        // create healthbarpivot object
        GameObject healthBarPivot = new GameObject("HealthBarPivot");
        healthBarPivot.transform.SetParent(instance.transform);
        healthBarPivot.transform.localPosition = new Vector3(0, 4f, 0); // position above locklet
        healthBarPivot.transform.localRotation = Quaternion.identity;
        healthBarPivot.transform.localScale = Vector3.one;

        // create canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.layer = LayerMask.NameToLayer("UI");
        canvasObj.transform.SetParent(healthBarPivot.transform);
        canvasObj.transform.localPosition = new Vector3(0, 0, -0.123f);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one;

        // add canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "UI";

        // add canvasscaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1;

        // add graphicraycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        // set recttransform
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 0.1f);

        // create background image
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.layer = LayerMask.NameToLayer("UI");
        backgroundObj.transform.SetParent(canvasObj.transform);
        backgroundObj.transform.localPosition = Vector3.zero;
        backgroundObj.transform.localRotation = Quaternion.identity;
        backgroundObj.transform.localScale = Vector3.one;

        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // create healthbar image
        GameObject healthBarImageObj = new GameObject("HealthBarImage");
        healthBarImageObj.layer = LayerMask.NameToLayer("UI");
        healthBarImageObj.transform.SetParent(canvasObj.transform);
        healthBarImageObj.transform.localPosition = Vector3.zero;
        healthBarImageObj.transform.localRotation = Quaternion.identity;
        healthBarImageObj.transform.localScale = Vector3.one;

        RectTransform healthBarRect = healthBarImageObj.AddComponent<RectTransform>();
        healthBarRect.anchorMin = Vector2.zero;
        healthBarRect.anchorMax = Vector2.one;
        healthBarRect.sizeDelta = Vector2.zero;
        healthBarRect.anchoredPosition = Vector2.zero;

        Image healthBarImage = healthBarImageObj.AddComponent<Image>();
        healthBarImage.color = new Color(0f, 1f, 0f, 1f); // green health bar
        healthBarImage.type = Image.Type.Filled;
        healthBarImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        // add worldspacehealthbar component to root
        WorldspaceHealthBar healthBarComponent = instance.AddComponent<WorldspaceHealthBar>();
        
        // get health component
        Health health = instance.GetComponent<Health>();
        
        // set references using reflection since the fields might be private
        var healthField = typeof(WorldspaceHealthBar).GetField("Health", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var imageField = typeof(WorldspaceHealthBar).GetField("HealthBarImage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var pivotField = typeof(WorldspaceHealthBar).GetField("HealthBarPivot", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var hideFullField = typeof(WorldspaceHealthBar).GetField("HideFullHealthBar", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (healthField != null) healthField.SetValue(healthBarComponent, health);
        if (imageField != null) imageField.SetValue(healthBarComponent, healthBarImage);
        if (pivotField != null) pivotField.SetValue(healthBarComponent, healthBarPivot.transform);
        if (hideFullField != null) hideFullField.SetValue(healthBarComponent, true);

        // apply changes to prefab
        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        
        // clean up
        DestroyImmediate(instance);
        
        Debug.Log("Successfully added health bar to YellowLocklet prefab!");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
