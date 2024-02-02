using UnityEngine;
using UnityEngine.UI;
using DavinciCore;

public class TextureExamlpe : MonoBehaviour
{
    public Renderer cube1, cube2;
    public string imgUrl1, imgUrl2;

    public Texture2D loadingSpr, errorSpr;

    private void Start()
    {
        Davinci.Get()
            .Load(imgUrl1)
            .Into(cube1)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .SetFadeTime(2f)
            .SetCached(false)
            .StartLoad();

        Davinci.Get()
            .Load(imgUrl2)
            .Into(cube2)
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .SetFadeTime(0f)
            .SetCached(false)
            .StartLoad();
    }
}