using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.UI;
using UnityEngine.Events;


public class WebviewController : MonoBehaviour
{
    private string Url;
    //public Text status;
    private WebViewObject webViewObject;

    public static UnityAction<string> OnUrlCallback;

    public void OpenUrl(string url)
    {
        Url = url;
        StartCoroutine(OpenUrlCorr());
    }

    public bool IsWebViewVisible()
    {
        return webViewObject.GetVisibility();
    }

    public void CloseWebview()
    {
        if (webViewObject != null)
        {
            Logger.Log("CloseWebView");
            ClearCache(true);
            ClearCookies();
            webViewObject.StopAllCoroutines();
            webViewObject.SetVisibility(false);
        }
            
    }

    public void ClearCache(bool includeFilesOnDisk)
    {
        if (webViewObject != null)
            webViewObject.ClearCache(includeFilesOnDisk);    
    }

    public void ClearCookies()
    {
        if (webViewObject != null)
            webViewObject.ClearCookies();
    }

    IEnumerator OpenUrlCorr()
    {
        if (webViewObject==null)
            webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();


        webViewObject.SetVisibility(false);

        webViewObject.Init(
            cb: (msg) =>
            {
                Logger.Log(string.Format("CallFromJS[{0}]", msg));
                //status.text = msg;
                //status.GetComponent<Animation>().Play();
            },
            err: (msg) =>
            {
                Logger.Log(string.Format("CallOnError[{0}]", msg));
                //status.text = msg;
                //status.GetComponent<Animation>().Play();
            },
            httpErr: (msg) =>
            {
                Logger.Log(string.Format("CallOnHttpError[{0}]", msg));
                //status.text = msg;
                //status.GetComponent<Animation>().Play();
            },
            started: (msg) =>
            {
                Logger.Log(string.Format("CallOnStarted[{0}]", msg));
                OnUrlCallback?.Invoke(msg);
            },
            hooked: (msg) =>
            {
                Logger.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            ld: (msg) =>
            {
                Logger.Log(string.Format("CallOnLoaded[{0}]", msg));
#if UNITY_EDITOR_OSX || (!UNITY_ANDROID && !UNITY_WEBPLAYER && !UNITY_WEBGL)
                // NOTE: depending on the situation, you might prefer
                // the 'iframe' approach.
                // cf. https://github.com/gree/unity-webview/issues/189
#if true
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        window.location = 'unity:' + msg;
                      }
                    }
                  }
                ");
#else
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        var iframe = document.createElement('IFRAME');
                        iframe.setAttribute('src', 'unity:' + msg);
                        document.documentElement.appendChild(iframe);
                        iframe.parentNode.removeChild(iframe);
                        iframe = null;
                      }
                    }
                  }
                ");
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                webViewObject.EvaluateJS(
                    "window.Unity = {" +
                    "   call:function(msg) {" +
                    "       parent.unityWebView.sendMessage('WebViewObject', msg)" +
                    "   }" +
                    "};");
#endif
                webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
            },
            //transparent: false,
            //zoom: true,
            //ua: "custom user agent string",
            //// android
            androidForceDarkMode: 2,  // 0: follow system setting, 1: force dark off, 2: force dark on
            //// ios
            enableWKWebView: true,
            wkContentMode: 1  // 0: recommended, 1: mobile, 2: desktop
            //wkAllowsLinkPreview: true,
            //// editor
            //separated: false
            );
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
#endif
        // cf. https://github.com/gree/unity-webview/pull/512
        // Added alertDialogEnabled flag to enable/disable alert/confirm/prompt dialogs. by KojiNakamaru · Pull Request #512 · gree/unity-webview
        //webViewObject.SetAlertDialogEnabled(false);

        // cf. https://github.com/gree/unity-webview/pull/728
        //webViewObject.SetCameraAccess(true);
        //webViewObject.SetMicrophoneAccess(true);

        // cf. https://github.com/gree/unity-webview/pull/550
        // introduced SetURLPattern(..., hookPattern). by KojiNakamaru · Pull Request #550 · gree/unity-webview
        //webViewObject.SetURLPattern("", "^https://.*youtube.com", "^https://.*google.com");

        // cf. https://github.com/gree/unity-webview/pull/570
        // Add BASIC authentication feature (Android and iOS with WKWebView only) by takeh1k0 · Pull Request #570 · gree/unity-webview
        //webViewObject.SetBasicAuthInfo("id", "password");

        //webViewObject.SetScrollbarsVisibility(true);

        webViewObject.SetMargins(5, 100, 5, Screen.height / 4);
        webViewObject.SetTextZoom(100);  // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410
        webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (Url.StartsWith("http"))
        {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        }
        else
        {
            var exts = new string[]{
                ".jpg",
                ".js",
                ".html"  // should be last
            };
            foreach (var ext in exts)
            {
                var url = Url.Replace(".html", ext);
                var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
                var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
                byte[] result = null;
                if (src.Contains("://"))
                {  // for Android
#if UNITY_2018_4_OR_NEWER
                    // NOTE: a more complete code that utilizes UnityWebRequest can be found in https://github.com/gree/unity-webview/commit/2a07e82f760a8495aa3a77a23453f384869caba7#diff-4379160fa4c2a287f414c07eb10ee36d
                    var unityWebRequest = UnityWebRequest.Get(src);
                    yield return unityWebRequest.SendWebRequest();
                    result = unityWebRequest.downloadHandler.data;
#else
                    var www = new WWW(src);
                    yield return www;
                    result = www.bytes;
#endif
                }
                else
                {
                    result = System.IO.File.ReadAllBytes(src);
                }
                System.IO.File.WriteAllBytes(dst, result);
                if (ext == ".html")
                {
                    webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
                    break;
                }
            }
        }
#else
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
        }
#endif
        yield break;
    }
    /*
   void OnGUI()
   {

       if (webViewObject)
       {
           var x = 10;

           GUI.enabled = webViewObject.CanGoBack();
           if (GUI.Button(new Rect(x, 10, 80, 80), "<"))
           {
               webViewObject.GoBack();
           }
           GUI.enabled = true;
           x += 90;

           GUI.enabled = webViewObject.CanGoForward();
           if (GUI.Button(new Rect(x, 10, 80, 80), ">"))
           {
               webViewObject.GoForward();
           }
           GUI.enabled = true;
           x += 90;

           if (GUI.Button(new Rect(x, 10, 80, 80), "r"))
           {
               webViewObject.Reload();
           }
           x += 90;

           GUI.TextField(new Rect(x, 10, 180, 80), "" + webViewObject.Progress());
           x += 190;

           if (GUI.Button(new Rect(x, 10, 80, 80), "*"))
           {
               var g = GameObject.Find("WebViewObject");
               if (g != null)
               {
                   Destroy(g);
               }
               else
               {
                   StartCoroutine(OpenUrlCorr());
               }
           }
           x += 90;

           if (GUI.Button(new Rect(x, 10, 80, 80), "c"))
           {
               Logger.Log(webViewObject.GetCookies(Url));
           }
           x += 90;

           if (GUI.Button(new Rect(x, 10, 80, 80), "x"))
           {
               webViewObject.ClearCookies();
           }
           x += 90;
       }
    }*/
}
