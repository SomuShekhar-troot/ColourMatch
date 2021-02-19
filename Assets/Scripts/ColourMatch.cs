using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourMatch : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;
    public RectTransform killedBoard;

    [Header("prefabs")]
    public GameObject nodePiece;
   public GameObject KilledPiece;

    int width = 9;
    int height = 14;
    int[] fills;
    Node[,] board;

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;


    System.Random random;
    void Start()
    {
        StartGame();
    }

    void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for(int i = 0; i < update.Count; i++)
        {
            NodePiece piece = update[i];
            if (!piece.UpdatePiece())
            {
                finishedUpdating.Add(piece);
            }
        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            FlippedPieces flip = getflipped(piece);
            NodePiece flippedPiece = null;

            int x =(int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] - 1, 0, width);

            List<Point> connected = isConnected(piece.index, true);
            bool wasflipped = (flip != null);

            if (wasflipped)// if we flipped to make a match
            {
                flippedPiece = flip.getOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }
            if (connected.Count == 0)// if we didnt make  a match
            {
                if (wasflipped)//if we flipped
                {

                    FlipPiece(piece.index, flippedPiece.index, false);//flip back

                }
            }
            else// if we need a match
            {
                foreach(Point pnt in connected)//remove the node pieces connected
                {
                    killPiece(pnt);

                    Node node = getNodeAtPoint(pnt);
                    NodePiece nodePiece = node.getPiece();
                    if(nodePiece != null)
                    {
                       nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }

                ApplyGravityToBoard();
            }

            flipped.Remove(flip);//remove the flip after update
            update.Remove(piece);
        }


    }

    void ApplyGravityToBoard()
    {
        for(int x = 0; x < width; x++)
        {
            for(int y = (height - 1); y >= 0; y--)//start at the bottom and grab the next
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val != 0)
                {
                    continue;//if its not a hole in the board,do nothing
                }
                for(int ny = (y - 1); ny >= -1; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextval = getValueAtPoint(next);
                    if(nextval == 0)
                    {
                        continue;
                    }
                    if (nextval != -1)//if we did nt hit an end,but its not 0 then use this to fill the current hole
                    {
                        Node got = getNodeAtPoint(next);
                        NodePiece piece = got.getPiece();

                        //set a hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        //Replace the hole
                        got.SetPiece(null);

                    }
                    else//use deadone or create new pieces to fill holes (hit a-1) only if we choose to
                    {
                        int newVal = fillpiece();
                        NodePiece piece;
                        Point fallPnt = new Point(x, (-1 - fills[x]));
                        if (dead.Count > 0)
                        {
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            piece = revived;

                            dead.RemoveAt(0);
                        }
                        else
                        {
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();
                            piece = n;
                        }

                        piece.Initialize(newVal, p, pieces[newVal - 1]);
                        piece.rect.anchoredPosition = getPositionfromPoint(fallPnt);

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;
                    }

                    break;
                }
            }
        }
    }

    FlippedPieces getflipped(NodePiece p)
    {
        FlippedPieces flip = null;
        for (int i = 0; i < flipped.Count; i++)
        {

            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }
    void StartGame()
    {
        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update =new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        killed = new List<KilledPiece>();

        InitializeBoard();
        VerifyBoard();
        InstanstiateBoard();
    }

    void InitializeBoard()
    {
        board = new Node[width, height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                board[x, y] = new Node((boardLayout.rows[y].row[x]) ? -1:fillpiece() , new Point(x, y));
            }
        }
    }
    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;

                remove = new List<int>();
                while (isConnected(p, true).Count > 0)
                {
                    val = getValueAtPoint(p);
                    if (!remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }

            }
        }
    }
    void InstanstiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x, y));

                int val = node.value;
                if (val <= 0) continue;
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece = p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 - (64 * y));
                piece.Initialize(val, new Point(x, y), pieces[val - 1]);
                node.SetPiece(piece);
            }
        }
    }

    public void ResetPiece(NodePiece piece)
    {
        piece.ResetPosition();
        update.Add(piece);
    }

    public void FlipPiece(Point one, Point two,bool main)
    {
        if (getValueAtPoint(one) < 0) return;


        Node nodeone = getNodeAtPoint(one);
        NodePiece pieceOne = nodeone.getPiece();
        if (getValueAtPoint(two) > 0)
        {
            Node nodetwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodetwo.getPiece();
            nodeone.SetPiece(pieceTwo);
            nodetwo.SetPiece(pieceOne);
            if(main)
                flipped.Add(new FlippedPieces(pieceOne,pieceTwo));

            update.Add(pieceOne);
            update.Add(pieceTwo);
        }
        else
        {
            ResetPiece(pieceOne);
        }
    }
    void killPiece(Point p)
    {
        List<KilledPiece> available = new List<KilledPiece>();
        for(int i = 0; i < killed.Count; i++)
            if (!killed[i].falling) available.Add(killed[i]);

        KilledPiece set = null;
        if (available.Count > 0)
            set = available[0];
        else
        {
            GameObject kill = GameObject.Instantiate(KilledPiece, killedBoard);
            KilledPiece kpiece = kill.GetComponent<KilledPiece>();
            set = kpiece;
            killed.Add(kpiece);
        }

        int val = getValueAtPoint(p) - 1;
        if (set != null && val >= 0 && val < pieces.Length)
            set.Initialize(pieces[val], getPositionfromPoint(p));
    }
    List<Point> isConnected(Point p, bool main)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions =
        {
            Point.up,
            Point.right,
            Point.down,
            Point.left
        };
        foreach (Point dir in directions) //checkig if there is 2 or more same shapes in the directions
        {
            List<Point> line = new List<Point>();
            int same = 0;
            for (int i = 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i));
                if (getValueAtPoint(check) == val)
                {
                    line.Add(check);
                    same++;
                }
            }
            if (same > 1)//if there are more than 1 of the same shape in the direction than we know it is  match
            {
                AddPoints(ref connected, line);//add these points to the overarching connected list
            }
        }
        for (int i = 0; i < 2; i++)//checking if we were in the middle of the two same shapes
        {
            List<Point> line = new List<Point>();
            int same = 0;
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check)// check both sides of the piece,if they are same values, add them to the list
            {
                if (getValueAtPoint(next) == val)
                {
                    line.Add(next);
                    same++;
                }
            }
            if (same > 1)
            {
                AddPoints(ref connected, line);
            }
        }
        for(int i = 0; i < 4; i++)//check for 2*2
        {
            List<Point> square = new List<Point>();
            int same = 0;
            int next = i + 1;
            if (next >= 4)
                next -= 4;
            Point[] check = {Point.add(p,directions[i]), Point.add(p, directions[next]), Point.add(p,Point.add(directions[i], directions[next])) };
            foreach (Point pnt in check)// check both sides of the piece,if they are same values, add them to the list
            {
                if (getValueAtPoint(pnt) == val)
                {
                    square.Add(pnt);
                    same++;
                }
            }
            if (same > 2)
            {
                AddPoints(ref connected, square);
            }
        }
        if (main)//checks before other matches along the current match
        {
            for(int i = 0; i < connected.Count; i++)
            {
                AddPoints(ref connected, isConnected(connected[i], false));
            }
        }
      /*  if (connected.Count > 0)
                                         //unneccessary 
            connected.Add(p);*/
        return connected;
    }
    void AddPoints(ref List<Point> points,List<Point> add)
    {
        foreach(Point p in add)
        {
            bool doAdd = true;
            for(int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(p))
                {
                    doAdd = false;
                    break;
                }
            }
            if (doAdd) points.Add(p);
        }
    }
    int fillpiece()
    {
        int val = 1;
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1;
        return val;
    }
    int getValueAtPoint(Point p)
    {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) return -1;
        return board[p.x, p.y].value;
    }
    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }
    Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }
    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < pieces.Length; i++)
            available.Add(i + 1);
        foreach (int i in remove)
            available.Remove(i);

        if (available.Count <= 0) return 0;
        return available[random.Next(0, available.Count)];
    }

    string getRandomSeed()
    {
        string seed = "";
        string acceptablechars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*~()";
        for (int i = 0; i < 20; i++)
            seed += acceptablechars[Random.Range(0, acceptablechars.Length)];
        return seed;
    }

    public Vector2 getPositionfromPoint(Point p)
    {
       return  new Vector2(32 + (64 *p.x), -32 - (64 *p.y));
    }
}
[System.Serializable]
public class Node
{
    public int value;//0=blank,1=cube,2=sphere,3=cylinder,4=pyramid,5=diamond,-1=hole
    public Point index;
     NodePiece piece;

    public Node(int v,Point i)
    {
        value = v;
        index = i;
    }
    public void SetPiece(NodePiece p)
    {
        piece = p;
        value=(piece==null) ? 0:piece.value;
        if (piece == null) return;
        piece.SetIndex(index);
    }

    public NodePiece getPiece()
    {
        return piece;
    }

}
[System.Serializable]
public class FlippedPieces
{
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o,NodePiece t)
    {
        one = o;
        two = t;
    }

    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p == one)
        {
            return two;
        }
        else if (p == two)
        {
            return one;
        }
        else
            return null;
    }
}
