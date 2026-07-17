using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System;

namespace EasyUI.Modals
{
    public class Modal
    {
        public Sprite title;
        public string message = "Your speed was <color=#E19344><b>0 WPM</b></color> with <color=#E19344><b>0%</b></color> accuracy!";
        public UnityAction onPlayAgain;
        public UnityAction onMainMenu;
    }

    public class ModalUI : MonoBehaviour
    {
        [SerializeField] GameObject canvas;
        [SerializeField] Image titleUI;
        [SerializeField] TextMeshProUGUI messageUI;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button mainMenuButton;

        Modal modal = new Modal();

        // Singleton pattern
        public static ModalUI instance;

        private void Awake()
        {
            instance = this;

            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(PlayAgain);

            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(MainMenu);
        }

        // Set Modal's title
        public ModalUI SetTitle(Sprite title)
        {
            modal.title = title;

            return instance;
        }

        // Set Modal's message
        public ModalUI SetMessage(string message)
        {
            modal.message = message;

            return instance;
        }

        // Set action if player click on "Play Again"
        public ModalUI OnPlayAgain(UnityAction action)
        {
            modal.onPlayAgain = action;

            return instance;
        }

        // Set action if player click on "Main Menu"
        public ModalUI OnMainMenu(UnityAction action)
        {
            modal.onMainMenu = action;

            return instance;
        }

        public void ShowModal()
        {
            titleUI.sprite = modal.title;
            messageUI.text = modal.message;

            canvas.SetActive(true);
            FindObjectOfType<Tweening>().PopUp(canvas, 0.5f);
            FindObjectOfType<Tweening>().TitlePulsate(titleUI);
        }

        private void MainMenu()
        {
            canvas.SetActive(false);
            modal.onMainMenu.Invoke();

            // Reset modal
            modal = new Modal();
        }

        private void PlayAgain()
        {
            canvas.SetActive(false);
            modal.onPlayAgain.Invoke();

            // Reset modal
            modal = new Modal();
        }
    }
}

