using UnityEngine.SceneManagement;

namespace _Project
{
    public class LobbyManager : GameManager
    {
        public static void StartGame()
        {
            SceneManager.LoadSceneAsync("InGame");
        }
    }
}