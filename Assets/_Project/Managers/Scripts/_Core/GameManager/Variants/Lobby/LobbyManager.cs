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