using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#endif

/// <summary>
/// Davinci - A powerful, easy-to-use image downloading and caching library for Unity in Run-Time
/// v 1.2
/// Developed by ShamsDEV.com
/// copyright (c) ShamsDEV.com All Rights Reserved.
/// Licensed under the MIT License.
/// https://github.com/shamsdev/davinci
/// </summary>
///

namespace DavinciCore
{
    public class Davinci : MonoBehaviour
    {
        private static readonly bool _enableGlobalLogs = true;
        private static readonly Dictionary<string, Davinci> _underProcessDavincies = new();
        private static readonly string _filePath =
            Application.persistentDataPath + "/" + "davinci" + "/";
        private static readonly float _maxCacheSizeBytes = 50000000L; //50Mb

        private bool _enableLog = false;
        private float _fadeTime = 1;
        private bool _cached = true;
        private string _authToken;

        private enum RendererType
        {
            none,
            uiImage,
            renderer,
            material,
            sprite
        }

        private RendererType _rendererType = RendererType.none;
        private GameObject _targetObj;
        private Material _targetMaterial;
        private string _url = null;

        private Texture2D _loadingPlaceholder,
            _errorPlaceholder;

        private UnityAction _onStartAction,
            _onDownloadedAction,
            _onLoadedAction,
            _onEndAction;

        private UnityAction<int> _onDownloadProgressChange;
        private UnityAction<string> _onErrorAction;

        private string _uniqueHash;
        private int _progress;

        private float _maxAlpha = 1.0f;

        /// <summary>
        /// Get instance of davinci class
        /// </summary>
        public static Davinci Get()
        {
            return new GameObject("Davinci").AddComponent<Davinci>();
        }

        private void OnDestroy()
        {
            if (
                _uniqueHash != null
                && _underProcessDavincies.ContainsKey(_uniqueHash)
                && _underProcessDavincies[_uniqueHash] == this
            )
            {
                if (_enableLog)
                    Debug.Log(
                        "[Davinci] Removing Davinci object while download unfinished: "
                            + _uniqueHash
                    );

                _underProcessDavincies.Remove(_uniqueHash);
            }
        }

        /// <summary>
        /// Set image url for download.
        /// </summary>
        /// <param name="url">Image Url</param>
        /// <returns></returns>
        public Davinci Load(string url)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Url set : " + url);

            _url = url;
            return this;
        }

        /// <summary>
        /// Set fading animation time.
        /// </summary>
        /// <param name="fadeTime">Fade animation time. Set 0 for disable fading.</param>
        /// <returns></returns>
        public Davinci SetFadeTime(float fadeTime)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Fading time set : " + fadeTime);

