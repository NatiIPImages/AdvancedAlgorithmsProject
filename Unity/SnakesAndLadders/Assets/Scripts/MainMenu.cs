using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This class is for the main menu scene
public class MainMenu : MonoBehaviour
{
    // All the input fields
    public TMP_InputField inputField;
    public TMP_InputField maxTurnsInput;
    public TMP_InputField diagonalInput;
    public TMP_InputField laddersInput;
    public TMP_InputField snakesInput;
    public TMP_InputField maximumDepthInput;
    // Text to show in case of an error
    public Text errorText;
    // Gets executed on game start
    void Start()
    {
        // Game just started - disable errors
        errorText.gameObject.SetActive(false);
    }

    // Stores the inputs to the local storage and loads the player vs. computer game scene
    public void PlayHuman()
    {
        // Store all input fields to local storage

        // Stores board size
        if (int.TryParse(inputField.text, out int playerInput))
        {
            // If board size is < 3, show error message and return
            if (playerInput < 3)
            {
                errorText.text = "Board size must be atleast 3!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // If we got here, the input value is good. Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("BoardSize", playerInput);
            PlayerPrefs.Save();
        }
        // If board size is not an integer, show error message and return
        else
        {
            errorText.text = "Board size must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores max turns
        if (int.TryParse(maxTurnsInput.text, out int maxTurns))
        {
            // If max turns is < 3, show error message and return
            if (maxTurns < 3)
            {
                errorText.text = "Max turns must be atleast 3!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // If we got here, the input value is good. Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("MaxTurns", maxTurns);
            PlayerPrefs.Save();
        }
        // If max turns is not an integer, show error message and return
        else
        {
            errorText.text = "Max turns must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores # of turns to get diagonal movement
        if (int.TryParse(diagonalInput.text, out int diagonal))
        {
            if (diagonal < 1)
            {
                errorText.text = "Diagonal must be atleast 1!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("DiagonalIn", diagonal);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Diagonal must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the # of requested ladders
        if (int.TryParse(laddersInput.text, out int ladders))
        {
            if (ladders < 0)
            {
                errorText.text = "Ladders must be a non-negative number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("Ladders", ladders);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Ladders must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the # of requested snakes
        if (int.TryParse(snakesInput.text, out int snakes))
        {
            if (snakes < 1)
            {
                errorText.text = "Snakes must be a positive number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("Snakes", snakes);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Snakes must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the maximum depth input
        if (int.TryParse(maximumDepthInput.text, out int maxD))
        {
            if (maxD < 1)
            {
                errorText.text = "Maximum depth must be a positive number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to MaxDepth
            PlayerPrefs.SetInt("MaxDepth", maxD);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Maximum depth must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // If everything is good, disable error messages
        errorText.gameObject.SetActive(false);
        // Load the player vs. computer scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Stores the inputs to the local storage and loads the computer vs. computer game scene
    public void PlayComputer()
    {
        // Store all input fields to local storage

        // Stores board size
        if (int.TryParse(inputField.text, out int playerInput))
        {
            // If board size is < 3, show error message and return
            if (playerInput < 3)
            {
                errorText.text = "Board size must be atleast 3!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // If we got here, the input value is good. Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("BoardSize", playerInput);
            PlayerPrefs.Save();
        }
        // If board size is not an integer, show error message and return
        else
        {
            errorText.text = "Board size must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores max turns
        if (int.TryParse(maxTurnsInput.text, out int maxTurns))
        {
            // If max turns is < 3, show error message and return
            if (maxTurns < 3)
            {
                errorText.text = "Max turns must be atleast 3!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // If we got here, the input value is good. Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("MaxTurns", maxTurns);
            PlayerPrefs.Save();
        }
        // If max turns is not an integer, show error message and return
        else
        {
            errorText.text = "Max turns must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores # of turns to get diagonal movement
        if (int.TryParse(diagonalInput.text, out int diagonal))
        {
            if (diagonal < 1)
            {
                errorText.text = "Diagonal must be atleast 1!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("DiagonalIn", diagonal);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Diagonal must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the # of requested ladders
        if (int.TryParse(laddersInput.text, out int ladders))
        {
            if (ladders < 0)
            {
                errorText.text = "Ladders must be a non-negative number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("Ladders", ladders);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Ladders must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the # of requested snakes
        if (int.TryParse(snakesInput.text, out int snakes))
        {
            if (snakes < 1)
            {
                errorText.text = "Snakes must be a positive number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to PlayerPrefs
            PlayerPrefs.SetInt("Snakes", snakes);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Snakes must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // Stores the maximum depth input
        if (int.TryParse(maximumDepthInput.text, out int maxD))
        {
            if (maxD < 1)
            {
                errorText.text = "Maximum depth must be a positive number!";
                errorText.gameObject.SetActive(true);
                return;
            }
            // Save the input integer to MaxDepth
            PlayerPrefs.SetInt("MaxDepth", maxD);
            PlayerPrefs.Save();
        }
        else
        {
            errorText.text = "Maximum depth must be an integer!!";
            errorText.gameObject.SetActive(true);
            return;
        }

        // If everything is good, disable error messages
        errorText.gameObject.SetActive(false);
        // Load the computer scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }

    // This gets executed when the quit game button is pressed. It closes the game.
    public void QuitGame()
    {
        Application.Quit();
    }
}
