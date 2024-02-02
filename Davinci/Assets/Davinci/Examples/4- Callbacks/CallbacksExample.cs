using UnityEngine;
using UnityEngine.UI;
using DavinciCore;

public class CallbacksExample : MonoBehaviour
{
    public Image image;
    public string imageUrl;
    public Text statusTxt;

    public Texture2D loadingSpr, errorSpr;

    private void Start()
    {
        //Use with... to add callbacks
        Davinci.Get()
            .Load(imageUrl)
            .Into(image)
            .WithStartAction(() =>
            {
                statusTxt.text = "Download has been started.";
            })
            .WithDownloadProgressChangedAction((progress) =>
            {
                statusTxt.text = "Download progress: " + progress;
            })
            .WithDownloadedAction(() =>
            {
                statusTxt.text = "Download has been completed.";
            })
            .WithLoadedAction(() =>
            {
                statusTxt.text = "Image has been loaded.";
            })
            .WithErrorAction((error) =>
            {
                statusTxt.text = "Got error : " + error;
            })
            .WithEndAction(() =>
            {
                print("Operation has been finished.");
            })
            .SetLoadingPlaceholder(loadingSpr)
            .SetErrorPlaceholder(errorSpr)
            .SetFadeTime(0.8f)
            .SetCached(false)
            .StartLoad();
    }
}