            _fadeTime = fadeTime;
            return this;
        }

        /// <summary>
        /// Set target alpha for fade.
        /// </summary>
        /// <param name="maxAlpha">Target alpha for fade. Set 0 for use alpha from target renderer.</param>
        /// <returns></returns>
        public Davinci SetMaxAlpha(float maxAlpha)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Max alpha set : " + maxAlpha);

            _maxAlpha = maxAlpha;
            return this;
        }

        /// <summary>
        /// Set target Image component.
        /// </summary>
        /// <param name="image">target Unity UI image component</param>
        /// <returns></returns>
        public Davinci Into(Image image)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Target as UIImage set : " + image);

            _rendererType = RendererType.uiImage;
            _targetObj = image.gameObject;
            return this;
        }

        /// <summary>
        /// Set target Renderer component.
        /// </summary>
        /// <param name="renderer">target renderer component</param>
        /// <returns></returns>
        public Davinci Into(Renderer renderer)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Target as Renderer set : " + renderer);

            _rendererType = RendererType.renderer;
            _targetObj = renderer.gameObject;
            return this;
        }

        /// <summary>
        /// Set target Renderer component.
        /// </summary>
        /// <param name="material">target material component</param>
        /// <returns></returns>
        public Davinci Into(Material material)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Target as Material set : " + material);

            _rendererType = RendererType.material;
            _targetMaterial = material;
            return this;
        }

        public Davinci Into(SpriteRenderer spriteRenderer)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Target as SpriteRenderer set : " + spriteRenderer);

            _rendererType = RendererType.sprite;
            _targetObj = spriteRenderer.gameObject;
            return this;
        }

        #region Actions
        public Davinci WithStartAction(UnityAction action)
        {
            _onStartAction = action;

            if (_enableLog)
                Debug.Log("[Davinci] On start action set : " + action);

            return this;
        }

        public Davinci WithDownloadedAction(UnityAction action)
        {
            _onDownloadedAction = action;

            if (_enableLog)
                Debug.Log("[Davinci] On downloaded action set : " + action);

            return this;
        }

        public Davinci WithDownloadProgressChangedAction(UnityAction<int> action)
        {
            _onDownloadProgressChange = action;

            if (_enableLog)
                Debug.Log("[Davinci] On download progress changed action set : " + action);

            return this;
        }

        public Davinci WithLoadedAction(UnityAction action)
        {
            _onLoadedAction = action;

            if (_enableLog)
                Debug.Log("[Davinci] On loaded action set : " + action);

            return this;
        }

        public Davinci WithErrorAction(UnityAction<string> action)
        {
            _onErrorAction = action;

            if (_enableLog)
                Debug.Log("[Davinci] On error action set : " + action);

            return this;
        }

        public Davinci WithEndAction(UnityAction action)
        {
            _onEndAction = action;

            if (_enableLog)
                Debug.Log("[Davinci] On end action set : " + action);

            return this;
        }
        #endregion

        /// <summary>
        /// Show or hide logs in console.
        /// </summary>
        /// <param name="enable">'true' for show logs in console.</param>
        /// <returns></returns>
        public Davinci SetEnableLog(bool enableLog)
        {
            _enableLog = enableLog;

            if (enableLog)
                Debug.Log("[Davinci] Logging enabled : " + enableLog);

            return this;
        }

        /// <summary>
        /// Set the sprite of image when davinci is downloading and loading image
        /// </summary>
        /// <param name="loadingPlaceholder">loading texture</param>
        /// <returns></returns>
        public Davinci SetLoadingPlaceholder(Texture2D loadingPlaceholder)
        {
            _loadingPlaceholder = loadingPlaceholder;

            if (_enableLog)
                Debug.Log("[Davinci] Loading placeholder has been set.");

            return this;
        }

        /// <summary>
        /// Set image sprite when some error occurred during downloading or loading image
        /// </summary>
        /// <param name="errorPlaceholder">error texture</param>
        /// <returns></returns>
        public Davinci SetErrorPlaceholder(Texture2D errorPlaceholder)
        {
            _errorPlaceholder = errorPlaceholder;

            if (_enableLog)
                Debug.Log("[Davinci] Error placeholder has been set.");

            return this;
        }

        /// <summary>
        /// Enable cache
        /// </summary>
        /// <returns></returns>
        public Davinci SetCached(bool cached)
        {
            _cached = cached;

            if (_enableLog)
                Debug.Log("[Davinci] Cache enabled : " + cached);

            return this;
        }

        /// <summary>
        /// Set authorization token.
        /// </summary>
        /// <param name="token">authorization token</param>
        /// <returns></returns>
        public Davinci SetAuthToken(string token)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Authorization Token set: " + token);

            _authToken = token;
            return this;
        }

        /// <summary>
        /// Start davinci process.
        /// </summary>
        public Davinci StartLoad()
        {
            if (_url == null)
            {
                Error("Url has not been set. Use 'load' function to set image url.");
                return this;
            }

            try
            {
                Uri uri = new(_url);
                _url = uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                Error($"Url is not correct: {ex.Message}");
                return this;
            }

            if (
                _rendererType == RendererType.none
                || (_targetObj == null && _targetMaterial == null)
            )
            {
                Error("Target has not been set. Use 'into' function to set target component.");
                return this;
            }

            if (_enableLog)
                Debug.Log("[Davinci] Start Working.");

            if (_loadingPlaceholder != null)
                SetLoadingImage();

            _onStartAction?.Invoke();

            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
            }

            _uniqueHash = CreateMD5(_url);

            if (_underProcessDavincies.ContainsKey(_uniqueHash))
            {
                Davinci sameProcess = _underProcessDavincies[_uniqueHash];
                sameProcess._onDownloadedAction += () =>
                {
                    // As this action will be called at a later time by another Davinci instance,
                    // make sure that this instance hasn't been destroyed in the meantime.
                    if (this == null)
                        return;

                    _onDownloadedAction?.Invoke();

                    LoadSpriteToImage();
                };
                sameProcess._onLoadedAction += () =>
                {
                    // As this action will be called at a later time by another Davinci instance,
                    // make sure that this instance hasn't been destroyed in the meantime.
                    if (this == null)
                        return;

                    _onLoadedAction?.Invoke();
                };
                sameProcess._onEndAction += () =>
                {
                    // As this action will be called at a later time by another Davinci instance,
                    // make sure that this instance hasn't been destroyed in the meantime.
                    if (this == null)
                        return;

                    _onEndAction?.Invoke();
                };
                sameProcess._onDownloadProgressChange += (progress) =>
                {
                    // As this action will be called at a later time by another Davinci instance,
                    // make sure that this instance hasn't been destroyed in the meantime.
                    if (this == null)
                        return;

                    _onDownloadProgressChange?.Invoke(progress);
                };
                sameProcess._onErrorAction += (message) =>
                {
                    // As this action will be called at a later time by another Davinci instance,
                    // make sure that this instance hasn't been destroyed in the meantime.
                    if (this == null)
                        return;

                    _onErrorAction?.Invoke(message);
                };
            }
            else
            {
                if (File.Exists(_filePath + _uniqueHash))
                {
                    _onDownloadedAction?.Invoke();

                    LoadSpriteToImage();
                }
                else
                {
                    _underProcessDavincies.Add(_uniqueHash, this);
                    StopAllCoroutines();
                    StartCoroutine(Downloader());
                }
            }
            return this;
        }

        /// <summary>
        /// Stop davinci process.
        /// </summary>
        public void Stop()
        {
            StopAllCoroutines();
            if (_maxAlpha > 0 && _fadeTime > 0 && (_targetObj != null || _targetMaterial != null))
            {
                Color color;
                switch (_rendererType)
                {
                    case RendererType.renderer:
                        Renderer renderer = _targetObj.GetComponent<Renderer>();

                        if (renderer == null || renderer.material == null)
                            break;

                        if (renderer.material.HasProperty("_Color"))
                        {
                            color = renderer.material.color;
                            color.a = _maxAlpha;
                            renderer.material.color = color;
                        }
                        break;

                    case RendererType.material:
                        Material material = _targetMaterial;

                        if (material == null)
                            break;

                        if (material.HasProperty("_Color"))
                        {
                            color = material.color;
                            color.a = _maxAlpha;
                            material.color = color;
                        }
                        break;

                    case RendererType.uiImage:
                        Image image = _targetObj.GetComponent<Image>();

                        if (image == null)
                            break;

                        color = image.color;
                        color.a = _maxAlpha;
                        image.color = color;
                        break;

                    case RendererType.sprite:
                        SpriteRenderer spriteRenderer = _targetObj.GetComponent<SpriteRenderer>();
                        if (spriteRenderer == null)
                            break;

                        color = spriteRenderer.color;
                        color.a = _maxAlpha;
                        spriteRenderer.color = color;
                        break;
                }
            }
        }

        /// <summary>
        /// Stop davinci process and destroy.
        /// </summary>
        public void Dispose()
        {
            Stop();
            Destroyer();
        }

        private IEnumerator Downloader()
        {
            if (_enableLog)
                Debug.Log("[Davinci] Download started.");

#if UNITY_2018_3_OR_NEWER
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(_url);
            if (!string.IsNullOrWhiteSpace(_authToken))
                www.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            yield return www.SendWebRequest();
#else
            var www = new WWW(url);
#endif

            while (!www.isDone)
            {
                if (www.error != null)
                {
                    Error("Error while downloading the image : " + www.error);
                    yield break;
                }

#if UNITY_2018_3_OR_NEWER
                _progress = Mathf.FloorToInt(www.downloadProgress * 100);
#else
                progress = Mathf.FloorToInt(www.progress * 100);
#endif
                _onDownloadProgressChange?.Invoke(_progress);

                if (_enableLog)
                    Debug.Log("[Davinci] Downloading progress : " + _progress + "%");

                yield return null;
            }

#if UNITY_2018_3_OR_NEWER
            if (www.error == null)
                File.WriteAllBytes(_filePath + _uniqueHash, www.downloadHandler.data);
#else
            if (www.error == null)
                File.WriteAllBytes(filePath + uniqueHash, www.bytes);
#endif

            www.Dispose();
            www = null;

            _onDownloadedAction?.Invoke();

            LoadSpriteToImage();

            _underProcessDavincies.Remove(_uniqueHash);
        }

        private void LoadSpriteToImage()
        {
            _progress = 100;
            _onDownloadProgressChange?.Invoke(_progress);

            if (_enableLog)
                Debug.Log("[Davinci] Downloading progress : " + _progress + "%");

            if (!File.Exists(_filePath + _uniqueHash))
            {
                Error("Loading image file has been failed.");
                return;
            }

            StopAllCoroutines();
            StartCoroutine(ImageLoader());
        }

        private void SetLoadingImage()
        {
            switch (_rendererType)
            {
                case RendererType.renderer:
                    Renderer renderer = _targetObj.GetComponent<Renderer>();
                    renderer.material.mainTexture = _loadingPlaceholder;
                    break;

                case RendererType.material:
                    Material material = _targetMaterial;
                    material.mainTexture = _loadingPlaceholder;
                    break;

                case RendererType.uiImage:
                    Image image = _targetObj.GetComponent<Image>();
                    Sprite sprite = Sprite.Create(
                        _loadingPlaceholder,
                        new Rect(0, 0, _loadingPlaceholder.width, _loadingPlaceholder.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    image.sprite = sprite;

                    break;

                case RendererType.sprite:
                    SpriteRenderer spriteRenderer = _targetObj.GetComponent<SpriteRenderer>();
                    Sprite spriteImage = Sprite.Create(
                        _loadingPlaceholder,
                        new Rect(0, 0, _loadingPlaceholder.width, _loadingPlaceholder.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    spriteRenderer.sprite = spriteImage;
                    break;
            }
        }

        private IEnumerator ImageLoader(Texture2D texture = null)
        {
            if (_enableLog)
                Debug.Log("[Davinci] Start loading image.");

            if (texture == null)
            {
                byte[] fileData;
                fileData = File.ReadAllBytes(_filePath + _uniqueHash);
                texture = new Texture2D(2, 2) { filterMode = FilterMode.Trilinear, anisoLevel = 2 };

                texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }

            if (_targetObj != null || _targetMaterial != null)
            {
                Color color;
                float maxAlpha;
                switch (_rendererType)
                {
                    case RendererType.renderer:
                        Renderer renderer = _targetObj.GetComponent<Renderer>();

                        if (renderer == null || renderer.material == null)
                            break;

                        renderer.material.mainTexture = texture;

                        if (_fadeTime > 0 && renderer.material.HasProperty("_Color"))
                        {
                            color = renderer.material.color;
                            maxAlpha = _maxAlpha > 0 ? _maxAlpha : color.a;

                            color.a = 0;

                            renderer.material.color = color;
                            float time = Time.time;
                            while (color.a < maxAlpha)
                            {
                                color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / _fadeTime);

                                if (renderer != null)
                                    renderer.material.color = color;

                                yield return null;
                            }
                        }

                        break;

                    case RendererType.material:
                        Material material = _targetMaterial;

                        if (material == null)
                            break;

                        material.mainTexture = texture;

                        if (material.HasProperty("_Color"))
                        {
                            material.SetColor("_BaseColor", Color.white);
                        }

                        if (_fadeTime > 0 && material.HasProperty("_Color"))
                        {
                            color = material.color;
                            maxAlpha = _maxAlpha > 0 ? _maxAlpha : color.a;

                            color.a = 0;

                            material.color = color;
                            float time = Time.time;
                            while (color.a < maxAlpha)
                            {
                                color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / _fadeTime);

                                if (material != null)
                                    material.color = color;

                                yield return null;
                            }
                        }

                        break;

                    case RendererType.uiImage:
                        Image image = _targetObj.GetComponent<Image>();

                        if (image == null)
                            break;

                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        image.sprite = sprite;
                        color = image.color;
                        maxAlpha = _maxAlpha > 0 ? _maxAlpha : color.a;

                        if (_fadeTime > 0)
                        {
                            color.a = 0;
                            image.color = color;

                            float time = Time.time;
                            while (color.a < maxAlpha)
                            {
                                color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / _fadeTime);

                                if (image != null)
                                    image.color = color;
                                yield return null;
                            }
                        }
                        break;

                    case RendererType.sprite:
                        SpriteRenderer spriteRenderer = _targetObj.GetComponent<SpriteRenderer>();

                        if (spriteRenderer == null)
                            break;

                        Sprite spriteImage = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        spriteRenderer.sprite = spriteImage;
                        color = spriteRenderer.color;
                        maxAlpha = _maxAlpha > 0 ? _maxAlpha : color.a;

                        if (_fadeTime > 0)
                        {
                            color.a = 0;
                            spriteRenderer.color = color;

                            float time = Time.time;
                            while (color.a < maxAlpha)
                            {
                                color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / _fadeTime);

                                if (spriteRenderer != null)
                                    spriteRenderer.color = color;
                                yield return null;
                            }
                        }
                        break;
                }
            }

            _onLoadedAction?.Invoke();

            if (_enableLog)
                Debug.Log("[Davinci] Image has been loaded.");

            Finish();
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private void Error(string message)
        {
            if (_enableLog)
                Debug.LogError("[Davinci] Error : " + message);

            _onErrorAction?.Invoke(message);

            if (_errorPlaceholder != null)
                StartCoroutine(ImageLoader(_errorPlaceholder));
            else
                Finish();
        }

        private void Finish()
        {
            if (_enableLog)
                Debug.Log("[Davinci] Operation has been finished.");

            if (!_cached)
            {
                try
                {
                    File.Delete(_filePath + _uniqueHash);
                }
                catch (Exception ex)
                {
                    if (_enableLog)
                        Debug.LogError($"[Davinci] Error while removing cached file: {ex.Message}");
                }
            }

            _onEndAction?.Invoke();

            Invoke(nameof(Destroyer), 0.5f);
        }

        private void Destroyer()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Clear a certain cached file with its url
        /// </summary>
        /// <param name="url">Cached file url.</param>
        /// <returns></returns>
        public static void ClearCache(string url)
        {
            try
            {
                File.Delete(_filePath + CreateMD5(url));

                if (_enableGlobalLogs)
                    Debug.Log($"[Davinci] Cached file has been cleared: {url}");
            }
            catch (Exception ex)
            {
                if (_enableGlobalLogs)
                    Debug.LogError($"[Davinci] Error while removing cached file: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear davinci cached files more than cache limit
        /// </summary>
        /// <returns></returns>
        public static void ClearCache()
        {
            var cacheDir = new DirectoryInfo(_filePath);
            if (cacheDir.Exists)
            {
                var files = cacheDir.GetFiles().OrderByDescending(it => it.CreationTimeUtc);
                var cacheSizeBytes = 0L;
                foreach (var file in files)
                {
                    if ((cacheSizeBytes + file.Length) >= _maxCacheSizeBytes)
                    {
                        file.Delete();
                        continue;
                    }

                    cacheSizeBytes += file.Length;
                }
            }
        }

        /// <summary>
        /// Clear all davinci cached files
        /// </summary>
        /// <returns></returns>
        public static void ClearAllCachedFiles()
        {
            try
            {
                Directory.Delete(_filePath, true);

                if (_enableGlobalLogs)
                    Debug.Log("[Davinci] All Davinci cached files has been cleared.");
            }
            catch (Exception ex)
            {
                if (_enableGlobalLogs)
                    Debug.LogError($"[Davinci] Error while removing cached file: {ex.Message}");
            }
        }
    }
}
