using UnityEngine;

public class GameTileContent : MonoBehaviour
{
    public void Clear()
    {
        Destroy(this.gameObject);
    }
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void SetForward(Vector3 forward)
    {
        transform.forward = forward;
    }
    public void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
}