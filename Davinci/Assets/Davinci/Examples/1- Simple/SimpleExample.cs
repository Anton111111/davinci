using UnityEngine;
using UnityEngine.UI;
using DavinciCore;

public class SimpleExample : MonoBehaviour
{
    public Image image;
    public string imageUrl;

    private void Start()
    {
        //Simple usage - Single line of code and ready to go!
        Davinci.Get().Load(imageUrl).Into(image).StartLoad();
    }
}