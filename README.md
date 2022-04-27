# LinkedinLogin-Unity
OAuth2 Linkedin Sign in for Unity

1. Please setup your Linkedin app at https://developer.linkedin.com.

2. Download LinkedinLogin-Unity and take a look at LinkedInLoginMainScene for reference.

3. Use Client ID, Client Secret and redirect URL in prefab LinkedinLoginControl.

![image](https://user-images.githubusercontent.com/12982381/165641439-a19bfad6-97dc-47f7-978f-75f310e05bc0.png)

4. Currently Scope is set to r_liteprofile%20r_emailaddress which can be changed as needed. More info at https://docs.microsoft.com/en-us/linkedin/shared/authentication/authentication

5. Unity-webview is ### required. Big thanks to https://github.com/gree/unity-webview!. Please include Unity-webview package from https://github.com/gree/unity-webview.

## Gotcha! You will see an error when you open the project. Ignore this error and once the project is open simply import https://github.com/gree/unity-webview.

Supported Platforms: Windows, Mac, iOS, Android etc.

MIT License
