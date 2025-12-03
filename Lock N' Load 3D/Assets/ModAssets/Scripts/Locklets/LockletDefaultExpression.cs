using UnityEngine;

/// <summary>
/// Sets the default eye expression for a locklet on spawn.
/// Expression is determined by the locklet's name.
/// </summary>
public class LockletDefaultExpression : MonoBehaviour
{
    void Start()
    {
        // Try immediately and with delays to ensure it applies
        StartCoroutine(ApplyExpressionWithRetry());
    }
    
    System.Collections.IEnumerator ApplyExpressionWithRetry()
    {
        LockletEyesController eyesController = GetComponentInChildren<LockletEyesController>();
        if (eyesController == null)
        {
            Debug.LogError("LockletDefaultExpression: No LockletEyesController found!");
            yield break;
        }
        
        int expression = 0; // Default
        
        // Check parent name if this is on a child object
        string lockletName = (transform.parent != null ? transform.parent.name : gameObject.name).ToLower();
        Debug.Log($"LockletDefaultExpression: Full hierarchy = {GetFullPath()}, checking name = '{lockletName}'");
        
        if (lockletName.Contains("red"))
            expression = 3;
        else if (lockletName.Contains("pink"))
            expression = 4;
        else if (lockletName.Contains("yellow"))
            expression = 0;
        
        Debug.Log($"LockletDefaultExpression: Setting expression {expression} for {lockletName}");
        
        // Apply multiple times to ensure it sticks
        eyesController.SetEyes(expression);
        yield return new WaitForSeconds(0.1f);
        eyesController.SetEyes(expression);
        yield return new WaitForSeconds(0.5f);
        eyesController.SetEyes(expression);
        
        Debug.Log($"LockletDefaultExpression: Applied expression {expression} three times");
    }
    
    string GetFullPath()
    {
        string path = gameObject.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
