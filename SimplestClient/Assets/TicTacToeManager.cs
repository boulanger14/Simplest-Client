using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class TicTacToeManager : MonoBehaviour
{
    GameObject playerSymbolText,opponentSymbolText, turnIndicatorText, characterSelectionPanel, xButton, oButton, roomNumberText;

    NetworkedClient connectionToHost;
    
    List<TicTacToeSquareBehaviour> ticTacToeSquares;

    string playerIcon, opponentIcon;

    bool isPlayersTurn = false;
    bool isGameOver = false;
    bool isObserver = false;

    const int three = 3;

    int turnCount = 0;

    // Start is called before the first frame update
    void Awake()
    {
        ticTacToeSquares = new List<TicTacToeSquareBehaviour>(GetComponentsInChildren<TicTacToeSquareBehaviour>());

        foreach(TicTacToeSquareBehaviour square in ticTacToeSquares)
        {
            square.OnSquarePressed += OnTicTacToeSquarePressed;
        }

        foreach(GameObject go in FindObjectsOfType<GameObject>())
        {
            if(go.name == "PlayerSymbolText")
                playerSymbolText = go;
            else if(go.name == "OpponentSymbolText")
                opponentSymbolText = go;
            else if(go.name == "TurnIndicatorText")
                turnIndicatorText = go;
            else if(go.name == "CharacterSelection")
                characterSelectionPanel = go;
            else if(go.name == "X Button")
                xButton = go;
            else if(go.name == "O Button")
                oButton = go;
            else if(go.name == "RoomNumberText")
                roomNumberText = go;

        }

        xButton.GetComponent<Button>().onClick.AddListener(XButtonPressed);
        oButton.GetComponent<Button>().onClick.AddListener(OButtonPressed);
       
    }

    private void OnEnable()
    {
        if(ticTacToeSquares != null)
            foreach (TicTacToeSquareBehaviour square in ticTacToeSquares)
            {
                square.OnSquarePressed += OnTicTacToeSquarePressed;
            }
        if(xButton != null)
            xButton.GetComponent<Button>().onClick.AddListener(XButtonPressed);
        if(oButton != null)
            oButton.GetComponent<Button>().onClick.AddListener(OButtonPressed);
    }
    private void OnDisable()
    {
        if (ticTacToeSquares != null)
            foreach (TicTacToeSquareBehaviour square in ticTacToeSquares)
            {
                square.OnSquarePressed -= OnTicTacToeSquarePressed;
            }
        if (xButton != null)
            xButton.GetComponent<Button>().onClick.RemoveListener(XButtonPressed);
        if (oButton != null)
            oButton.GetComponent<Button>().onClick.RemoveListener(OButtonPressed);
    }


    private void OnTicTacToeSquarePressed(TicTacToeSquareBehaviour square)
    {
        if(playerIcon == "" || !isPlayersTurn) //player hasn't picked their symbol yet or it isn't their turn, they cant claim a square yet
            return;

        isPlayersTurn = false;
        turnIndicatorText.GetComponent<Text>().text = "It's your opponent's turn";

        square.ClaimSquare(playerIcon);
        if(connectionToHost != null)
            connectionToHost.SendMessageToHost(ClientToServerSignifiers.SelectedTicTacToeSquare + "," + square.ID);

        CheckForLineOfThree(square.row, square.column);
        CheckForTie();
    }

    //checks the row, column and two diagonals to see if theres a winning line of three
   void CheckForLineOfThree(int rowToCheck, int colToCheck)
    {
        int rowCount, colCount, diagonal1Count, diagonal2Count;
        rowCount = colCount = diagonal1Count = diagonal2Count = 0;

        foreach(TicTacToeSquareBehaviour s in ticTacToeSquares)
        {
            if(s.isSquareTaken == false || s.icon == opponentIcon)
                continue;

            if(s.row == rowToCheck)
                rowCount++;
            if(s.column == colToCheck)
                colCount++;
            if(s.diagonal1)
                diagonal1Count++;
            if(s.diagonal2)
                diagonal2Count++;
        }

        if(rowCount == three || colCount == three || diagonal1Count == three || diagonal2Count == three)
        {
            //win
            OnGameOver("You Won!");
            connectionToHost.SendMessageToHost(ClientToServerSignifiers.EndingTheGame + "," + "Game over, you lost");
        }
        
    }

    public void OpponentTookTurn(int squareID)
    {
        foreach(TicTacToeSquareBehaviour s in ticTacToeSquares)
        {
            if(s.ID == squareID)
                s.ClaimSquare(opponentIcon);
        }
        
        if(!isObserver)
        { 
            isPlayersTurn = true;
            turnIndicatorText.GetComponent<Text>().text = "It's your turn";
        }
        else
        {
            ObserverChangeSquare(squareID);
        }
    }

    public void OnGameOver(string endingMsg)
    {
        turnIndicatorText.GetComponent<Text>().text = endingMsg;

        if(isObserver)
            turnIndicatorText.GetComponent<Text>().text = "the game has ended";
        //enable ui for replay
        ChangeState(TicTacToeStates.GameOver);
    }

    public void SetNetworkConnection(NetworkedClient networkClient)
    {
        connectionToHost = networkClient;
    }


    void XButtonPressed()
    {
        CharacterSelected("X", "O");
    }
    void OButtonPressed()
    {
        CharacterSelected("O", "X");
    }

    void CharacterSelected(string symbol, string otherSymbol)
    {
        playerIcon = symbol;
        opponentIcon = otherSymbol;

        playerSymbolText.GetComponent<Text>().text = "You Are: " + symbol;
        opponentSymbolText.GetComponent<Text>().text = "Opponent is: " + otherSymbol;

        characterSelectionPanel.SetActive(false);
        turnIndicatorText.SetActive(true);

        //check if the other player made a choice before your icons were set
        foreach(TicTacToeSquareBehaviour s in ticTacToeSquares)
        {
            if(s.isSquareTaken)
                s.ClaimSquare(opponentIcon);
        }
    }

    public void ChosenAsPlayerOne()
    {
        isPlayersTurn = true;
        turnIndicatorText.GetComponent<Text>().text = "It's your turn";
    }


    private void CheckForTie()
    {
        int takenTileCount = 0;
        foreach(TicTacToeSquareBehaviour s in ticTacToeSquares)
        {
            if(s.isSquareTaken)
                takenTileCount++;
        }

        if(takenTileCount >= 9 && isGameOver == false)
        {
            connectionToHost.SendMessageToHost(ClientToServerSignifiers.EndingTheGame + "," + "No Squares Left. You tied");
            OnGameOver("No squares left. You tied");
        }
    }

    public void SetRoomNumberText(string roomNumber)
    {
        roomNumberText.GetComponent<Text>().text = "Room: " + roomNumber;
    }

    public void EnterGameAsObserver(string[] csv_TurnsSoFar)
    {
        ChangeState(TicTacToeStates.Observing);

        //update already taken squares
        foreach(string index in csv_TurnsSoFar)
        {
            int squareIndex = int.Parse(index);
            ObserverChangeSquare(squareIndex);
        }

        if(isGameOver)
            ChangeState(TicTacToeStates.GameOver);
    }

    void ObserverChangeSquare(int squareID)
    {
        if (turnCount++ % 2 == 0)
            ticTacToeSquares[squareID].ClaimSquare(playerIcon);
        else
            ticTacToeSquares[squareID].ClaimSquare(opponentIcon);
    }

    public bool IsSafeToLeaveTheRoom()
    {
        return (isObserver || isGameOver);
    }

    private void ResetGameState()
    {
        playerSymbolText.GetComponent<Text>().text = "You Are: " ;
        opponentSymbolText.GetComponent<Text>().text = "Opponent is: " ;

        foreach (TicTacToeSquareBehaviour s in ticTacToeSquares)
        {
            s.ResetSquare();
        }
        turnIndicatorText.GetComponent<Text>().text = "It's you're opponent's turn";
        turnIndicatorText.SetActive(false);
        turnCount = 0;
    }

    public void ChangeState(int state)
    {
        isPlayersTurn = false;

        if(state == TicTacToeStates.StartingGame)
        {
            ResetGameState();
           
            isGameOver = false;
            opponentSymbolText.SetActive(true);
            characterSelectionPanel.SetActive(true);

            isObserver = false;
        }
        else if(state == TicTacToeStates.Observing)
        {
            ResetGameState();
            playerIcon = "X";
            opponentIcon = "O";
            opponentSymbolText.SetActive(false);
            characterSelectionPanel.SetActive(false);

            playerSymbolText.GetComponent<Text>().text = "You Are: Observing";

            isObserver = true;
        }
        else if(state == TicTacToeStates.GameOver)
        {
            isGameOver = true;
            turnIndicatorText.SetActive(true);
            //enable replay 
        }
    }
}


public class TicTacToeStates
{
    public const int StartingGame = 1;
    public const int Observing = 2;
    public const int GameOver = 3;
}