﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ff.vr.interaction
{
    /* Switch to virtual keyboard mode if one of the controllers 
    comes too close to to PunchKeyboard.
    */
    public class KeyboardEnabler : MonoBehaviour
    {
        public bool IsVisible = false;
        public KeyboardControllerStick[] _sticks;


        [Header("--- internal prefab references -----")]
        public GameObject PunchKeyboardObject;
        public InputField _inputField;
        public GameObject _inputFieldCanvas;

        public event Action InputCompleted;

        public event Action<string> InputChanged;

        void Start()
        {
            UpdateKeyboardVisibility(forceUpdate: true);


            var controllerManager = FindObjectOfType(typeof(SteamVR_ControllerManager)) as SteamVR_ControllerManager;
            _controllers = controllerManager.GetComponentsInChildren<InteractiveController>(true);
            foreach (var controller in controllerManager.GetComponentsInChildren<SteamVR_TrackedController>(true))
            {
                Debug.Log("register handler to controller");
                controller.TriggerClicked += new ClickedEventHandler
                (
                    delegate (object o, ClickedEventArgs a)
                    {
                        if (IsVisible)
                            InputCompleted();
                        // _keyboardEnabler.Hide();
                    }
                );
            }
        }

        public void Show()
        {
            foreach (var c in _controllers)
                c.SetLaserPointerEnabled(false);

            IsVisible = true;
            UpdateKeyboardVisibility();
            PositionInFrontOfCamera();

            _inputField.text = "annotation";

            // see http://answers.unity3d.com/questions/1159573/
            _inputField.Select();
            _inputField.OnSelect(null);
        }


        public void Hide()
        {
            foreach (var c in _controllers)
                c.SetLaserPointerEnabled(true);

            IsVisible = false;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Return))
            {
                if (InputCompleted != null)
                {
                    InputCompleted();
                }
            }
            else if (Input.anyKey)
            {
                if (InputChanged != null)
                {
                    InputChanged(_inputField.text);
                }
            }
            UpdateKeyboardVisibility();
        }

        private void UpdateKeyboardVisibility(bool forceUpdate = false)
        {
            if (IsVisible != _wasEnabled || forceUpdate)
            {
                _wasEnabled = IsVisible;
                PunchKeyboardObject.SetActive(IsVisible);
                _inputFieldCanvas.SetActive(IsVisible);

                foreach (var stick in _sticks)
                {
                    stick.gameObject.SetActive(IsVisible);
                }

                // if (IsVisible)
                // {
                //     // Focus KeyInput
                //     EventSystem.current.SetSelectedGameObject(_inputField.gameObject, null);
                // }
                // else
                // {

                // }
            }
        }

        private void PositionInFrontOfCamera()
        {
            var forward = Camera.main.transform.forward * 0.5f;
            forward.y = 0;
            var pos = Camera.main.transform.position + forward + Vector3.down * 0.5f;

            this.transform.position = pos;
            var ea = Camera.main.transform.eulerAngles;
            ea.x = 0;
            ea.z = 0;
            transform.eulerAngles = ea;
        }

        private bool _wasEnabled = false;
        private InteractiveController[] _controllers;
    }
}