using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// This class represents states of games in the Computer vs. Computer game mode.
public class ComGameState
{
    // Stores the position of the player
    public int playerPos;
    // Stores the positions of the snake. We used LightweightSnake as a struct that stores start and end position of a specific snake.
    public List<ComLightweightSnake> snakeObjects = new List<ComLightweightSnake>();
    // Tracks the turns that the player have already used.
    public int currentTurn;
    // Stores the number of max allowed player turns.
    public int maxTurns;
    // Stores the board's size
    public int rows;
    public int cols;
    // Stores the start and end positions of all ladders.
    public List<(int, int)> ladders;
    // Stores the current turn type. This is an enum with two options: Player or Snake.
    public ComGameController.Turn turnType;
    // Stores the number of turns since last time diagonal movement was available.
    public int turnsSinceLastDiagonal;
    // Stores the number of turns that need to pass before a diagonal movement is available.
    public int DiagonalEveryXTurns = 3;
    // Stores the index of the current snake. This is calculated in the constructor according to the snake that is closest to the player.
    public int currentSnake;

    // The constructor
    public ComGameState(int playerPos, List<ComLightweightSnake> snakeObjects, int currentTurn, int maxTurns,
        int rows, int cols, List<(int, int)> ladders, ComGameController.Turn turnType, int turnsSinceLastDiagonal, int DiagonalEveryXTurns)
    {
        this.playerPos = playerPos;
        this.snakeObjects = snakeObjects;
        this.currentTurn = currentTurn;
        this.maxTurns = maxTurns;
        this.rows = rows;
        this.cols = cols;
        this.ladders = ladders;
        this.turnType = turnType;
        this.turnsSinceLastDiagonal = turnsSinceLastDiagonal;
        this.DiagonalEveryXTurns = DiagonalEveryXTurns;
        this.currentSnake = getCurrentSnake(); // Calculated  by the snake that is the closest to the player
    }

    // Returns true iff this state is a goal state.
    public bool IsGoal()
    {
        // Goal state if player reaches the end or max turns are exceeded
        return playerPos >= rows * cols - 1 || currentTurn > maxTurns;
    }

    // Our Utility function. Returns the value of goal states. Different values for each agent type: player/snake.
    public float Utility()
    {
        // If player reached the end of the board
        if (playerPos >= cols * rows - 1)
        {
            return turnType == ComGameController.Turn.Player ?  float.NegativeInfinity: float.PositiveInfinity;
        }
        // Else we got here because currentTurn > maxTurns and after this if check we know the player is not in the finished square
        return turnType == ComGameController.Turn.Player ?  float.PositiveInfinity : float.NegativeInfinity;
    }

    // This is our evaluation method.
    public float Evaluate()
    {
        // Return Utility if it's a goal state
        if (IsGoal())
        {
            return Utility();
        }
        // Get the player's position
        (int, int) player = getPlayerPosition();
        // If it's a player's turn
        if (turnType == ComGameController.Turn.Player)
        {
            // Return the player's Manhattan distance from his position to his target (The top right square) * -1
            // because the algorithm maximizes the distance, and we want the minimum distance. 
            return (getDistance(rows - 1, cols - 1, player.Item1, player.Item2)) * -1;
        }
        // If it's a snake's turn
        else
        {
            // Get the snake position
            (int, int) snakeStart = getMatrixPosition(snakeObjects[currentSnake].startPos);
            (int, int) snakeEnd = getMatrixPosition(snakeObjects[currentSnake].endPos);
            // Calculate the player's Manhattan distance from his position to his target
            float playerToGoalDistance = getDistance(rows - 1, cols - 1, player.Item1, player.Item2);
            // Calculate the distance between the player to the closest snake to him.
            float closestSnakeToPlayerDistance = float.MaxValue;
            foreach (var snake in snakeObjects)
            {
                (int, int) snakePos = getMatrixPosition(snake.startPos);
                float distance = getDistance(player.Item1, player.Item2, snakePos.Item1, snakePos.Item2);
                closestSnakeToPlayerDistance = Mathf.Min(closestSnakeToPlayerDistance, distance);
            }
            // Return the player's goal to goal minus the distance to the closest snake.
            return playerToGoalDistance - closestSnakeToPlayerDistance;
        }
    }

