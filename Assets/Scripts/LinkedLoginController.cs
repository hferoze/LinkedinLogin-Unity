using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(WebviewController))]
public class LinkedLoginController : MonoBehaviour
{
    [Header("Linkedin Settings")]
    [SerializeField] private string CLIENT_ID;
    [SerializeField] private string CLIENT_SECRET;
    [SerializeField] private string REDIRECT_URI;
    [SerializeField] private string SCOPE = "r_liteprofile%20r_emailaddress";

    [Header ("UI")]
    [SerializeField] private Button m_LinkedinLoginBtn;
    [SerializeField] private Button m_LinkedinLogoutBtn;
    [SerializeField] private GameObject m_UserData;
    [SerializeField] private Text m_UserName;
    [SerializeField] private Text m_UserEmail;
    [SerializeField] private Image m_UserImage;

    private static string AUTHURL = "https://www.linkedin.com/oauth/v2/authorization";
    private static string TOKENURL = "https://www.linkedin.com/oauth/v2/accessToken";
    private static string REVOKEURL = "https://www.linkedin.com/oauth/v2/revoke";
    private static string AUTHCANCELURL = "https://www.linkedin.com/oauth/v2/authorization-cancel";
    private static string LOGINCANCELURL = "https://www.linkedin.com/oauth/v2/login-cancel";
    private static string USER_PROFILE_URL = "https://api.linkedin.com/v2/me?projection=(id,firstName,lastName,profilePicture(displayImage~:playableStreams))&oauth2_access_token=";
    private static string USER_EMAIL_URL = "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))&oauth2_access_token=";

    private string mCurrentAccessToken;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnUrlCallback(string msg)
    {
        if (GetComponent<WebviewController>().IsWebViewVisible())
        {
            if (msg.StartsWith(REDIRECT_URI))
            {
                if (msg.Contains("?code="))
                {
                    int idx1 = msg.IndexOf("?code=");
                    int length = msg.Length - idx1 - "?code=".Length;
                    string authCode = msg.Substring(idx1 + "?code=".Length, length);

                    CloseWebview();

                    GetAccessToken(authCode);

                }
            }
            else if (msg.StartsWith(AUTHCANCELURL) || msg.StartsWith(LOGINCANCELURL))
            {
                CloseWebview();
            }
        }
        else
        {
            Logger.LogWarning("Webview is not visible");
        }
    }

 
    private void GetAuth()
    {
        StartCoroutine(HandleGetAuth());
    }

    private void GetAccessToken(string authCode)
    {
        StartCoroutine(HandleGetAccessToken(authCode));
    }

    private void GetUserProfile(string access_token)
    {
        StartCoroutine(HandleGetUserProfile(access_token));
    }

