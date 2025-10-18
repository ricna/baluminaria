
using UnityEngine;

namespace Unrez
{
    public static class Unbug
    {
        public static string Username = "ricna";

        public static string Log(string message, Uncolor color = null, bool showUser = false, string userName = "")
        {
            if (userName == "")
            {
                userName = Username;
            }
            if (color == null)
            {
                color = Uncolor.White;
            }
            string strShowUser = showUser ? "<color = black >\n[{ userName}]</color>" : "";
            string log = $"<color=#{color.colorHex}> {message} </color> " + strShowUser;
            if (userName == Username)
            {
                Debug.Log(log);
            }
            return log;
        }
    }
}