using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BoardManager : MonoBehaviour {
    public static BoardManager Instance;

    private const float CameraAnimationDuration = 1;
    private const float PieceDestroyAnimationDuration = 1.5f;
    private const float PieceMoveAnimationDuration = 1;

    public GameObject squarePrefab;
    public GameObject pawnPrefab;
    public GameObject rookPrefab;
    public GameObject knightPrefab;
    public GameObject bishopPrefab;
    public GameObject queenPrefab;
    public GameObject kingPrefab;
    public Material black;
    public Material white;
    public GameObject gameOverButton; 
    
    private readonly Square[][] _squares = new Square[8][];
    private Square _selectedSquare;
    private string _turn = "White";
    private Camera _camera;
    private ReplayManager _replayManager;
    private bool _gameOver;
    private TMP_Text _gameOverButtonText;

    public void Awake() {
        if (Instance == null) Instance = this;
        else {
            Destroy(this);
            return;
        }

        for (int i = 0; i < 8; i++) {
            _squares[i] = new Square[8];
        }
        _camera = Camera.main;
        _gameOverButtonText = gameOverButton.GetComponentInChildren<TMP_Text>();
        Application.targetFrameRate = 30;
    }

    public void Start() {
        _replayManager = ReplayManager.Instance;
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                GameObject newSquare = Instantiate(squarePrefab, transform, true);
                Transform newSquareTransform = newSquare.transform;
                newSquareTransform.position += new Vector3(j, 0, i);
                Square squareScript = newSquare.GetComponent<Square>();
                squareScript.row = i;
                squareScript.column = j;
                _squares[i][j] = squareScript;
                // ReSharper disable once Unity.InefficientPropertyAccess
                Vector3 squarePosition = newSquareTransform.position;
                Material color;
                if (i < 2) color = white;
                else if (i > 5) color = black;
                else continue;
                GameObject newPiece = null;
                switch (i) {
                    case 1:
                    case 6: {
                        newPiece = Instantiate(pawnPrefab, newSquareTransform, true);
                        newPiece.GetComponent<Piece>().type = "Pawn";
                        break;
                    }
                    case 0:
                    case 7: {
                        switch (j) {
                            case 0:
                            case 7: {
                                newPiece = Instantiate(rookPrefab, newSquareTransform, true);
                                newPiece.GetComponent<Piece>().type = "Rook";
                                break;
                            }
                            case 1:
                            case 6: {
                                newPiece = Instantiate(knightPrefab, newSquareTransform, true);
                                newPiece.GetComponent<Piece>().type = "Knight";
                                break;
                            }
                            case 2:
                            case 5: {
                                newPiece = Instantiate(bishopPrefab, newSquareTransform, true);
                                newPiece.GetComponent<Piece>().type = "Bishop";
                                break;
                            }
                            case 4: {
                                newPiece = Instantiate(queenPrefab, newSquareTransform, true);
                                newPiece.GetComponent<Piece>().type = "Queen";
                                break;
                            }
                            case 3: {
                                newPiece = Instantiate(kingPrefab, newSquareTransform, true);
                                newPiece.GetComponent<Piece>().type = "King";
                                break;
                            }
                            default: {
                                Debug.Log("This should never happen!");
                                break;
                            }
                        }
                        break;
                    }
                }
                if (newPiece == null) Debug.Log("New piece is null!");
                else {
                    newPiece.GetComponent<MeshRenderer>().material = color;
                    newPiece.transform.position += new Vector3(squarePosition.x, 0, squarePosition.z);
                    Piece pieceScript = newPiece.GetComponent<Piece>();
                    pieceScript.color = i < 2 ? "White" : "Black";
                    squareScript.piece = pieceScript;
                }
            }
        }
    }

    public void ResetBoard() {
        _gameOver = false;
        gameOverButton.SetActive(false);
    }
    
    public void DoMove(int fromRow, int fromColumn, int toRow, int toColumn, bool isUndo, bool castle) {
        Square destination = _squares[toRow][toColumn];
        Square start = _squares[fromRow][fromColumn];

        Piece piece = start.piece;
        if (!isUndo) piece.movesMade++;
        else piece.movesMade--;
        destination.piece = piece;
        start.piece = null;
        Transform destinationTransform = destination.transform;
        Transform selectedPieceTransform = destination.piece.transform;
        selectedPieceTransform.parent = destinationTransform;
        Vector3 destinationPosition = destinationTransform.position;
        Vector3 startPosition = start.transform.position;
        if (!castle) {
            _turn = _turn.Equals("White") ? "Black" : "White";
            StartCoroutine(MoveCamera());
        }
        StartCoroutine(MovePiece(selectedPieceTransform, 
            new Vector2(destinationPosition.x, destinationPosition.z),
            new Vector2(startPosition.x, startPosition.z)));
    }

    //This method should only ever be used by the ReplayManager
    public void SummonPiece(int row, int column, string pieceType, string pieceColor) {
        Square square = _squares[row][column];
        GameObject prefab = null;
        switch (pieceType) {
            case "Pawn": {
                prefab = pawnPrefab;
                break;
            }
            case "Rook": {
                prefab = rookPrefab;
                break;
            }
            case "Knight": {
                prefab = knightPrefab;
                break;
            }
            case "Bishop": {
                prefab = bishopPrefab;
                break;
            }
            case "Queen": {
                prefab = queenPrefab;
                break;
            }
            case "King": {
                prefab = kingPrefab;
                break;
            }
        }
        Transform squareTransform = square.transform;
        GameObject newPiece = Instantiate(prefab, squareTransform, true);
        Vector3 squarePosition = squareTransform.position;
        newPiece.transform.position = new Vector3(squarePosition.x, newPiece.transform.position.y, squarePosition.z);
        Piece pieceScript = newPiece.GetComponent<Piece>();
        pieceScript.type = pieceType;
        pieceScript.color = pieceColor;
        square.piece = pieceScript;
        newPiece.GetComponent<MeshRenderer>().material = pieceColor.Equals("White") ? white : black;
    }

    //This method should only ever be used by the ReplayManager
    public void RemovePiece(int row, int column, string pieceType, string pieceColor) {
        Square square = _squares[row][column];
        Piece squarePiece = square.piece;
        if (squarePiece.type.Equals(pieceType) && squarePiece.color.Equals(pieceColor)) {
            if (squarePiece.type.Equals("King")) {
                _gameOver = true;
                _gameOverButtonText.text = _turn.ToUpper() + " WON";
                gameOverButton.SetActive(true);
            }
            StartCoroutine(DestroyPiece(squarePiece.gameObject));
        }
    }

    public void SquareSelected(int row, int column) {
        if (_gameOver) return;
        Square newSelectedSquare = _squares[row][column];
        if (_selectedSquare == null) {
            if (newSelectedSquare.piece == null) return;
            if (!newSelectedSquare.piece.color.Equals(_turn)) {
                Debug.Log("Cannot select other players piece! selected color: " + newSelectedSquare.piece.color + ", turn: " + _turn);
                return;
            }
            _selectedSquare = newSelectedSquare;
            _selectedSquare.GetComponent<MeshRenderer>().enabled = true;
        }
        else if (newSelectedSquare.Equals(_selectedSquare)) {
            _selectedSquare.GetComponent<MeshRenderer>().enabled = false;
            _selectedSquare = null;
        }
        else {
            if (!AttemptMove(newSelectedSquare)) return;
            //If move was legal
            if (_gameOver) {
                _gameOverButtonText.text = _turn.ToUpper() + " WON";
                gameOverButton.SetActive(true);
                    
            }
            StartCoroutine(MoveCamera());
            _turn = _turn.Equals("White") ? "Black" : "White";
            _selectedSquare.GetComponent<MeshRenderer>().enabled = false;
            _selectedSquare = null;
        }
    }

    private bool AttemptMove(Square destination) {
        if (_selectedSquare == null) throw new NullReferenceException("selected square null while attempting move!");
        Piece selectedPiece = _selectedSquare.piece;
        int rowChange = destination.row - _selectedSquare.row;
        int columnChange = destination.column - _selectedSquare.column;
        bool moveLegal = false;
        bool castle = false;
        switch (selectedPiece.type) {
            case "Pawn": {
                int movementDirection = selectedPiece.color == "White" ? 1 : -1;
                if (rowChange == 2 * movementDirection && columnChange == 0 && selectedPiece.movesMade == 0 &&
                    (_squares[destination.row - movementDirection][destination.column].piece == null)) {
                    //this allows first step of size 2
                }
                else if (rowChange != movementDirection) {
                        Debug.Log("Invalid move, pawn can only move forwards, by 1 or at first 2 squares!");
                        return false;
                }
                if (columnChange == 0) {
                    //Move pawn forwards
                    if (destination.piece == null) {
                        moveLegal = true;
                        break;
                    }
                    Debug.Log("Invalid move, a piece is blocking your way!");
                    return false;
                }
                if (columnChange == 1 || columnChange == -1) {
                    //move diagonally only on top of other pieces!
                    if (destination.piece != null && destination.piece.color != _turn) {
                        moveLegal = true;
                        break;
                    }
                    Debug.Log("Invalid move, no pieces to attack here!");
                    return false;
                }
                Debug.Log("Pawn can only move 1 square diagonally!");
                return false;
            }
            case "Rook": {
                if (rowChange != 0 && columnChange != 0) {
                    Debug.Log("Rook can only move up, down, left, and right!");
                    return false;
                }
                if (rowChange == 0) {
                    int row = _selectedSquare.row;
                    int sign = Math.Sign(columnChange);
                    for (int column = _selectedSquare.column + sign; 
                        Math.Abs(column - _selectedSquare.column) < Math.Abs(columnChange); column += sign) {
                        if (_squares[row][column].piece == null) continue;
                        Debug.Log("Pieces in the way, cannot go over them!" + row + ", " + column);
                        return false;
                    }
                }
                if (columnChange == 0) {
                    int column = _selectedSquare.column;
                    int sign = Math.Sign(rowChange);
                    for (int row = _selectedSquare.row + sign; Math.Abs(row - _selectedSquare.row) < Math.Abs(rowChange); row += sign) {
                        if (_squares[row][column].piece == null) continue;
                        Debug.Log("Pieces in the way of rook!" + row + ", " + column);
                        return false;
                    }
                }
                //Move is legal
                moveLegal = true;
                break;
            }
            case "Bishop": {
                if (Math.Abs(rowChange) != Math.Abs(columnChange)) {
                    Debug.Log("Bishops can only move diagonally!");
                    return false;
                }
                int rowSign = Math.Sign(rowChange);
                int columnSign = Math.Sign(columnChange);
                for (int inc = 1; inc < Math.Abs(rowChange); inc++) {
                    if (_squares[_selectedSquare.row + rowSign * inc][_selectedSquare.column + columnSign * inc].piece == null) continue;
                    return false;
                }
                //Move legal
                moveLegal = true;
                break;
            }
            case "Knight": {
                if (!((Math.Abs(rowChange) == 2 && Math.Abs(columnChange) == 1) ||
                      (Math.Abs(rowChange) == 1 && Math.Abs(columnChange) == 2))) {
                    Debug.Log("Knight can only move in an L patter!");
                    return false;
                }
                moveLegal = true;
                break;
            }
            case "Queen": {
                if (!((Math.Abs(rowChange) == Math.Abs(columnChange)) || rowChange == 0 || columnChange == 0)) {
                    Debug.Log("Queen can only move in the 8 main directions!");
                    return false;
                }
                int rowSign = Math.Sign(rowChange);
                int columnSign = Math.Sign(columnChange);
                for (int inc = 1; inc < Math.Abs(rowChange); inc++) {
                    if (_squares[_selectedSquare.row + rowSign * inc][_selectedSquare.column + columnSign * inc].piece == null) continue;
                    Debug.Log("Pieces in the way of Queen!");
                    return false;
                }
                moveLegal = true;
                break;
            }
            case "King": {
                if (Math.Abs(rowChange) > 1 || Math.Abs(columnChange) > 1) {
                    if (rowChange == 0 && Math.Abs(columnChange) == 2 && _selectedSquare.row % 7 == 0 && selectedPiece.movesMade == 0) {
                        //Allow castling
                        int rookColumn = columnChange == 2 ? 7 : 0;
                        Square rookSquare = _squares[_selectedSquare.row][rookColumn];
                        if (rookSquare.piece != null && rookSquare.piece.type.Equals("Rook") && rookSquare.piece.movesMade == 0) {
                            //King and rook both there
                            for (int col = _selectedSquare.column + Math.Sign(columnChange);
                                col == 1 || col == 6; col += Math.Sign(columnChange)) {
                                if (_squares[_selectedSquare.row][col].piece == null) continue;
                                Debug.Log("Pieces in the way, cannot castle!");
                                return false;
                            }
                            //If this is reached the move is legal, do castle;
                            castle = true;
                        }
                        else {
                            Debug.Log("King cannot castle without rook!");
                            return false;
                        }
                    }
                    else {
                        Debug.Log("King can only move 1 square in each direction!");
                        return false;
                    }
                }
                for (int i = -1; i < 2; i++) {
                    for (int j = -1; j < 2; j++) {
                        int rowIndex = Mathf.Clamp(destination.row + i, 0, 7);
                        int columnIndex = Mathf.Clamp(destination.column + j, 0, 7);
                        Piece temp = _squares[rowIndex][columnIndex].piece;
                        if ( temp == null || temp.type != "King" || temp.color == _turn) continue;
                        Debug.Log("King can't move close to the opposing king!");
                        return false;
                    }
                }
                moveLegal = true;
                if (castle) {
                    DoMove(_selectedSquare.row, columnChange == 2 ? 7 : 0, 
                        _selectedSquare.row, destination.column - Math.Sign(columnChange), 
                        false, true);
                }
                break;
            }
            default:
                Debug.Log("This should never happen!");
                break;
        }
        //return false if this move is not legal
        if (!moveLegal) return false;
        
        selectedPiece.movesMade++;
        string destroyedPieceType = "None";
        string destroyedPieceColor = "None";
        if (destination.piece != null) {
            if (destination.piece.color == _turn) {
                Debug.Log("Cannot destroy own piece!");
                return false;
            }
            destroyedPieceType = destination.piece.type;
            destroyedPieceColor = destination.piece.color;
            if (destination.piece.type.Equals("King")) _gameOver = true;
            StartCoroutine(DestroyPiece(destination.piece.gameObject));
        }
        destination.piece = selectedPiece;
        _selectedSquare.piece = null;
        Transform destinationTransform = destination.transform;
        Transform selectedPieceTransform = selectedPiece.transform;
        selectedPieceTransform.parent = destinationTransform;
        Vector3 destinationPosition = destinationTransform.position;
        Vector3 startSquarePosition = _selectedSquare.transform.position;
        _replayManager.DoMove(_selectedSquare.row, _selectedSquare.column, 
            destination.row, destination.column, destroyedPieceType, destroyedPieceColor, castle);
        StartCoroutine(MovePiece(selectedPieceTransform, 
            new Vector2(destinationPosition.x, destinationPosition.z), 
            new Vector2(startSquarePosition.x, startSquarePosition.z)));
        return true;
    }

    private IEnumerator MoveCamera() {
        float elapsed = 0;
        Transform cameraTransform = _camera.transform;
        while (elapsed < CameraAnimationDuration) {
            float step = Time.deltaTime;
            elapsed += step;
            if (elapsed > CameraAnimationDuration) {
                step -= elapsed - CameraAnimationDuration;
            }
            cameraTransform.RotateAround(Vector3.zero, Vector3.up, 180 * step);
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    private static IEnumerator DestroyPiece(GameObject piece) {
        float elapsed = 0;
        Transform pieceTransform = piece.transform;
        while (elapsed < PieceDestroyAnimationDuration) {
            float step = Time.deltaTime;
            elapsed += step;
            if (elapsed > PieceDestroyAnimationDuration) {
                step -= elapsed - PieceDestroyAnimationDuration;
            }
            pieceTransform.position += new Vector3(0, 3 * (step / PieceDestroyAnimationDuration), 0);
            yield return new WaitForEndOfFrame();
        }
        Destroy(piece);
        yield return null;
    }

    private static IEnumerator MovePiece(Transform piece, Vector2 destination, Vector2 start) {
        float elapsed = 0;
        Vector3 diff = new Vector3(destination.x - start.x, 0, destination.y - start.y);
        while (elapsed < PieceMoveAnimationDuration) {
            float step = Time.deltaTime;
            elapsed += step;
            if (elapsed > PieceMoveAnimationDuration) {
                step -= elapsed - PieceMoveAnimationDuration;
            }
            piece.position += step * diff / PieceMoveAnimationDuration;
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }
}
