using UnityEngine;

[RequireComponent(typeof(Camera))]
public class UnityCam : MonoBehaviour
{
    private readonly TextureWrapper _wrapper = new();
    private readonly Object textureLock = new();
    private Texture2D buffer;
    
    void Start()
    {
        gameObject.AddComponent<UnityCamPostRenderer>();
    }
    
    public void RenderImage(RenderTexture source, RenderTexture destination)
    {
      _wrapper.ConvertTexture(source);
   
        SendTexture(_wrapper.WrappedTexture);
        Graphics.Blit(source, destination);
    }

    void OnDestroy()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject obj = transform.GetChild(i).gameObject;
            DestroyImmediate(obj);
        }
    }

    private void SendTexture(Texture2D texture2D)
    {
        lock (textureLock)
        {
            buffer = texture2D;
        }
    }

    public Texture2D GetJPG()
    {
        lock (textureLock)
        {
            return buffer;
        }
    }
}