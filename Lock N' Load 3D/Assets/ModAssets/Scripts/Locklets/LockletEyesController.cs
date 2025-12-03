using UnityEngine;

public class LockletEyesController : MonoBehaviour
{
    [SerializeField] private Transform[] eyeBones;
    [SerializeField] private float eyeDistanceFromFace = 0.25f;
    [SerializeField] private float eyeRange = 0.3f;
    [SerializeField] private Renderer[] eyeRenderers;
    [SerializeField] private Vector2Int eyeSpriteDimensions;
    [SerializeField] private int eyeSpriteColumns = 3;

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        if (mpb == null)
            mpb = new MaterialPropertyBlock();
    }

    public void LookAt(Vector3 worldPosition)
    {
        if (eyeBones != null)
        {
            foreach (var eyeBone in eyeBones)
            {
                eyeBone.localPosition = Vector3.zero;

                var localLookAtDirection = eyeBone.InverseTransformPoint(worldPosition).normalized;

                eyeBone.localPosition = new Vector3(
                    x: localLookAtDirection.x * eyeRange,
                    y: eyeDistanceFromFace,
                    z: localLookAtDirection.z * eyeRange);
            }
        }
    }

    public void SetEyes(int eyesIdx)
    {
        if (mpb == null)
            mpb = new MaterialPropertyBlock();
        
        Debug.Log($"LockletEyesController.SetEyes({eyesIdx}): eyeRenderers array has {(eyeRenderers != null ? eyeRenderers.Length : 0)} elements");
            
        if (eyeRenderers != null)
        {
            for (int i = 0; i < eyeRenderers.Length; i++)
            {
                var eyeRenderer = eyeRenderers[i];
                
                if (eyeRenderer == null)
                {
                    Debug.LogWarning($"LockletEyesController.SetEyes({eyesIdx}): eyeRenderers[{i}] is NULL!");
                    continue;
                }
                    
                var tex = eyeRenderer.sharedMaterial != null ? eyeRenderer.sharedMaterial.mainTexture : null;
                if (tex == null)
                {
                    Debug.LogWarning($"LockletEyesController.SetEyes({eyesIdx}): eyeRenderers[{i}] ({eyeRenderer.gameObject.name}) has no texture!");
                    continue;
                }
                    
                var cellWidth = (float)(eyeSpriteDimensions.x + 1) / tex.width;
                var cellHeight = (float)(eyeSpriteDimensions.y + 1) / tex.height;

                var col = eyesIdx % eyeSpriteColumns;
                var row = eyesIdx / eyeSpriteColumns;

                eyeRenderer.GetPropertyBlock(mpb);
                mpb.SetVector("_BaseMap_ST", new Vector4(1f, 1f, col * cellWidth, row * -cellHeight));
                eyeRenderer.SetPropertyBlock(mpb);
                
                Debug.Log($"LockletEyesController.SetEyes({eyesIdx}): Set UV offset to ({col * cellWidth}, {row * -cellHeight}) on {eyeRenderer.gameObject.name}");
            }
        }
    }
}
