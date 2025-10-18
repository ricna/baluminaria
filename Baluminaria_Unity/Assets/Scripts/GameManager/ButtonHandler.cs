using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unrez.BackyardShowdown
{

    public enum ButtonSFXType
    {
        None,
        Default,
        Soft,
        Heavy,
    }

    public class ButtonHandler : MonoBehaviour, IPointerClickHandler, ISelectHandler, ISubmitHandler
    {
        [SerializeField]
        private ButtonSFXType _buttonSfxType = ButtonSFXType.Default;

        [Header("Extra Audio Clip")]
        [SerializeField]
        private AudioClip _sfxOnClick = default;

        public Action onClick;
        public void OnSubmit(BaseEventData eventData)
        {
            if (_buttonSfxType != ButtonSFXType.None)
            {
                AudioManager.Instance.PlaySFXButtonClick(_buttonSfxType);
            }

            if (_sfxOnClick != null)
            {
                AudioManager.Instance.PlaySFX(_sfxOnClick);
            }
            onClick?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_buttonSfxType != ButtonSFXType.None)
            {
                AudioManager.Instance.PlaySFXButtonClick(_buttonSfxType);
            }

            if (_sfxOnClick != null)
            {
                AudioManager.Instance.PlaySFX(_sfxOnClick);
            }
            onClick?.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_buttonSfxType != ButtonSFXType.None)
            {
                AudioManager.Instance.PlaySFXButtonSelect(_buttonSfxType);
            }
        }

    }
}