using UnityEngine;

public class Square : MonoBehaviour {
    public int row;
    public int column;
    public Piece piece;

    public void OnMouseDown(){
        BoardManager.Instance.SquareSelected(row, column);
    }

    public override string ToString() {
        return row + " " + column;
    }

    public override bool Equals(object other) {
        Square o = other as Square;
        return o != null && (o.row == row && o.column == column);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ row;
            hashCode = (hashCode * 397) ^ column;
            return hashCode;
        }
    }
}
