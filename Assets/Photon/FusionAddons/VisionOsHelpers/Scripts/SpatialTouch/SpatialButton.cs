using Fusion.XR.Shared;
using Fusion.XR.Shared.Touch;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.VisionOsHelpers
{
    /**
    * 
    *  SpatialButton is an enhanced version of SpatialTouchable class
    *  It can be used as a press, toggle or radio button.
    *  It provides visual & audio feedback when the button is touched
    *  
    **/
    public class SpatialButton : SpatialTouchable
    {
        [Header("Current state")]
        public bool isButtonPressed = false;
        protected MeshRenderer meshRenderer;

        public enum ButtonType
        {
            PressButton,
            RadioButton,
            ToggleButton
        }

        [Header("Button")]
        public ButtonType buttonType = ButtonType.PressButton;
        public bool toggleStatus = false;

        [SerializeField]
        List<SpatialButton> radioGroupButtons = new List<SpatialButton>();

        public bool isRadioGroupDefaultButton = false;

        [Header("Anti-bounce")]
        public float timeBetweenTouchTrigger = 0.3f;

        [Header("Feedback")]
        protected Material materialAtStart;
        [SerializeField] IFeedbackHandler feedback;
        [SerializeField] string audioType;
        [SerializeField] protected Material touchMaterial;
        [SerializeField] private bool playSoundWhenTouched = true;
        [SerializeField] private bool playHapticFeedbackOnToucher = true;
        [SerializeField] float toucherHapticAmplitude = 0.8f;
        [SerializeField] float toucherHapticDuration = 0.2f;

        [Tooltip("Set this to true if a toucher is also used, to avoid double callback triggering")]
        public bool ignoreSpatialContact = false;

        [Header("Sibling button")]
        [SerializeField]
        bool doNotallowTouchIfSiblingTouched = true;
        [SerializeField]
        bool doNotallowTouchIfSiblingWasRecentlyTouched = true;
        [SerializeField]
        bool automaticallyDetectSiblings = true;
        [SerializeField]
        List<SpatialButton> siblingButtons = new List<SpatialButton>();


        float lastTouchEnd = -1;

        public bool WasRecentlyTouched => lastTouchEnd != -1 && (Time.time - lastTouchEnd) < timeBetweenTouchTrigger;
        public bool IsToggleButton => buttonType == ButtonType.ToggleButton;
        public bool IsRadioButton => buttonType == ButtonType.RadioButton;



        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer) materialAtStart = meshRenderer.material;

            if (feedback == null)
                feedback = GetComponentInParent<IFeedbackHandler>();
        }

        private void OnEnable()
        {
            // We need to clear if component was disabled 
            isButtonPressed = false;
            UpdateButton();
        }

        private void Start()
        {
            if (automaticallyDetectSiblings && transform.parent)
            {
                foreach (Transform child in transform.parent)
                {
                    if (child == transform) continue;
                    if (child.TryGetComponent<SpatialButton>(out var sibling))
                    {
                        siblingButtons.Add(sibling);
                    }
                }
            }


            if (IsRadioButton && radioGroupButtons.Count == 0)
            {
                foreach (var siblingButton in siblingButtons)
                {
                    if (siblingButton.IsRadioButton)
                    {
                        radioGroupButtons.Add(siblingButton);
                    }
                }
            }
        }



        bool CheckIfTouchIsAllowed()
        {
            if (WasRecentlyTouched)
            {
                // Local anti-bounce 
                return false;
            }
            if (doNotallowTouchIfSiblingTouched)
            {
                foreach (var sibling in siblingButtons)
                {
                    if (sibling.isButtonPressed)
                    {
                      //  Debug.LogError("Preventing due to active " + sibling);
                        return false;
                    }
                    else if (doNotallowTouchIfSiblingWasRecentlyTouched && sibling.WasRecentlyTouched)
                    {
                        // Sibling anti-bounce 
                     //   Debug.LogError("Preventing due to recently active" + sibling);
                        return false;

                    }
                }
            }
            return true;
        }

        public void ChangeButtonStatus(bool status)
        {
            if(IsToggleButton || IsRadioButton)
            {
                toggleStatus = status;
                if (toggleStatus)
                { 
                    base.OnTouchStart();
                }
            }
            else
            {
                if (isButtonPressed)
                      {
                         base.OnTouchStart();
                      }
            }
            UpdateButton();
        }

        void ChangeRadioButtonsStatus()
        {
            ChangeButtonStatus(true);
            foreach (var button in radioGroupButtons)
            {
                button.ChangeButtonStatus(false);
            }
        }

        [ContextMenu("OnTouchStart")]
        public override void OnTouchStart()
        {
            TouchStart(null);
        }

        public override void OnTouchStart(Toucher toucher)
        {
            TouchStart(toucher);
        }

        private void TouchStart(Toucher toucher)
        {
            if (CheckIfTouchIsAllowed() == false) return;
            isButtonPressed = true;

            if (IsToggleButton)
            {
                ChangeButtonStatus(!toggleStatus);
            }
            else if (IsRadioButton)
            {
                ChangeRadioButtonsStatus(); 
            }
            else
            {
                ChangeButtonStatus(true);
            }

            if (playSoundWhenTouched && feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
                feedback.PlayAudioFeeback(audioType);

            if (playHapticFeedbackOnToucher && toucher != null)
            {
                var feedbackHandler = toucher.gameObject.GetComponentInParent<IFeedbackHandler>();
 
                if (feedbackHandler != null)
                {
                    feedbackHandler.PlayHapticFeedback(hapticAmplitude:toucherHapticAmplitude, hapticDuration:toucherHapticDuration);
                }
            }
        }

        private void TouchEnd(Toucher toucher)
        {
            var buttonWasActive = isButtonPressed;
            isButtonPressed = false;


            if (buttonWasActive)
            {
                base.OnTouchEnd();
                lastTouchEnd = Time.time;
                if (buttonType == ButtonType.PressButton)
                {
                    ChangeButtonStatus(false);
                }
            }

            UpdateButton();
        }

        [ContextMenu("OnTouchEnd")]
        public override void OnTouchEnd()
        {
            TouchEnd(null);
        }

        public override void OnTouchEnd(Toucher toucher)
        {
            TouchEnd(toucher);
        }


        public virtual void UpdateButton()
        {
            if (!meshRenderer) return;

            bool boutonActivated = isButtonPressed || toggleStatus;

            if (touchMaterial && boutonActivated)
            {
                meshRenderer.material = touchMaterial;
            }
            else if (materialAtStart && boutonActivated == false)
            {
                RestoreMaterial();
            }
        }

        protected async void RestoreMaterial()
        {
            await System.Threading.Tasks.Task.Delay(100);
            if(meshRenderer) meshRenderer.material = materialAtStart;
        }
    }
}
