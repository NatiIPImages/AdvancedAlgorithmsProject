using System.Collections.Generic;
using UnityEngine;

// This class represents a ladder in the Computer vs. Computer game mode
public class ComLadder : MonoBehaviour
{
    // Store the positions of the ladder
    public int startPos;
    public int endPos;
    // Store the index of the ladder in the specific game
    public int index;
    // Store the board's size
    private int rows;
    private int cols;
    // Store the game controller
    private ComGameController gameController;

    // This function gets executed on game start
    private void Start()
    {
        // Get the game controller and board size
        gameController = FindObjectOfType<ComGameController>();
        rows = gameController.Rows;
        cols = gameController.Cols;
    }

    // This function gets (from the game controller) a new start and end position for the ladder, it renderes the
    // ladder on the new position and updates its positions.
    public void UpdateLadderPosition(int newStartPos, int newEndPos)
    {
        this.startPos = newStartPos;
        this.endPos = newEndPos;
        Vector3 start = gameController.GetBoardSpacePosition(startPos);
        Vector3 end = gameController.GetBoardSpacePosition(endPos);

        Vector3 direction = end - start;
        Vector3 midpoint = (start + end) / 2;
        midpoint.z = -1; // Ensure the snake is above the board

        transform.position = midpoint;

        // Calculate the angle of rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Calculate the scale to fit the distance
        float distance = direction.magnitude;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float spriteHeight = spriteRenderer.sprite.bounds.size.y;

        float scaleY = distance / spriteHeight;
        transform.localScale = new Vector3(0.2f, scaleY, 1);
    }
}