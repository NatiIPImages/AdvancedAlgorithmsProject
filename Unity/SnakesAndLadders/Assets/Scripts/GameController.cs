using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This class is the controller for Player vs. Computer game mode.
public class GameController : MonoBehaviour
{
    // Board generation variables
    public GameObject playerPiecePrefab;
    public GameObject boardSpacePrefab;
    public GameObject ladderPrefab;
    public GameObject blueSnakePrefab;
    public GameObject greenSnakePrefab;
    public GameObject pinkSnakePrefab;
    public GameObject purpleSnakePrefab;
    public Transform boardParent;
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button rightUpButton;
    public Button rightDownButton;
    public Button leftUpButton;
    public Button leftDownButton;
    public GameObject indexTextPrefab;
    public int Rows = 7;
    public int Cols = 7;
    public int MaxTurns = 15;
    public int RequestedLadders = 5;
    public int RequestedSnakes = 5;

    // Stores the player prefab that was instantiated at game start.
    private GameObject playerPiece;
    // Stores the player's position
    private int playerPosition = 0;
    // This stores transforms of the board spaces that are being spawned at game start
    // Note: transforms contain the position & rotation of our board spaces.
    private List<Transform> boardSpaces = new List<Transform>();
    // This list stores the positions where ladders start and end
    private List<(int, int)> ladders = new List<(int, int)>();
    // This list stores the actual ladder game objects
    private List<GameObject> ladderGameObjects = new List<GameObject>();
    // This stores our snakes
    public List<GameObject> snakeObjects = new List<GameObject>();
    // The current turn
    private int currentTurn = 0;
    // # of turns since last time diagonal was available
    private int turnsSinceLastDiagonal = 0;
    // # of turns of which we allow diagonal movement
    private int DiagonalEveryXTurns = 3;
    // Time to wait before each computer turn
    private float TurnTimeWait = 1f;

    // Maximum d for alpha-beta pruning before using Eval
    public static int MaximumDepth = 10;

    // Takes care of game over logic (game over screen & text...)
    public GameOverScreen GameOverScript;

    // Shows an indicator on the screen of current turn
    public Text TurnsText;
    // Shows an indicator on the screen of # of turns since diagonal movement was available
    public Text DiagonalText;

    // Define an enum and variable of that enum that stores whether it's a player's or snake's turn
    public enum Turn { Player, Snake };
    private Turn currentTurnType = Turn.Player;

    // This function gets executed on start
    void Start()
    {
        // Get inputs from main menu
        int boardSizeInput = PlayerPrefs.GetInt("BoardSize", 7);
        Rows = boardSizeInput;
        Cols = boardSizeInput;
        int maxT = PlayerPrefs.GetInt("MaxTurns", 12);
        MaxTurns = maxT;
        int diagonal = PlayerPrefs.GetInt("DiagonalIn", 3);
        DiagonalEveryXTurns = diagonal;

        int ladders = PlayerPrefs.GetInt("Ladders", 2);
        RequestedLadders = ladders;
        int snakes = PlayerPrefs.GetInt("Snakes", 3);
        RequestedSnakes = snakes;
        int MaxD = PlayerPrefs.GetInt("MaxDepth", 10);
        MaximumDepth = MaxD;

        // Generate board
        GenerateBoard();
        // Generate snakes and ladders
        GenerateSnakesAndLadders();
        // Renders the player
        SpawnPlayerPiece();
        // Add a listener to the buttons that will make the player move when they're pressed
        upButton.onClick.AddListener(() => PlayerMove("Up"));
        downButton.onClick.AddListener(() => PlayerMove("Down"));
        leftButton.onClick.AddListener(() => PlayerMove("Left"));
        rightButton.onClick.AddListener(() => PlayerMove("Right"));
        rightUpButton.onClick.AddListener(() => PlayerMove("RightUp"));
        rightDownButton.onClick.AddListener(() => PlayerMove("RightDown"));
        leftUpButton.onClick.AddListener(() => PlayerMove("LeftUp"));
        leftDownButton.onClick.AddListener(() => PlayerMove("LeftDown"));
        // Update the on-screen indicator's text
        TurnsText.text = "Turns: " + MaxTurns.ToString();
        if (DiagonalEveryXTurns - turnsSinceLastDiagonal == 0)
            DiagonalText.text = "Diagonal Available";
        else DiagonalText.text = "Diagonal In: " + (DiagonalEveryXTurns - turnsSinceLastDiagonal).ToString();
        // Enable/Disable digonal movement according to the DiagonalIn input from the main menu
        EnableDiagonalButtons(diagonal == 0);
    }