    private IEnumerator HandleGetUserProfile(string access_token)
    {
        m_LinkedinLoginBtn.gameObject.SetActive(false);
        m_LinkedinLogoutBtn.gameObject.SetActive(true);
        m_UserData.SetActive(true);

        yield return null;

        string userProfileUrl = USER_PROFILE_URL + access_token;

        UnityWebRequest www = UnityWebRequest.Get(userProfileUrl);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.ConnectionError)
        {
            string result = www.downloadHandler.text;
            result = result.Replace("displayImage~", "displayImageArr");

            //Logger.Log(result);
            UserProfile userProfile = JsonUtility.FromJson<UserProfile>(result);

            m_UserName.text = userProfile.firstName.localized.en_US + " " + userProfile.lastName.localized.en_US;
            StartCoroutine(DownloadImage(userProfile.profilePicture.displayImageArr.elements[2].identifiers[0].identifier));

            Logger.Log("first name " + userProfile.firstName.localized.en_US + " last name " + userProfile.lastName.localized.en_US + " id " + userProfile.id);
            Logger.Log("profile pic " + userProfile.profilePicture.displayImageArr.elements[2].identifiers[0].identifier);

            StartCoroutine(HandleGetUserEmail(access_token));
        }
        else
        {
            Logger.Log("GetUserProfileCorr Failed");
            //handle failure
        }
    }

    private IEnumerator DownloadImage(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError)
            Debug.Log(www.error);
        else
        {
            Sprite sprite = Sprite.Create(((DownloadHandlerTexture)www.downloadHandler).texture, new Rect(0, 0, 400, 400), Vector2.zero);
            yield return null;
            m_UserImage.sprite = sprite;
        }
    }


    private IEnumerator HandleGetUserEmail(string access_token)
    {
        yield return null;

        string userEmailUrl = USER_EMAIL_URL + access_token;

        UnityWebRequest www = UnityWebRequest.Get(userEmailUrl);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.ConnectionError)
        {
            string result = www.downloadHandler.text;
            result = result.Replace("handle~", "handleTilde");

            //Logger.Log(result);

            UserEmail userEmail = JsonUtility.FromJson<UserEmail>(result);
            m_UserEmail.text = userEmail.elements[0].handleTilde.emailAddress;
            Logger.Log("Email " + userEmail.elements[0].handleTilde.emailAddress );
        }
        else
        {
            Logger.Log("GetUserEmailCorr Failed");
            //handle failure
        }
    }

    private IEnumerator HandleGetAuth()
    {
        yield return null;

        string authUrl = AUTHURL + "?response_type=code" + "&client_id=" + CLIENT_ID + "&redirect_uri=" + REDIRECT_URI + "&scope=" + SCOPE;

      
        GetComponent<WebviewController>().OpenUrl(authUrl);
    }

    private IEnumerator HandleGetAccessToken(string authCode)
    {
        yield return null;

        Dictionary<string, string> content = new Dictionary<string, string>();
        content.Add("grant_type", "authorization_code");
        content.Add("code", authCode);
        content.Add("redirect_uri", REDIRECT_URI);
        content.Add("client_id", CLIENT_ID);
        content.Add("client_secret", CLIENT_SECRET);

        UnityWebRequest www = UnityWebRequest.Post(TOKENURL, content);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.ConnectionError)
        {
            string resultContent = www.downloadHandler.text;

            AccessToken accessTokenJson = JsonUtility.FromJson<AccessToken>(resultContent);

            if (accessTokenJson.access_token != null && accessTokenJson.access_token.Trim().Length > 0)
            {
                Logger.Log("access token " + accessTokenJson.access_token);
                Logger.Log("expires in: " + accessTokenJson.expires_in);

                mCurrentAccessToken = accessTokenJson.access_token;

                GetUserProfile(accessTokenJson.access_token);
            }
            else
            {
                Logger.LogError("empty access token");
            }
        }
        else
        {
            Logger.Log("GetAccessTokenCorr Failed");
            //handle failure
        }
    }

    private IEnumerator HandleLogout()
    {
        yield return null;

        Dictionary<string, string> content = new Dictionary<string, string>();
        content.Add("client_id", CLIENT_ID);
        content.Add("client_secret", CLIENT_SECRET);
        content.Add("token", mCurrentAccessToken);

        UnityWebRequest www = UnityWebRequest.Post(REVOKEURL, content);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.ConnectionError)
        {
            Logger.Log(www.downloadHandler.text);

            m_LinkedinLoginBtn.gameObject.SetActive(true);
            m_LinkedinLogoutBtn.gameObject.SetActive(false);
            m_UserData.SetActive(false);
            m_UserEmail.text = "";
            m_UserName.text = "";
            m_UserImage.sprite = null;
            mCurrentAccessToken = "";

            GetComponent<WebviewController>().ClearCache(true);
            GetComponent<WebviewController>().ClearCookies();
        }
        else
        {
            Logger.Log("HandleLogout Failed");
        }
    }

    private void LogOut()
    {
        StartCoroutine(HandleLogout());
    }

    public void OnLinkedInLoginBtnClick()
    {
        GetAuth();
    }

    public void OnLinkedInLogoutBtnClick()
    {
        LogOut();
    }

    private void CloseWebview()
    {
        GetComponent<WebviewController>().CloseWebview();
    }

    private void Subscribe()
    {
        WebviewController.OnUrlCallback += OnUrlCallback;
    }

    private void Unsubscribe()
    {
        WebviewController.OnUrlCallback -= OnUrlCallback;

    }
}

[Serializable]
public struct AccessToken
{
    public string access_token;
    public string expires_in;
}

//User Profile
[Serializable]
public struct UserProfile
{
    public string id;
    public Name firstName;
    public Name lastName;
    public ProfilePicture profilePicture;
}

[Serializable]
public struct Name
{
    public Localized localized;
    public PreferredLocale preferredLocale;
}

[Serializable]
public struct Localized
{
    public string en_US;
}

[Serializable]
public struct PreferredLocale
{
    public string country;
    public string language;
}

[Serializable]
public struct ProfilePicture
{ 
    public DisplayImage displayImageArr;
}

[Serializable]
public struct DisplayImage
{
    public Elements [] elements;
}

[Serializable]
public struct Elements
{
    public Identifiers[] identifiers;
}

[Serializable]
public struct Identifiers
{
    public string identifier;
}

//User email
[Serializable]
public struct UserEmail
{
    public UserEmailElements[] elements;
}

[Serializable]
public struct UserEmailElements
{
    public HandleTilde handleTilde;
    public string handle;
}


[Serializable]
public struct HandleTilde
{
    public string emailAddress;
}
