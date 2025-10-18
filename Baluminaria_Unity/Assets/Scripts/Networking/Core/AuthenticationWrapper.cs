using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;


namespace Unrez.Networking
{
    public enum AuthState
    {
        NotAuthenticated,
        Authenticating,
        Authenticated,
        ErrorAuthenticating,
        ErrorRequesting,
        Timeout
    }

    public static class AuthenticationWrapper
    {
        public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

        public static async Task<AuthState> DoAuth(int retries = 5)
        {
            if (AuthState == AuthState.Authenticated)
            {
                return AuthState;
            }
            if (AuthState == AuthState.Authenticating)
            {
                Debug.LogWarning("Already authenticating!");
                AuthState = await Authenticating();
                return AuthState;
            }
            await SignInAnonymouslyAsync(retries);
            return AuthState;
        }

        private static async Task<AuthState> Authenticating()
        {
            while (AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated)
            {
                await Task.Delay(200);
            }
            return AuthState;
        }


        private static async Task SignInAnonymouslyAsync(int retries = 5)
        {
            AuthState = AuthState.Authenticating;
            try
            {
                int tries = 0;
                while (AuthState == AuthState.Authenticating)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        AuthState = AuthState.Authenticated;
                        break;
                    }
                    else
                    {
                        if (tries > retries)
                        {
                            Debug.Log($"<color=red>TIMEOUT.Player can not connect after {retries} retries. </color>");

                            AuthState = AuthState.Timeout;
                            break;
                        }
                        await Task.Delay(1000);
                        tries++;
                    }
                }
            }
            catch (AuthenticationException authException)
            {
                Debug.LogError($"<color=red>Can NOT authenticate. ERROR:{authException} </color>");
                AuthState = AuthState.ErrorAuthenticating;
            }
            catch (RequestFailedException requestException)
            {
                Debug.LogError($"<color=red>Can NOT request (internet issues). ERROR:{requestException} </color>");
                AuthState = AuthState.ErrorAuthenticating;
            }
        }
    }
}