using UnityEngine;

public class GameTileAsset : MonoBehaviour
{
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void SetForward(Vector3 forward)
    {
        transform.forward = forward;
    }
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }
    public void Clear()
    {
        Destroy(this.gameObject);
    }
}