    // A different evaluation function used for tests, and defined for snakes. We found our current evaluation method better.
    public float snakeAgentEval(int currentSnake)
    {
        (int, int) snakeStartMatrixPos = getMatrixPosition(snakeObjects[currentSnake].startPos);
        (int, int) snakeEndMatrixPos = getMatrixPosition(snakeObjects[currentSnake].endPos);
        (int, int) playerMatrixPos = getPlayerPosition();
        return getDistance(snakeStartMatrixPos.Item1, snakeStartMatrixPos.Item2, playerMatrixPos.Item1, playerMatrixPos.Item2) +
            getDistance(snakeEndMatrixPos.Item1, snakeEndMatrixPos.Item2, rows - 1, cols - 1);
    }

    // Returns Manhattan distance between two points in the board
    public float getDistance(int p1_row, int p1_col, int p2_row, int p2_col)
    {
        return Mathf.Abs(p1_row - p2_row) + Mathf.Abs(p1_col - p2_col);
    }

    // Returns the player's position as a matrix position. Example: (0, 0)
    public (int, int) getPlayerPosition()
    {
        int playerRow = (int)Mathf.Floor(playerPos / (float)rows);
        int playerCol = playerPos % cols;
        return (playerRow, playerCol);
    }

    // Returns the player's minimum distance to goal by checking his distance to ladders/snakes + distance from
    // ladder/snake end to goal. Used to test another evaluation function.
    public float getPlayerMinimumDistanceToGoal()
    {
        (int, int) target = (rows - 1, cols - 1);
        (int, int) playerCurrentMatrixPosition = getPlayerPosition();
        float ManhattanDistance = getDistance(playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2,
            target.Item1, target.Item2);
        foreach ((int, int) ladder in ladders)
        {
            (int, int) ladderStartPos = getMatrixPosition(ladder.Item1);
            (int, int) ladderEndPos = getMatrixPosition(ladder.Item2);
            float playerToLadderStart = getDistance(ladderStartPos.Item1, ladderStartPos.Item2,
            playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
            float ladderEndToGoal = getDistance(ladderEndPos.Item1, ladderEndPos.Item2,
                target.Item1, target.Item2);
            float tmpDistance = playerToLadderStart + ladderEndToGoal;
            if (tmpDistance < ManhattanDistance)
            {
                ManhattanDistance = tmpDistance;
            }
        }
        foreach (ComLightweightSnake snake in snakeObjects)
        {
            if (snake != null)
            {
                int snakePosition = snake.startPos;
                (int, int) snakeStartPosition = getMatrixPosition(snake.startPos);
                (int, int) snakeEndPosition = getMatrixPosition(snake.endPos);
                float distancePlayerToStartSnake = getDistance(snakeStartPosition.Item1,
                    snakeStartPosition.Item2, playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
                float distanceSnakeEndToGoal = getDistance(snakeEndPosition.Item1, snakeEndPosition.Item2,
                    target.Item1, target.Item2);
                if (distancePlayerToStartSnake + distanceSnakeEndToGoal < ManhattanDistance)
                {
                    ManhattanDistance = distancePlayerToStartSnake + distanceSnakeEndToGoal;
                }
            }
        }
        return ManhattanDistance;
    }

    // Returns the player's maximum distance to goal by checking his distance to ladders/snakes + distance from
    // ladder/snake end to goal. Used to test another evaluation function.
    public float getPlayerMaximumDistanceToGoal()
    {
        (int, int) target = (rows - 1, cols - 1);
        (int, int) playerCurrentMatrixPosition = getPlayerPosition();
        float ManhattanDistance = getDistance(playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2,
            target.Item1, target.Item2);
        foreach ((int, int) ladder in ladders)
        {
            (int, int) ladderStartPos = getMatrixPosition(ladder.Item1);
            (int, int) ladderEndPos = getMatrixPosition(ladder.Item2);
            float playerToLadderStart = getDistance(ladderStartPos.Item1, ladderStartPos.Item2,
            playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
            float ladderEndToGoal = getDistance(ladderEndPos.Item1, ladderEndPos.Item2,
                target.Item1, target.Item2);
            float tmpDistance = playerToLadderStart + ladderEndToGoal;
            if (tmpDistance > ManhattanDistance)
            {
                ManhattanDistance = tmpDistance;
            }
        }
        foreach (ComLightweightSnake snake in snakeObjects)
        {
            if (snake != null)
            {
                int snakePosition = snake.startPos;
                (int, int) snakeStartPosition = getMatrixPosition(snake.startPos);
                (int, int) snakeEndPosition = getMatrixPosition(snake.endPos);
                float distancePlayerToStartSnake = getDistance(snakeStartPosition.Item1,
                    snakeStartPosition.Item2, playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
                float distanceSnakeEndToGoal = getDistance(snakeEndPosition.Item1, snakeEndPosition.Item2,
                    target.Item1, target.Item2);
                if (distancePlayerToStartSnake + distanceSnakeEndToGoal > ManhattanDistance)
                {
                    ManhattanDistance = distancePlayerToStartSnake + distanceSnakeEndToGoal;
                }
            }
        }
        return ManhattanDistance;
    }

    // Another evaluation method testing. This one wasn't used.
    public float getPlayerEval()
    {
        (int, int) target = (rows, cols);
        (int, int) playerCurrentMatrixPosition = getPlayerPosition();
        float ManhattanDistance = getDistance(playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2,
            target.Item1, target.Item2);
        foreach ((int, int) ladder in ladders)
        {
            float tmpDistance = getDistance(ladder.Item1, ladder.Item2,
            playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
            if (tmpDistance < ManhattanDistance)
            {
                ManhattanDistance = tmpDistance;
            }
        }
        foreach (ComLightweightSnake snake in snakeObjects)
        {
            if (snake != null)
            {
                int snakePosition = snake.startPos;
                if (getMatrixPosition(snakePosition).Item1 <
                    getMatrixPosition(snake.endPos).Item1 ||

                    getMatrixPosition(snakePosition).Item1 ==
                    getMatrixPosition(snake.endPos).Item1 &&
                    getMatrixPosition(snakePosition).Item2 <
                    getMatrixPosition(snake.endPos).Item2)
                {
                    (int, int) snakeMatrixPosition = getMatrixPosition(snakePosition);
                    float tmpDistance = getDistance(snakeMatrixPosition.Item1, snakeMatrixPosition.Item2,
                    playerCurrentMatrixPosition.Item1, playerCurrentMatrixPosition.Item2);
                    if (tmpDistance < ManhattanDistance)
                    {
                        ManhattanDistance = tmpDistance;
                    }
                }
            }
        }
        return ManhattanDistance;
    }

    // This method gets an index position and returns its matrix position. For example: 1 -> (row 0, column 1)
    public (int, int) getMatrixPosition(int position)
    {
        int positionRow = (int)Mathf.Floor(position / (float)rows);
        int positionCol = position % cols;
        return (positionRow, positionCol);
    }

    // This function is the inverse of the last function. It gets a matrix position and returns its index position.
    public int getIndexPosition((int, int) position)
    {
        return position.Item1 * rows + position.Item2;
    }

    // This function generates the successors of a state.
    private List<ComGameState> GenerateSuccessors(int currentSnake)
    {
        // This list will contain all the possible successors when we finish.
        List<ComGameState> successors = new List<ComGameState>();
        if (turnType == ComGameController.Turn.Snake) // If it's a snake's turn
        {
            // Moving ladders (from ladder's start position)
            int startPos = snakeObjects[currentSnake].startPos;
            int endPos = snakeObjects[currentSnake].endPos;
            // Get the snake's start and end positions
            (int, int) startMatrixPos = getMatrixPosition(startPos);
            (int, int) endMatrixPos = getMatrixPosition(endPos);
            foreach ((int, int) ladder in ladders)
            {
                (int, int) ladderStartMatrixPosition = getMatrixPosition(ladder.Item1);
                (int, int) ladderEndMatrixPosition = getMatrixPosition(ladder.Item2);
                // There's a ladder under the snake
                if (startMatrixPos.Item1 - 1 == ladderStartMatrixPosition.Item1 && startMatrixPos.Item2 == ladderStartMatrixPosition.Item2)
                {
                    // Move ladder up if possible
                    if (ladderEndMatrixPosition.Item1 + 1 < rows && ladderStartMatrixPosition.Item1 + 1 < rows)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 + 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 + 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                    // Move ladder down if possible
                    if (ladderStartMatrixPosition.Item1 - 1 >= 0 && ladderEndMatrixPosition.Item1 - 1 >= 0 &&
                        endPos - 2 * rows >= 0)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 - 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos - 2 * rows;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 - 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                }
                // There's a ladder to the left of the snake
                if (startMatrixPos.Item2 - 1 == ladderStartMatrixPosition.Item2 && startMatrixPos.Item1 == ladderStartMatrixPosition.Item1)
                {
                    // Move ladder up if possible
                    if (ladderEndMatrixPosition.Item1 + 1 < rows && ladderStartMatrixPosition.Item1 + 1 < rows &&
                        endPos + rows - 1 < rows * cols)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 + 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos + rows - 1;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 + 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                    // Move ladder down if possible
                    if (ladderStartMatrixPosition.Item1 - 1 >= 0 && ladderEndMatrixPosition.Item1 - 1 >= 0 &&
                        endPos - rows - 1 >= 0)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 - 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos - rows - 1;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 - 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                }
                // There's a ladder above the snake
                if (startMatrixPos.Item1 + 1 == ladderStartMatrixPosition.Item1 && startMatrixPos.Item2 == ladderStartMatrixPosition.Item2)
                {
                    // Move ladder up if possible
                    if (ladderEndMatrixPosition.Item1 + 1 < rows && ladderStartMatrixPosition.Item1 + 1 < rows &&
                        endPos + 2 * rows < rows * cols)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 + 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos + 2 * rows;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 + 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                    // Move ladder down if possible
                    if (ladderStartMatrixPosition.Item1 - 1 >= 0 && ladderEndMatrixPosition.Item1 - 1 >= 0)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 - 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 - 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                }
                // There's a ladder to the right of the snake
                if (startMatrixPos.Item2 + 1 == ladderStartMatrixPosition.Item2 && startMatrixPos.Item1 == ladderStartMatrixPosition.Item1)
                {
                    // Move ladder up if possible
                    if (ladderEndMatrixPosition.Item1 + 1 < rows && ladderStartMatrixPosition.Item1 + 1 < rows &&
                        endPos + rows + 1 < rows * cols)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 + 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos + rows + 1;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 + 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                    // Move ladder down if possible
                    if (ladderStartMatrixPosition.Item1 - 1 >= 0 && ladderEndMatrixPosition.Item1 - 1 >= 0 &&
                        endPos - rows + 1 >= 0)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 - 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos - rows + 1;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 - 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                }
                // There's a ladder right on the snake
                if (startMatrixPos.Item1 == ladderStartMatrixPosition.Item1 &&
                    startMatrixPos.Item2 == ladderStartMatrixPosition.Item2)
                {
                    // Move ladder up if possible
                    if (ladderEndMatrixPosition.Item1 + 1 < rows && ladderStartMatrixPosition.Item1 + 1 < rows &&
                        endPos + rows < rows * cols)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 + 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos + rows;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 + 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                    // Move ladder down if possible
                    if (ladderStartMatrixPosition.Item1 - 1 >= 0 && ladderEndMatrixPosition.Item1 - 1 >= 0 &&
                        endPos - rows >= 0)
                    {
                        int snakeNewStartPosition = getIndexPosition((ladderStartMatrixPosition.Item1 - 1, ladderStartMatrixPosition.Item2));
                        int snakeNewEndPosition = endPos - rows;
                        int ladderNewEndPosition = getIndexPosition((ladderEndMatrixPosition.Item1 - 1, ladderEndMatrixPosition.Item2));
                        ComGameState ngs = SnakeTurnBuildComGameStateMoveLadder(snakeNewStartPosition,
                            snakeNewEndPosition, ladder, (snakeNewStartPosition,
                            ladderNewEndPosition));
                        successors.Add(ngs);
                    }
                }
            }

            // Move without moving any ladders
            // Snake going up
            if (startMatrixPos.Item1 + 1 < rows && endMatrixPos.Item1 + 1 < rows)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 + 1, startMatrixPos.Item2));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 + 1, endMatrixPos.Item2));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going down
            if (startMatrixPos.Item1 - 1 >= 0 && endMatrixPos.Item1 - 1 >= 0)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 - 1, startMatrixPos.Item2));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 - 1, endMatrixPos.Item2));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going left
            if (startMatrixPos.Item2 - 1 >= 0 && endMatrixPos.Item2 - 1 >= 0)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1, startMatrixPos.Item2 - 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1, endMatrixPos.Item2 - 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going right
            if (startMatrixPos.Item2 + 1 < cols && endMatrixPos.Item2 + 1 < cols)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1, startMatrixPos.Item2 + 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1, endMatrixPos.Item2 + 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going up-right (diagonal)
            if (startMatrixPos.Item1 + 1 < cols && endMatrixPos.Item1 + 1 < cols && startMatrixPos.Item2 + 1 < cols && endMatrixPos.Item2 + 1 < cols)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 + 1, startMatrixPos.Item2 + 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 + 1, endMatrixPos.Item2 + 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going up-left (diagonal)
            if (startMatrixPos.Item1 + 1 < cols && endMatrixPos.Item1 + 1 < cols && startMatrixPos.Item2 - 1 >= 0 && endMatrixPos.Item2 - 1 >= 0)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 + 1, startMatrixPos.Item2 - 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 + 1, endMatrixPos.Item2 - 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going down-left (diagonal)
            if (startMatrixPos.Item1 - 1 >= 0 && endMatrixPos.Item1 - 1 >= 0 && startMatrixPos.Item2 - 1 >= 0 && endMatrixPos.Item2 - 1 >= 0)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 - 1, startMatrixPos.Item2 - 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 - 1, endMatrixPos.Item2 - 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }
            // Snake going down-right (diagonal)
            if (startMatrixPos.Item1 - 1 >= 0 && endMatrixPos.Item1 - 1 >= 0 && startMatrixPos.Item2 + 1 < cols && endMatrixPos.Item2 + 1 < cols)
            {
                int newStartPos = getIndexPosition((startMatrixPos.Item1 - 1, startMatrixPos.Item2 + 1));
                int newEndPos = getIndexPosition((endMatrixPos.Item1 - 1, endMatrixPos.Item2 + 1));
                ComGameState ngs = SnakeTurnBuildComGameState(newStartPos, newEndPos);
                successors.Add(ngs);
            }

            ComGameState current = new ComGameState(playerPos, snakeObjects, currentTurn, maxTurns, rows, cols, ladders,
                ComGameController.Turn.Player, turnsSinceLastDiagonal, DiagonalEveryXTurns);
            // Add the option for the snake to stay in its current position
            successors.Add(current);
            return successors;
        }
        else // If it's a player's turn
        {
            // Get the player's position
            (int, int) playerMatrixPosition = getPlayerPosition();
            int playerRow = playerMatrixPosition.Item1;
            int playerCol = playerMatrixPosition.Item2;
            // Player going up
            if (playerRow + 1 < rows)
            {
                ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow + 1, playerCol), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                successors.Add(ngs);
            }
            // Player going right
            if (playerCol + 1 < cols)
            {
                ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow, playerCol + 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                successors.Add(ngs);
            }
            // Player going down
            if ((playerRow - 1) >= 0)
            {
                ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow - 1, playerCol), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                successors.Add(ngs);
            }
            // Player going left
            if ((playerCol - 1) >= 0)
            {
                ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow, playerCol - 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                successors.Add(ngs);
            }
            // If the player can use diagonal now
            if (CanUseDiagonal())
            {
                // Player going up and right
                if (playerRow + 1 < rows && playerCol + 1 < cols)
                {
                    ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow + 1, playerCol + 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                    successors.Add(ngs);
                }
                // Player going down and left
                if (playerRow - 1 >= 0 && playerCol - 1 >= 0)
                {
                    ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow - 1, playerCol - 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                    successors.Add(ngs);
                }
                // Player going up and left
                if (playerRow + 1 < rows && playerCol - 1 >= 0)
                {
                    ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow + 1, playerCol - 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                    successors.Add(ngs);
                }
                // Player going down and right
                if (playerRow - 1 >= 0 && playerCol + 1 < cols)
                {
                    ComGameState ngs = new ComGameState(CalculatePlayerNewPosition(playerRow - 1, playerCol + 1), snakeObjects,
                    currentTurn + 1, maxTurns, rows, cols, ladders,
                    ComGameController.Turn.Snake, (turnsSinceLastDiagonal + 1) % DiagonalEveryXTurns, DiagonalEveryXTurns);
                    successors.Add(ngs);
                }
            }
        }
        return successors;
    }


    // This function calculates the next player position. We need to take into account the ladders and snakes
    // for every new position the player lands on and thus the loop.
    // For example: A player can move from 20 to 21 and in 21 there's a snake from 21 to 15, and in 15 there's a ladder from
    // 15 to 31. In that case, the player will end at 31.
    // Note: Ladders get a priority, which means that if there are both a snake and a ladder on some position, the
    // player will choose the ladder at anytime.
    private int CalculatePlayerNewPosition(int newPositionRow, int newPositionCol, List<ComLightweightSnake> snakes = null)
    {
        int newPosition = getIndexPosition((newPositionRow, newPositionCol));
        int i = 0;
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
            foreach (ComLightweightSnake snake in (snakes == null ? snakeObjects : snakes))
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
        return newPosition;
    }

    // Calculates the distance from the player to every snake and returns the index of the closest snake to the player.
    // This is the snake that will play this turn.
    public int getCurrentSnake()
    {
        float value = float.PositiveInfinity;
        int Index = 0;
        // Iterate over all snakes and find the closest snake to the player. Returns the closest snake's index.
        for (int i = 0; i < snakeObjects.Count; i++)
        {
            ComLightweightSnake snake = snakeObjects[i];
            (int, int) snakeStartMatrixPosition = getMatrixPosition(snake.startPos);
            (int, int) playerMatrixPosition = getPlayerPosition();
            float distanceFromPlayerToSnakeStart = getDistance(playerMatrixPosition.Item1, playerMatrixPosition.Item2,
                snakeStartMatrixPosition.Item1, snakeStartMatrixPosition.Item2);
            if (distanceFromPlayerToSnakeStart < value)
            {
                value = distanceFromPlayerToSnakeStart;
                Index = i;
            }
        }
        return Index;
    }

    // This is used as enhancement for our evaluation method. If two states have the same evaluation we'll choose
    // the state that:
    // Maximizes player's distance to goal if it's a snake's turn
    // Minimizes player's distance to goal if it's a player's turn
    // This improved our evaluation method.
    private bool ShouldSwitchGameState(ComGameState g2, ComGameController.Turn currentTurnType)
    {
        (int, int) playerMatrixPosition1 = getMatrixPosition(playerPos);
        float distanceToGoal1 = getDistance(playerMatrixPosition1.Item1, playerMatrixPosition1.Item2,
            rows - 1, cols - 1);
        (int, int) playerMatrixPosition2 = getMatrixPosition(g2.playerPos);
        float distanceToGoal2 = getDistance(playerMatrixPosition2.Item1, playerMatrixPosition2.Item2,
            rows - 1, cols - 1);
        if (currentTurnType == ComGameController.Turn.Snake)
        {
            return distanceToGoal1 < distanceToGoal2;
        }
        else
        {
            return distanceToGoal1 > distanceToGoal2;
        }
    }

    // This is the alpha-beta pruning algorithm we've implemented.
    public (float, ComGameState) AlphaBetaPruning(int d, float alpha, float beta, ComGameController.Turn currentTurnType)
    {
        // If it's a goal state, return its utility value.
        if (IsGoal())
        {
            return (Utility(), this);
        }
        // If we can't keep going since d is zero, return the evaluation value of this state.
        if (d <= 0)
        {
            return (Evaluate(), this);
        }
        int chosenSnake = turnType == ComGameController.Turn.Player ? -1 : currentSnake;
        // Get the sucessors of this state
        List<ComGameState> succ = GenerateSuccessors(chosenSnake);
        if (currentTurnType == turnType)
        {
            (float, ComGameState) maxEval = (float.NegativeInfinity, this);
            // Iterate over the successors and find the maximum value we can use. Prune all states
            // that we can't or won't choose to save critical time.
            foreach (ComGameState child in succ)
            {
                // Recursively call AlphaBetaPruning again with the state's children
                (float, ComGameState) value = child.AlphaBetaPruning(d - 1, alpha, beta, currentTurnType);
                // If we found a better state (either by having a better evaluation or having the same evaluation and distance is better)
                // Set this state as the current best.
                if (maxEval.Item1 < value.Item1 || maxEval.Item1 == value.Item1 && maxEval.Item2.ShouldSwitchGameState(child, turnType))
                {
                    maxEval = (value.Item1, child);
                }
                // Prune all states that we won't or can't choose to save critical computing time.
                alpha = Mathf.Max(alpha, value.Item1);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            (float, ComGameState) minEval = (float.PositiveInfinity, this);
            // Iterate over the successors and find the minimum value we can use. Prune all states
            // that we can't or won't choose to save critical time.
            foreach (ComGameState child in succ)
            {
                // Recursively call AlphaBetaPruning again with the state's children
                (float, ComGameState) value = child.AlphaBetaPruning(d - 1, alpha, beta, currentTurnType);
                // If we found a better state (either by having a better evaluation or having the same evaluation and distance is better)
                // Set this state as the current best.
                if (minEval.Item1 > value.Item1 || minEval.Item1 == value.Item1 && minEval.Item2.ShouldSwitchGameState(child, turnType))
                {
                    minEval = (value.Item1, child);
                }
                // Prune all states that we won't or can't choose to save critical computing time.
                beta = Mathf.Min(beta, value.Item1);
                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }

    // This function deep copies a gameobject list. This is a helper function.
    private List<GameObject> DeepCopyGameObjectList(List<GameObject> originalList)
    {
        List<GameObject> copiedList = new List<GameObject>();
        foreach (GameObject original in originalList)
        {
            GameObject copy = GameObject.Instantiate(original) as GameObject;
            copiedList.Add(copy);
        }
        return copiedList;
    }

    // This function deep copies a positions list. This is a helper function.
    private List<(int, int)> DeepCopyPositionsList(List<(int, int)> originalList)
    {
        List<(int, int)> copiedList = new List<(int, int)>();

        for (int i = 0; i < originalList.Count; i++)
        {
            copiedList.Add((originalList[i].Item1, originalList[i].Item2));
        }

        return copiedList;
    }

    // This function deep copies the snakes list. This is a helper function.
    private List<ComLightweightSnake> DeepCopySnakeList(List<ComLightweightSnake> originalList)
    {
        List<ComLightweightSnake> secondList = new List<ComLightweightSnake>();
        for (int i = 0; i < originalList.Count; i++)
        {
            secondList.Add(new ComLightweightSnake(originalList[i].startPos, originalList[i].endPos));
        }
        return secondList;
    }

    // This function is used as a helper function for sucessors, and it builds the next snake states when the snake
    // doesn't move a ladder.
    private ComGameState SnakeTurnBuildComGameState(int newStartPos, int newEndPos)
    {
        List<ComLightweightSnake> snakes = DeepCopySnakeList(snakeObjects);
        snakes[currentSnake].startPos = newStartPos;
        snakes[currentSnake].endPos = newEndPos;
        List<(int, int)> newLadders = DeepCopyPositionsList(ladders);
        if (newStartPos == playerPos)
        {
            int playerPos = CalculatePlayerNewPosition(getMatrixPosition(newStartPos).Item1, getMatrixPosition(newStartPos).Item2, snakes);
            ComGameState ngs = new ComGameState(playerPos, snakes, currentTurn, maxTurns, rows, cols, newLadders, ComGameController.Turn.Player,
                turnsSinceLastDiagonal, DiagonalEveryXTurns);
            return ngs;
        }
        ComGameState gs = new ComGameState(CalculatePlayerNewPosition(getMatrixPosition(playerPos).Item1, getMatrixPosition(playerPos).Item2, snakes), snakes, currentTurn, maxTurns, rows, cols, newLadders, ComGameController.Turn.Player,
                turnsSinceLastDiagonal, DiagonalEveryXTurns);
        return gs;
    }

    // This function is used as a helper function for sucessors, and it builds the next snake states when the snake
    // moves a ladder.
    private ComGameState SnakeTurnBuildComGameStateMoveLadder(int newStartPos, int newEndPos, (int, int) ladderMoved, (int, int) ladderNewPosition)
    {
        List<ComLightweightSnake> snakes = DeepCopySnakeList(snakeObjects);
        snakes[currentSnake].startPos = newStartPos;
        snakes[currentSnake].endPos = newEndPos;
        if (newStartPos == playerPos)
        {
            int PlayerPosition = CalculatePlayerNewPosition(getMatrixPosition(newStartPos).Item1, getMatrixPosition(newStartPos).Item2, snakes);
            ComGameState ngs = new ComGameState(PlayerPosition, snakes, currentTurn, maxTurns, rows, cols, ladders, ComGameController.Turn.Player,
                turnsSinceLastDiagonal, DiagonalEveryXTurns);
            return ngs;
        }
        List<(int, int)> newLadders = DeepCopyPositionsList(ladders);
        for (int i = 0; i < newLadders.Count; i++)
        {
            if (ladderMoved.Item1 == newLadders[i].Item1 && ladderMoved.Item2 == newLadders[i].Item2)
            {
                newLadders[i] = (ladderNewPosition.Item1, ladderNewPosition.Item2);
            }
        }
        ComGameState gs = new ComGameState(playerPos, snakes, currentTurn, maxTurns, rows, cols, newLadders, ComGameController.Turn.Player,
                turnsSinceLastDiagonal, DiagonalEveryXTurns);
        return gs;
    }

    // Returns true if the player can use diagonal at this turn
    private bool CanUseDiagonal()
    {
        return turnsSinceLastDiagonal == DiagonalEveryXTurns;
    }
}
// This serves as a struct only containg the snakes start and end positions. We don't want to save the whole
// snake gameobject for only two integers so we use this as a "Lightweight Snake".
public class ComLightweightSnake
{
    public int startPos;
    public int endPos;

    public ComLightweightSnake(int start, int end)
    {
        startPos = start;
        endPos = end;
    }
}