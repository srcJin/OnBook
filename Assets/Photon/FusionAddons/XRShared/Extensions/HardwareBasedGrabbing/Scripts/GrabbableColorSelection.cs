using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fusion.XR.Shared.Grabbing
{
    /***
     * 
     *  GrabbableColorSelection is in charged to sync the color modification of a grabbable.
     *  CheckColorModification() method is called during FUN to check if the local user used the button to change the color.
     *  In this case, ChangeColor() updates the networked variable CurrentColor. So, OnColorChanged() is called on all players.
     *  Then, ApplyColorChange() updates the object. It can be overrided in subclasses
     *  
     *  The start color will be the first of the colorList. This can be overriden in DefaultColor() method.
     *          
     ***/
    public class GrabbableColorSelection : NetworkBehaviour, IColorProvider
    {
        [Networked, OnChangedRender(nameof(OnCurrentColorChange))]
        public Color CurrentColor { get; set; }
        private Color previousColor = Color.clear;
        public List<Color> colorList = new List<Color> {
            new Color(10f/255f, 10f/255f, 10f/255f, 1),
            new Color(255f/255f, 100f/255f, 100f/255f, 1),
            new Color(121f/255f, 255f/255f, 86f/255f, 1),
            new Color(45f/255f, 156f/255f, 255f/255f, 1),
            new Color(255f/255f, 246f/255f, 76f/255f, 1)
        };

        protected int colorIndex = 0;
        [SerializeField] private float changeColorCoolDown = 1f;
        private float lastColorChangedTime = 0f;

        public bool useInput = true;

        public NetworkGrabbable grabbable;
        public InputActionProperty leftControllerChangeColorAction;
        public InputActionProperty rightControllerChangeColorAction;
        public InputActionProperty ChangeColorAction => grabbable != null && grabbable.IsGrabbed && grabbable.CurrentGrabber.hand && grabbable.CurrentGrabber.hand.side == RigPart.LeftController ? leftControllerChangeColorAction : rightControllerChangeColorAction;

        public bool IsGrabbed => grabbable.IsGrabbed;
        public bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable.CurrentGrabber.Object.StateAuthority == Runner.LocalPlayer;

        public float changeColorInputThreshold = 0.5f;

        [SerializeField] bool automaticallyFillEmptyRenderersList = true;
        [SerializeField] protected List<Renderer> coloredRenderers = new List<Renderer>();

        [Header("Feedback")]
        IFeedbackHandler feedback;
        [SerializeField] string audioType;
        [SerializeField] FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic;

        protected virtual void Awake()
        {
            grabbable = GetComponent<NetworkGrabbable>();
            feedback = GetComponent<IFeedbackHandler>();
            BindColorChangeActions();
            FillColoredRenderers();
        }

        protected virtual void FillColoredRenderers()
        {
            if (automaticallyFillEmptyRenderersList && coloredRenderers.Count == 0)
            {
                coloredRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
            }
        }

        protected virtual void BindColorChangeActions() { 

            var controllersBindings = new List<string> { "joystick" };
            var keyboardBindings = new List<string> { "composite||2DVector||Up||<Keyboard>/C" };

            leftControllerChangeColorAction.EnableWithDefaultXRBindings(bindings: keyboardBindings, leftBindings: controllersBindings);
            rightControllerChangeColorAction.EnableWithDefaultXRBindings(rightBindings: controllersBindings);
        }

        protected Color DefaultColor()
        {
            return colorList[0];
        }

        public override void Spawned()
        {
            base.Spawned();
            // Set the default color
            if (Object.HasStateAuthority)
            {
                CurrentColor = DefaultColor();
            }
            OnColorChanged();
        }

        protected virtual void OnCurrentColorChange()
        {
            OnColorChanged();
        }

        // Update the color when the network var has been changed
        protected virtual void OnColorChanged(bool forceChange = false)
        {
            if (CurrentColor == previousColor) return;
            // Update the color
            previousColor = CurrentColor;

            // Update color index
            colorIndex = -1;
            for(int i = 0; i < colorList.Count; i++)
            {
                if(colorList[i] == CurrentColor)
                {
                    colorIndex = i;
                    break;
                }
            }

            ApplyColorChange(CurrentColor);
        }

        protected virtual void ApplyColorChange(Color color)
        {
            foreach (var r in coloredRenderers) r.material.color = color;
        }


        // Update the networked StickyNoteColor & local StickyNote color
        private void ChangeColor()
        {
            // check color index
            if (colorIndex >= colorList.Count)
                colorIndex = 0;
            if (colorIndex < 0)
                colorIndex = colorList.Count - 1;

            // change the networked color to inform remote players and update the local StickyNote color
            CurrentColor = colorList[colorIndex];
        }

        public void ChangeColor(int index)
        {
            if (index >= colorList.Count || index < 0)
                Debug.LogError("Index color out of range");
            else
            {
                colorIndex = index;
                ChangeColor();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object == null || Object.HasStateAuthority == false) return;

            CheckColorModification();
        }

        private void CheckColorModification()
        {
            // Check if the the local player press the color modification button
            if (IsGrabbedByLocalPLayer && (lastColorChangedTime + changeColorCoolDown < Time.time))
            {
                var stick = ChangeColorAction.action.ReadValue<Vector2>().y;
                if (Mathf.Abs(stick) > changeColorInputThreshold)
                {
                    // button has been used, change the color index
                    lastColorChangedTime = Time.time;

                    if (stick < 0)
                        colorIndex--;
                    else
                        colorIndex++;

                    // Apply color update
                    ChangeColor();

                    // Audio & Haptic feedback
                    if (feedback != null) 
                        feedback.PlayAudioAndHapticFeeback(audioType: audioType, audioOverwrite: true, feedbackMode: feedbackMode);
                }
            }
        }
    }
}
