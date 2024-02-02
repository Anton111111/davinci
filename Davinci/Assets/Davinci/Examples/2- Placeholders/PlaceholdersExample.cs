using UnityEngine;
using UnityEngine.UI;
using DavinciCore;

public class PlaceholdersExample : MonoBehaviour
{
    public Image image_1;
    public Image image_2;

    public Texture2D loadingSpr, errorSpr;

    public string correctUrl;
    public string wrongUrl;

    private void Start()
    {
        //use setLoadingSprite and setError sprite to set placeholders

        Davinci.Get()
            .Load(correctUrl)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .SetCached(false)
            .Into(image_1)
            .StartLoad();

        Davinci.Get()
            .Load(wrongUrl)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .SetCached(false)
            .Into(image_2)
            .StartLoad();
    }
}