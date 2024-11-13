using System;
using _Project.InputSystem;
using _Project.UI;
using _Project.UI.MainMenu;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class LobbyManager : GameManager
    {
        public void StartGame()
        {
            SceneManager.LoadSceneAsync("InGame");
        }
    }
}