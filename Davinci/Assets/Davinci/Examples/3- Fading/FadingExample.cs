using UnityEngine;
using UnityEngine.UI;
using DavinciCore;

public class FadingExample : MonoBehaviour
{
    public Image image_1;
    public Image image_2;
    public Image image_3;

    public Texture2D loadingSpr, errorSpr;

    public string url;

    private void Start()
    {
        //Use setFadeTime to set fading animation time. Set 0 for disable fading.

        Davinci.Get()
            .Load(url)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .Into(image_1)
            .SetFadeTime(2)
            .StartLoad();

        Davinci.Get()
            .Load(url)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .Into(image_2)
            .SetFadeTime(5)
            .StartLoad();

        Davinci.Get()
            .Load(url)
            .SetFadeTime(0)//disable fading
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .Into(image_3)
            .StartLoad();
    }
}