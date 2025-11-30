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
        if (eyeRenderers != null)
        {
            foreach (var eyeRenderer in eyeRenderers)
            {
                var tex = eyeRenderer.material.mainTexture;
                var cellWidth = (float)(eyeSpriteDimensions.x + 1) / tex.width;
                var cellHeight = (float)(eyeSpriteDimensions.y + 1) / tex.height;

                var col = eyesIdx % eyeSpriteColumns;
                var row = eyesIdx / eyeSpriteColumns;

                eyeRenderer.GetPropertyBlock(mpb);
                mpb.SetVector("_BaseMap_ST", new Vector4(1f, 1f, col * cellWidth, row * -cellHeight));
                eyeRenderer.SetPropertyBlock(mpb);
            }
        }
    }
}
