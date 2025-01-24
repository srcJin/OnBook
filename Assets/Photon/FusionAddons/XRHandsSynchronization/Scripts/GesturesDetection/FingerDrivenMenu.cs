using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;


namespace Fusion.Addons.XRHandsSync
{
    public interface IMenuManager
    {
        public void RequiredByHand(HardwareHand hand, bool isRequired);
        public void PoseChangeRequestedByHand(HardwareRig rig, HardwareHand hand, Pose pose, bool forceMove);
        public Transform MenuObjectTransform { get; }
        public bool ShouldDisplayActivationSpinner { get; }
    }

    public class FingerDrivenMenu : FingerDrivenGesture
    {
        float wasActiveStart = -1;
        bool wasActive = false;
        [SerializeField]
        float maxPalmAngleForMenu = 45;
        [SerializeField]
        float poseDurationBeforeMenuActivation = 0.5f;
        [SerializeField]
        Vector3 menuOffsetToPalm = new Vector3(-0.16f, -0.04f, 0.0f);
        [SerializeField]
        GameObject menuObject;
        IMenuManager menuManager;

        [SerializeField] GameObject spinner;
        [SerializeField] Image canvasSpinner;

        protected override void Awake()
        {
            base.Awake();
            if (menuObject)
            {
                menuManager = menuObject.GetComponentInChildren<IMenuManager>();
            }
            else
            {
                Debug.LogError("A menuObject is required. Disabling FingerDrivenMenu");
                enabled = false;
            }
            if (spinner != null)
                canvasSpinner = spinner.GetComponentInChildren<Image>();


        }

        private void OnEnable()
        {
            if (spinner != null)
                spinner.SetActive(false);
            else
                Debug.LogError("spinner not set");
        }

        #region FingerDrivenGesture
        protected override void OnActivateInputControl()
        {
        }

        protected override void OnDesactivateInputcontrol()
        {
            if (wasActive)
            {
                RequireMenu(false);
            }
            wasActiveStart = -1;
            wasActive = false;
            RequireSpinner(false);
        }

        protected override void UpdatedHands(XRHand hand)
        {
            bool actionDetected = false;

            var indexProximalAvailable = TryGetBonePose(hand, XRHandJointID.IndexProximal, out _, out var indexProximalWorldPose);
            var palmAvailable = TryGetBonePose(hand, XRHandJointID.Palm, out _, out var palmWorldPose);

            // Index
            if (palmAvailable && indexProximalAvailable)
            {
                if (wasActive == false)
                {
                    // To start the action, we need to actually look at the hand
                    var angle = Vector3.Angle(palmWorldPose.rotation * Vector3.up, hardwareRig.headset.transform.forward);
                    if (angle < maxPalmAngleForMenu)
                    {
                        // The hand also needs to look in the direction of the head, no matter where the user looks
                        angle = Vector3.Angle(palmWorldPose.rotation * Vector3.up, palmWorldPose.position - hardwareRig.headset.transform.position);
                        if (angle < maxPalmAngleForMenu)
                        {
                            actionDetected = true;
                        }
                    }
                }
                else
                {
                    // To keep the action active, the hand just needs to keep being in the direction of the head, no matter where the user looks
                    var angle = Vector3.Angle(palmWorldPose.rotation * Vector3.up, palmWorldPose.position - hardwareRig.headset.transform.position);
                    if (angle < maxPalmAngleForMenu)
                    {
                        actionDetected = true;
                    }
                }

                if (actionDetected)
                {
                    // TODO Check that hand is opened
                }
            }
            bool isMenuActive = false;
            bool requireSpinner = false;
            if (actionDetected)
            {
                if (poseDurationBeforeMenuActivation == 0)
                {
                    isMenuActive = true;
                }
                else if (wasActiveStart != -1)
                {
                    if ((Time.time - wasActiveStart) > poseDurationBeforeMenuActivation)
                    {
                        isMenuActive = true;
                    }
                }

                // manage waiting 
                if (actionDetected && isMenuActive == false)
                    requireSpinner = true;
            }
            RequireSpinner(requireSpinner);

            if (isMenuActive)
            {
                RequireMenu(true);
                Pose menuPose;
                menuPose.position = (palmWorldPose.position + indexProximalWorldPose.position) / 2f;

                menuPose.position += menuOffsetToPalm.x * (palmWorldPose.rotation * Vector3.left);
                menuPose.position += menuOffsetToPalm.y * (palmWorldPose.rotation * Vector3.up);
                menuPose.position += menuOffsetToPalm.z * (palmWorldPose.rotation * Vector3.forward);

                var menuTransform = menuManager != null ? menuManager.MenuObjectTransform : menuObject.transform;
                // Look at the user
                menuPose.rotation = Quaternion.LookRotation(hardwareRig.headset.transform.position - menuTransform.position);

                if (menuManager != null)
                {
                    menuManager.PoseChangeRequestedByHand(hardwareRig, hardwareHand, menuPose, forceMove: wasActive == false);
                }
                else
                {
                    menuTransform.position = menuPose.position;
                    menuTransform.rotation = menuPose.rotation;
                }
            }
            else
            {
                RequireMenu(false);
            }
            wasActive = isMenuActive;

            if (actionDetected)
            {
                if (wasActiveStart == -1)
                {
                    wasActiveStart = Time.time;
                }
            }
            else
            {
                wasActiveStart = -1;
            }
        }
        #endregion

        void RequireMenu(bool isRequired)
        {
            if (menuManager != null)
            {
                menuManager.RequiredByHand(hardwareHand, isRequired);
            }
            else
            {
                menuObject.SetActive(isRequired);
            }
        }

        float spinnerStart = -1;
        float fillamout = -1;
        float elapsedTime = -1;
        void RequireSpinner(bool isRequired)
        {
            if (spinner)
            {
                bool menuRequireSpinner = menuManager == null || menuManager.ShouldDisplayActivationSpinner;
                if (isRequired && menuRequireSpinner)
                {
                    if (spinnerStart == -1)
                    {
                        spinnerStart = Time.time;
                        spinner.SetActive(true);
                    }
                    else
                    {
                        elapsedTime = Time.time - spinnerStart;
                        if (elapsedTime > poseDurationBeforeMenuActivation)
                            elapsedTime = 0f;
                        fillamout = elapsedTime / poseDurationBeforeMenuActivation;

                        canvasSpinner.fillAmount = fillamout;
                        spinner.transform.LookAt(hardwareRig.headset.transform.position);
                    }
                }
                else if (isRequired == false && spinner.activeSelf == true)
                {
                    canvasSpinner.fillAmount=0;
                    spinnerStart = -1;
                    elapsedTime = -1;
                    spinner.SetActive(false);
                }
                    
            }
        }
    }
}
