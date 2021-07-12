using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindObjectOfType(typeof(T));
                if (instance == null)
                {
                    var singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";
                }
            }
            return instance;
        }
    }
    public virtual void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (instance == null)
        {
            instance = this as T;
        }
    }
    public static void Destory()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

}