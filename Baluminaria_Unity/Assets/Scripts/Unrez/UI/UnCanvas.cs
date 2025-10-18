using System.Threading.Tasks;
using UnityEngine;

namespace Unrez
{
    public static class UnCanvas
    {
        //public static CancellationToken TokenToggleCanvas = new CancellationToken();
        private static bool _break;

        public static void ToggleCanvasGroup(CanvasGroup canvasGroup, bool show = false, float time = 0.2f)
        {
            _break = true;
            ToggleCanvasGroupCoroutine(canvasGroup, show, time);
        }

        private static async void ToggleCanvasGroupCoroutine(CanvasGroup canvasGroup, bool show = false, float time = 0.2f)
        {
            canvasGroup.alpha = show ? 0 : 1;
            canvasGroup.interactable = false;
            canvasGroup.gameObject.SetActive(true);
            float t = 0;
            _break = false;
            await Task.Delay(1);
            while (t < 1)
            {
                if (_break)
                {
                    break;
                }
                t += Time.deltaTime / time;
                canvasGroup.alpha = show ? t : 1 - t;
                await Task.Delay(1);
            }
            canvasGroup.alpha = show ? 1 : 0;
            canvasGroup.interactable = show ? true : false;
            canvasGroup.gameObject.SetActive(show);
        }
    }
}