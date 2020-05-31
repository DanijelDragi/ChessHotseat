using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour {
    public static ReplayManager Instance;

    public GameObject undoButton;
    public GameObject redoButton;
    public GameObject saveNameField;
    public GameObject loadNameField;
    
    private List<Move> _gameMoves;
    private List<Move> _undoneMoves;
    private BoardManager _boardManager;
    private Button _undoButton;
    private Button _redoButton;
    private TMP_InputField _saveNameField;
    private TMP_InputField _loadNameField;

    public void Awake() {
        if (Instance == null) Instance = this;
        else {
            Destroy(this);
            return;
        }
        _gameMoves = new List<Move>();
        _undoneMoves = new List<Move>();
    }

    private void Start() {
        _boardManager = BoardManager.Instance;
        _undoButton = undoButton.GetComponent<Button>();
        _redoButton = redoButton.GetComponent<Button>();
        _undoButton.interactable = false;
        _redoButton.interactable = false;
        _saveNameField = saveNameField.GetComponent<TMP_InputField>();
        _loadNameField = loadNameField.GetComponent<TMP_InputField>();
    }

    public void DoMove(int fromRow, int fromColumn, int toRow, int toColumn, string removedPieceType, string removedPieceColor, bool castle) {
        _gameMoves.Add(new Move(fromRow, fromColumn, toRow, toColumn, removedPieceType, removedPieceColor, castle));
        if (_undoneMoves.Count > 0) {
            _undoneMoves.Clear();
            _redoButton.interactable = false;
        }
        _undoButton.interactable = true;
    }

    public void UndoMove() {
        if (_gameMoves.Count == 0) {
            Debug.Log("No moves to undo!");
            return;
        }
        Move undoneMove = _gameMoves[_gameMoves.Count - 1];
        _undoneMoves.Add(undoneMove);
        _gameMoves.RemoveAt(_gameMoves.Count - 1);
        _boardManager.DoMove(undoneMove.ToRow, undoneMove.ToColumn, 
            undoneMove.FromRow, undoneMove.FromColumn, true, false);
        if (_gameMoves.Count == 0) _undoButton.interactable = false;
        _redoButton.interactable = true;

        if (undoneMove.Castle) {
            int columnChange = undoneMove.ToColumn - undoneMove.FromColumn;
            int columnSign = Math.Sign(columnChange);
            _boardManager.DoMove(undoneMove.ToRow, undoneMove.ToColumn - columnSign, 
                undoneMove.FromRow, columnSign == 1 ? 7 : 0, true, true);
        }
        
        if (undoneMove.RemovedPieceType.Equals("None")) return;
        if (undoneMove.RemovedPieceType.Equals("King")) _boardManager.ResetBoard();
        _boardManager.SummonPiece(undoneMove.ToRow, undoneMove.ToColumn, undoneMove.RemovedPieceType, undoneMove.RemovedPieceColor);
    }

    public void RedoMove() {
        if (_undoneMoves.Count == 0) {
            Debug.Log("No moves to redo!");
            return;
        }
        Move redoneMove = _undoneMoves[_undoneMoves.Count - 1];
        if (!redoneMove.RemovedPieceType.Equals("None")){
            _boardManager.RemovePiece(redoneMove.ToRow, redoneMove.ToColumn, redoneMove.RemovedPieceType, redoneMove.RemovedPieceColor);
        }
        _gameMoves.Add(redoneMove);
        _undoneMoves.RemoveAt(_undoneMoves.Count - 1);
        _boardManager.DoMove(redoneMove.FromRow, redoneMove.FromColumn, redoneMove.ToRow, redoneMove.ToColumn, false, false);
        
        if (redoneMove.Castle) {
            int columnChange = redoneMove.ToColumn - redoneMove.FromColumn;
            int columnSign = Math.Sign(columnChange);
            _boardManager.DoMove(redoneMove.FromRow, columnSign == 1 ? 7 : 0, 
                redoneMove.ToRow, redoneMove.ToColumn - columnSign, false, true);
        }
        
        if (_undoneMoves.Count == 0) _redoButton.interactable = false;
        if (_gameMoves.Count > 0) _undoButton.interactable = true;
    }

    public void RestartGame() {
        int movesToUndo = _gameMoves.Count;
        for (int i = 0; i < movesToUndo; i++) {
            UndoMove();
        }
        _gameMoves = new List<Move>();
        _undoneMoves = new List<Move>();
        _undoButton.interactable = false;
        _redoButton.interactable = false;
        _boardManager.ResetBoard();
    }

    public void SaveReplay() {
        saveNameField.SetActive(false);
        string replayName = _saveNameField.text;
        _saveNameField.text = "";
        if (replayName.Equals("")) {
            Debug.Log("Replay name cannot be empty!");
            return;
        }
        FileStream fs = new FileStream(replayName + ".replay", FileMode.Create);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, _gameMoves);
        fs.Close();
        RestartGame();
    }

    public void LoadReplay() {
        loadNameField.SetActive(false);
        string replayName = _loadNameField.text;
        _loadNameField.text = "";
        if (replayName.Equals("")) {
            Debug.Log("Replay name cannot be empty!");
            return;
        }
        RestartGame();
        using (Stream stream = File.Open(replayName + ".replay", FileMode.Open)) {
            BinaryFormatter bf = new BinaryFormatter();
            _undoneMoves = ((List<Move>)bf.Deserialize(stream));
            _undoneMoves.Reverse();
            _undoButton.interactable = false;
            _redoButton.interactable = true;
        }
    }

    [Serializable]
    private class Move {
        public readonly int FromRow;
        public readonly int FromColumn;
        public readonly int ToRow;
        public readonly int ToColumn;
        public readonly string RemovedPieceType;
        public readonly string RemovedPieceColor;
        public readonly bool Castle;
        
        public Move(int fromRow, int fromColumn, int toRow, int toColumn, string removedPieceType, string removedPieceColor, bool castle) {
            this.FromRow = fromRow;
            this.FromColumn = fromColumn;
            this.ToRow = toRow;
            this.ToColumn = toColumn;
            this.RemovedPieceType = removedPieceType;
            this.RemovedPieceColor = removedPieceColor;
            this.Castle = castle;
        }
    }
}
