using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// This is the game over screen class
public class GameOverScreen : MonoBehaviour
{
    // Store the text that we should show on-screen
    public Text GameOverText;
    // This function gets a text to show, sets the game over screen's text and displays it on the screen
    public void Setup(string Text)
    {
        GameOverText.text = Text;
        gameObject.SetActive(true);
    }
    // This function gets executed when the restart button is pressed, and it restarts the current scene
    // which gets the player a new game from the same game mode he's currently playing
    public void RestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    // This function gets executed when the main menu button is pressed, and it takes the player back
    // to the main menu
    public void MainMenuButton()
    {
        SceneManager.LoadScene(0);
    }
}