    // Enables/Disables digonal movement according to the DiagonalIn input from the main menu
    void EnableDiagonalButtons(bool bEnable)
    {
        rightDownButton.gameObject.SetActive(bEnable);
        rightUpButton.gameObject.SetActive(bEnable);
        leftDownButton.gameObject.SetActive(bEnable);
        leftUpButton.gameObject.SetActive(bEnable);
    }

    // Generates the board on game start
    void GenerateBoard()
    {
        // Calculate space size
        float screenHeight = Camera.main.orthographicSize * 2;
        float screenWidth = screenHeight * Camera.main.aspect;
        float spaceSize = Mathf.Min(screenWidth / Cols, screenHeight / Rows);

        // Reduce space size significantly to ensure the board fits within the screen
        spaceSize *= 0.8f;

        // Generates all spaces and renders them on the screen
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                Vector3 position = new Vector3(j * spaceSize, i * spaceSize, 0);
                GameObject space = Instantiate(boardSpacePrefab, position, Quaternion.identity, boardParent);
                boardSpaces.Add(space.transform);

                SpriteRenderer renderer = space.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = Color.white;
                    renderer.size = new Vector2(spaceSize, spaceSize);
                }

                // Add index text
                GameObject indexText = Instantiate(indexTextPrefab, position, Quaternion.identity, space.transform);
                TextMesh textMesh = indexText.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = (i * Cols + j).ToString();
                    textMesh.characterSize = spaceSize / 5;
                    textMesh.color = Color.black;
                    textMesh.anchor = TextAnchor.UpperLeft;
                    textMesh.alignment = TextAlignment.Left;
                    textMesh.transform.localPosition = new Vector3(-spaceSize / 2 + 0.1f, spaceSize / 2 - 0.1f, 0);
                }
            }
        }

        // Center the board
        float boardWidth = Cols * spaceSize;
        float boardHeight = Rows * spaceSize;
        boardParent.position = new Vector3(-boardWidth / 2 + spaceSize / 2, -boardHeight / 2 + spaceSize / 2, 0);
    }

    // Generates ladders and snakes
    void GenerateSnakesAndLadders()
    {
        // Get some fresh data structures to store what we need
        ladders.Clear();
        HashSet<int> usedPositions = new HashSet<int>();

        // Generate requested ladders
        GenerateEntities(ladders, usedPositions, RequestedLadders);

        // Generate requested snakes
        GenerateEntitiesForSnakes(RequestedSnakes, usedPositions);
    }

    // Used to generate ladders
    void GenerateEntities(List<(int, int)> entities, HashSet<int> usedPositions, int count)
    {
        // Generates the ladders (randomally) and add to ladders dictionary. They will be rendered later.
        for (int i = 0; i < count; i++)
        {
            int startPos = 0, endPos = 0;
            startPos = Random.Range(1, Rows * Cols - 1); // Avoid start position at 0
            endPos = Random.Range(startPos + 1, Mathf.Min(Rows * Cols, startPos + 4 * Rows)); // Ladder moves up
            entities.Add((startPos, endPos));
            GameObject ladderObject = InstantiateLadder(ladderPrefab, boardSpaces[startPos].position, boardSpaces[endPos].position);
            Ladder ladderScript = ladderObject.GetComponent<Ladder>();
            ladderScript.startPos = startPos;
            ladderScript.endPos = endPos;
            ladderScript.index = i;
            ladderGameObjects.Add(ladderObject);
            for (int pos = Mathf.Min(startPos, endPos); pos <= Mathf.Max(startPos, endPos); pos++)
            {
                usedPositions.Add(pos);
            }
        }
    }

    // Generate snakes
    void GenerateEntitiesForSnakes(int count, HashSet<int> usedPositions)
    {
        // Generates # of requested snakes. Instantiates snake gameobjects and adds to our list.
        for (int i = 0; i < count; i++)
        {
            int startPos = 0, endPos = 0;
            startPos = Random.Range(1, Rows * Cols - 1); // Avoid start position at 0 (start)
            endPos = Random.Range(0, Rows * Cols - 1);
            if (startPos == endPos)
            {
                endPos += (endPos == Rows * Cols - 1) ? -1 : 1;
            }
            GameObject snakePrefab = GetRandomSnakePrefab();
            GameObject snakeObject = InstantiateSnake(snakePrefab, boardSpaces[startPos].position, boardSpaces[endPos].position);
            Snake snakeScript = snakeObject.GetComponent<Snake>();
            snakeScript.startPos = startPos;
            snakeScript.endPos = endPos;
            snakeScript.index = i;
            snakeObjects.Add(snakeObject);
            for (int pos = Mathf.Min(startPos, endPos); pos <= Mathf.Max(startPos, endPos); pos++)
            {
                usedPositions.Add(pos);
            }
        }
    }

    // Gets a random snake prefab to spawn and render. This chooses the color of the snake out of four different
    // colors (blue, green, pink, purple).
    GameObject GetRandomSnakePrefab()
    {
        GameObject[] snakePrefabs = { blueSnakePrefab, greenSnakePrefab, pinkSnakePrefab, purpleSnakePrefab };
        return snakePrefabs[Random.Range(0, snakePrefabs.Length)];
    }

    // Instantiates (spawnes) the ladders
    GameObject InstantiateLadder(GameObject prefab, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        Vector3 midpoint = (start + end) / 2;
        midpoint.z = -1; // Ensure the ladder is above the board

        GameObject ladder = Instantiate(prefab, midpoint, Quaternion.identity, boardParent);

        // Calculate the angle of rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
        ladder.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Calculate the scale to fit the distance
        float distance = direction.magnitude;
        SpriteRenderer spriteRenderer = ladder.GetComponent<SpriteRenderer>();
        float spriteHeight = spriteRenderer.sprite.bounds.size.y;

        float scaleY = distance / spriteHeight;
        ladder.transform.localScale = new Vector3(0.2f, scaleY, 1);
        Ladder ladderScript = ladder.GetComponent<Ladder>();
        if (ladderScript == null)
        {
            ladderScript = ladder.AddComponent<Ladder>();
        }
        return ladder;
    }

    // Instantiates (spawnes) the snakes.
    // Calculates how to render the snake on the board to show correctly.
    GameObject InstantiateSnake(GameObject prefab, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        Vector3 midpoint = (start + end) / 2;
        midpoint.z = -1; // Ensure the snake is above the board

        GameObject snake = Instantiate(prefab, midpoint, Quaternion.identity, boardParent);

        // Calculate the angle of rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
        snake.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Calculate the scale to fit the distance
        float distance = direction.magnitude;
        SpriteRenderer spriteRenderer = snake.GetComponent<SpriteRenderer>();
        float spriteHeight = spriteRenderer.sprite.bounds.size.y;

        float scaleY = distance / spriteHeight;
        snake.transform.localScale = new Vector3(0.3f, scaleY, 1);

        // Ensure the snake has a Snake.cs component
        Snake snakeScript = snake.GetComponent<Snake>();
        if (snakeScript == null)
        {
            snakeScript = snake.AddComponent<Snake>();
        }

        return snake;
    }

    // Spawns the actual player.
    void SpawnPlayerPiece()
    {
        // Instantiate the player piece at the center of the first square
        Vector3 startPosition = boardSpaces[0].position;
        startPosition.z = -2;  // Ensure the player piece is above the board and other elements
        playerPiece = Instantiate(playerPiecePrefab, startPosition, Quaternion.identity);
        playerPiece.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);  // Adjust the scale as needed

        // Ensure the player piece is centered in the first square
        CenterPlayerPiece();
    }

    // Centeres the player on its current board square so he wouldn't look off.
    void CenterPlayerPiece()
    {
        // Ensure the player piece is centered in the square and above the board
        if (playerPiece != null && boardSpaces.Count > 0)
        {
            Vector3 newPosition = boardSpaces[playerPosition].position;
            newPosition.y += 0.5f; // Adjust the height as needed
            newPosition.x += 1.25f; // Adjust the height as needed
            newPosition.z = -2;  // Ensure the player piece is above the board
            playerPiece.transform.position = newPosition;
        }
    }

    // Game ended, show the game over screen with text according to the game's results. 
    void GameEnded()
    {
        if (playerPosition == Rows * Cols - 1)
        {
            GameOverScript.Setup("Victory!");
        }
        else
        {
            GameOverScript.Setup("Defeat!");
        }
    }

    // This function is moving the player, and it's happening according to the button that was pressed.
    void PlayerMove(string direction)
    {
        // Return early if it's the snake's turn
        if (currentTurnType != Turn.Player)
            return;

        // Get the current player's position
        int newPosition = playerPosition;

        // This switch statement is used to know what button was pressed. This is how we know where to move the player to.
        switch (direction)
        {
            case "Up":
                newPosition += Cols;
                break;
            case "Down":
                newPosition -= Cols;
                break;
            case "Left":
                newPosition -= 1;
                break;
            case "Right":
                newPosition += 1;
                break;
            case "RightUp":
                newPosition += Cols + 1;
                break;
            case "LeftUp":
                newPosition += Cols - 1;
                break;
            case "LeftDown":
                newPosition -= (Cols + 1);
                break;
            case "RightDown":
                newPosition -= (Cols - 1);
                break;
        }

        // If the player is inside the board, set his new position and render on-screen.
        // Note: Here we are taking care of what happens if a player lands on snake or ladder, using the CalculatePlayerNewPosition function.
        if (newPosition >= 0 && newPosition < boardSpaces.Count)
        {
            // Calculate player's position, takes into account snakes and ladders.
            playerPosition = CalculatePlayerNewPosition(newPosition);
            // CenterPlayerPiece();
            CheckSnakesAndLadders();
        }

        // Track diagonal movement, if the player can move diagonally, update the on-screen text indicator and enable buttons
        turnsSinceLastDiagonal++;
        if (turnsSinceLastDiagonal > DiagonalEveryXTurns)
            turnsSinceLastDiagonal = 0;
        // If the player can move diagonally
        if (DiagonalEveryXTurns - turnsSinceLastDiagonal == 0)
        {
            EnableDiagonalButtons(true);
            DiagonalText.text = "Diagonal\nAvailable";
        }
        // If the player can't move diagonally
        else
        {
            EnableDiagonalButtons(false);
            DiagonalText.text = "Diagonal In: " + (DiagonalEveryXTurns - turnsSinceLastDiagonal).ToString();
        }

        // Track the turns that were used by the player
        currentTurn++;
        TurnsText.text = "Turns: " + (MaxTurns - currentTurn).ToString();
        // If the player used all his turns or reached the top, game ended
        if (currentTurn >= MaxTurns || playerPosition == Rows * Cols - 1)
        {
            GameEnded();
        }
        // If the game didn't end, set the turn to be snake's turn and keep the game going!
        else
        {
            // End player's turn and start snake's turn
            currentTurnType = Turn.Snake;
            Invoke("SnakeTurn", TurnTimeWait);
        }
    }

    // This function calculates the next player position. We need to take into account the ladders and snakes
    // for every new position the player lands on and thus the loop.
    // For example: A player can move from 20 to 21 and in 21 there's a snake from 21 to 15, and in 15 there's a ladder from
    // 15 to 31. In that case, the player will end at 31.
    // Note: Ladders get a priority, which means that if there are both a snake and a ladder on some position, the
    // player will choose the ladder at anytime.
    private int CalculatePlayerNewPosition(int newPosition)
    {
        do
        {
            foreach (var ladder in ladders)
            {
                if (ladder.Item1 == newPosition)
                {
                    newPosition = ladder.Item2;
                    continue;
                }
            }
            foreach (var snake in snakeObjects)
            {
                if (snake.GetComponent<Snake>().startPos == newPosition)
                {
                    newPosition = snake.GetComponent<Snake>().endPos;
                    continue;
                }
            }
            break;
        } while (true);
        return newPosition;
    }

    // This function returns true if there's a ladder start at the given position.
    private bool LadderStartExistsAtPosition(int position)
    {
        foreach (var ladder in ladders)
        {
            if (ladder.Item1 == position)
                return true;
        }
        return false;
    }
    // This function returns the ladder end position if there's a ladder start at the given position. Otherwise, returns -1.
    private int GetLadderEndPositionFromStartPosition(int position)
    {
        foreach (var ladder in ladders)
        {
            if (ladder.Item1 == position)
                return ladder.Item2;
        }
        return -1;
    }
    // Checks whether the current player's position contains and snakes or ladders. If so, move him according to the position.
    void CheckSnakesAndLadders()
    {
        int newPosition = playerPosition;
        int i = 0;
        List<LightweightSnake> snakes = TranslateSnakesToTemporaryClass();
        do
        {
            i++;
            // If the there's a ladder on the new player's position, move the player to the ladder's top
            foreach (var ladder in ladders)
            {
                if (ladder.Item1 == newPosition)
                {
                    newPosition = ladder.Item2;
                    // Up to 100 times because we don't want to be in an endless loop... There could be a snake and ladder loop on two position. We don't want the game to be stuck or crash at this situation!
                    if (i >= 100)
                        break;
                    continue;
                }
            }
            // If there's a snake head on the new player's position, move the player to the snake's tail
            foreach (LightweightSnake snake in snakes)
            {
                if (snake.startPos == newPosition)
                {
                    newPosition = snake.endPos;
                    // Up to 100 times because we don't want to be in an endless loop... There could be a snake and ladder loop on two position. We don't want the game to be stuck or crash at this situation!
                    if (i >= 100)
                        break;
                    continue;
                }
            }
            // If we got here, there's isn't anything on the new player's position to move him.
            break;
        } while (i < 100); // Up to 100 times because we don't want to be in an endless loop... There could be a snake and ladder loop on two position. We don't want the game to be stuck or crash at this situation!
        playerPosition = newPosition;
        CenterPlayerPiece();
    }

    // On snake's turn, create the current game state, run alpha-beta pruning,
    // set the snake's new position, ladders new position, and player's new position according to the algorithm's choice.
    void SnakeTurn()
    {
        // Creates the current game state
        GameState currentState = new GameState(playerPosition, TranslateSnakesToTemporaryClass(), currentTurn, MaxTurns, Rows, Cols, GetLadders(), currentTurnType, turnsSinceLastDiagonal, DiagonalEveryXTurns);
        // Alpha-beta pruning:
        (float, GameState) bestMove = currentState.AlphaBetaPruning(MaximumDepth, float.NegativeInfinity, float.PositiveInfinity, currentTurnType);
        // Updates snakes positions and render them on on-screen
        for (int i = 0; i < snakeObjects.Count; i++)
        {
            Snake snake = snakeObjects[i].GetComponent<Snake>();
            snake.startPos = bestMove.Item2.snakeObjects[i].startPos;
            snake.endPos = bestMove.Item2.snakeObjects[i].endPos;
            snake.UpdateSnakePosition(bestMove.Item2.snakeObjects[i].startPos, bestMove.Item2.snakeObjects[i].endPos);
        }
        // Get ladders again, nedded because snakes can move ladders
        ladders.Clear();
        for (int i = 0; i < bestMove.Item2.ladders.Count; i++)
        {
            (int, int) ladder = bestMove.Item2.ladders[i];
            ladders.Add((ladder.Item1, ladder.Item2));
            ladderGameObjects[i].GetComponent<Ladder>().UpdateLadderPosition(ladder.Item1, ladder.Item2);
        }
        // Updates player position according to new snakes and ladders positon
        CheckSnakesAndLadders();
        // Set the next turn to be the player's, and keep the game going!
        currentTurnType = Turn.Player;
    }

    // This is used to deep copy the snakes when creating a new game state so we don't ruin our current data
    private List<LightweightSnake> TranslateSnakesToTemporaryClass()
    {
        List < LightweightSnake > snakes = new List<LightweightSnake>();
        for (int i = 0; i < snakeObjects.Count; i++)
        {
            snakes.Add(new LightweightSnake(snakeObjects[i].GetComponent<Snake>().startPos, snakeObjects[i].GetComponent<Snake>().endPos));
        }
        return snakes;
    }

    // Returns a list of all snake matrix positions
    List<(int, int)> GetSnakesPositions()
    {
        List<(int, int)> positions = new List<(int, int)>();
        foreach (var snake in snakeObjects)
        {
            positions.Add((snake.GetComponent<Snake>().startPos, snake.GetComponent<Snake>().endPos));
        }
        return positions;
    }

    // Returns a list of all ladders matrix positions
    List<(int, int)> GetLadders()
    {
        List<(int, int)> ladderPositions = new List<(int, int)>();
        foreach (var ladder in ladders)
        {
            ladderPositions.Add((ladder.Item1, ladder.Item2));
        }
        return ladderPositions;
    }

    // Returns the board's position in the current scene as a Vector3 (x, y, z)
    // Note: If this index doesn't exist in our board, (0, 0, 0) will be returned.
    public Vector3 GetBoardSpacePosition(int index)
    {
        if (index >= 0 && index < boardSpaces.Count)
        {
            return boardSpaces[index].position;
        }
        return Vector3.zero;
    }
}
