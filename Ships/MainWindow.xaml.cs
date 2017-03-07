using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ships
{
    public partial class MainWindow : Window
    {
        private Board userBoard;
        private Board computerBoard;
        private const int BOARD_SIZE_IN_PIXELS = 400;
        private const int BOARD_SIZE_IN_CELLS = 10;
        private const int CELL_SIZE_IN_PIXELS = BOARD_SIZE_IN_PIXELS / BOARD_SIZE_IN_CELLS;
        private Board currentBoard;
        private Point _lastCursorPos;
        private bool isUserTurn;
        private bool isEndOfGame;
        private Random rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            userBoard = new Board(BOARD_SIZE_IN_CELLS, BOARD_SIZE_IN_PIXELS, userBoardCanvas);
            computerBoard = new Board(BOARD_SIZE_IN_CELLS, BOARD_SIZE_IN_PIXELS, computerBoardCanvas);

            userBoard.PaintBoard();
            computerBoard.PaintBoard();

            GenerateComputersBoard();
            SettingUp();
            
            computerBoard.HideShips = true;

            // uncomment to show computer's ships
            //computerBoard.HideShips = false; computerBoard.PaintBoard();
        }

        private Ship CreateNewShip(Board board, Point cursorPos)
        {
            Ship oldShip = board.GetNewShip();
            Ship newShip = new Ship();

            int widthInCells = oldShip.IsHorizontal ? oldShip.Size : 1;
            int heightInCells = oldShip.IsHorizontal ? 1 : oldShip.Size;

            Point coords = CoordsPixelToCell(cursorPos);
            if (coords.X + widthInCells > BOARD_SIZE_IN_CELLS)
                coords.X = BOARD_SIZE_IN_CELLS - widthInCells;

            if (coords.Y + heightInCells > BOARD_SIZE_IN_CELLS)
                coords.Y = BOARD_SIZE_IN_CELLS - heightInCells;

            newShip.XCellCoords = (int)coords.X;
            newShip.YCellCoords = (int)coords.Y;
            newShip.IsHorizontal = oldShip.IsHorizontal;
            newShip.Size = oldShip.Size;

            Color shipColor;

            if (board.IsShipPositionAllowed(newShip))
                shipColor = Colors.LightGreen;
            else
                shipColor = Colors.LightCoral;

            if (board.Shooting)
                shipColor = Colors.WhiteSmoke;

            newShip.Rect = board.CreateShipRect(widthInCells, heightInCells, shipColor);
            return newShip;
        }

        private Point CoordsPixelToCell(Point coords)
        {
            Point point = new Point();
            point.X = (int)coords.X / CELL_SIZE_IN_PIXELS;
            point.Y = (int)coords.Y / CELL_SIZE_IN_PIXELS;
            return point;
        }

        private void userBoardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!userBoard.AddingNewShip)
                return;

            Point newCursorPos = e.GetPosition(userBoardCanvas);
            if (!CursorPositionChanged(newCursorPos))
                return;

            CreateShipAndPaintBoard(userBoard, newCursorPos);
        }

        private void computerBoardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!computerBoard.Shooting)
                return;

            Point newCursorPos = e.GetPosition(computerBoardCanvas);
            if (!CursorPositionChanged(newCursorPos))
                return;

            CreateShipAndPaintBoard(computerBoard, newCursorPos);
        }

        private bool CursorPositionChanged(Point newCursorPos)
        {
            bool changed = _lastCursorPos != newCursorPos;
            _lastCursorPos = newCursorPos;

            return changed;
        }

        private void CreateShipAndPaintBoard(Board board, Point cursorPos)
        {
            Ship newShip = CreateNewShip(board, cursorPos);
            board.PrepareNewShip(newShip);
            board.PaintBoard();
        }

        private void userBoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!userBoard.AddingNewShip)
                return;

            AcceptNewShip(userBoard);
            if (userBoard.ShipsCount == 10)
                StartBattle();
        }

        private void AcceptNewShip(Board board)
        {
            board.AcceptNewShip();
            board.PaintBoard();
        }

        private void userBoardCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            RotateShip(userBoard.GetNewShip());
            CreateShipAndPaintBoard(userBoard, e.GetPosition(userBoardCanvas));
        }

        private void RotateShip(Ship ship)
        {
            ship.IsHorizontal = !ship.IsHorizontal;
        }

        private void userShip4_Click(object sender, MouseButtonEventArgs e)
        {
            shipButtonClicked(4, userBoard, sender);
        }

        private void userShip3_Click(object sender, MouseButtonEventArgs e)
        {
            shipButtonClicked(3, userBoard, sender);
        }

        private void userShip2_Click(object sender, MouseButtonEventArgs e)
        {
            shipButtonClicked(2, userBoard, sender);
        }

        private void userShip1_Click(object sender, MouseButtonEventArgs e)
        {
            shipButtonClicked(1, userBoard, sender);
        }

        private void shipButtonClicked(int shipSize, Board board, object shipCanvas)
        {
            board.StartAddingNewShip(shipSize);
            currentBoard = board;
            ((Canvas)shipCanvas).IsEnabled = false;
            ((Canvas)shipCanvas).Background = new SolidColorBrush(Colors.LightGray);
        }

        private void computerBoardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isUserTurn)
                return;

            if (UserShot(e.GetPosition(computerBoardCanvas)) == false)
                ComputerTurn();

            computerBoard.PaintBoard();

            if (computerBoard.AreAllShipsDown())
                EndGame("user");
        }

        private void EndGame(string winner)
        {
            isEndOfGame = true;
            isUserTurn = false;
            lblTurnArrow.Content = "";

            if (winner == "computer")
                lblInfo.Content = "Komputer wygrywa!";
            else if (winner == "user")
                lblInfo.Content = "Wygrywasz grę!";
        }

        private bool UserShot(Point cursorPos)
        {
            Point coords = CoordsPixelToCell(cursorPos);

            if (computerBoard.IsShot(coords))
                return true;

            int shipNumber = ShootShip(coords, computerBoard);

            return shipNumber > 0;
        }

        private void StartBattle()
        {
            UserTurn();
        }

        private void GenerateComputersBoard()
        {
            List<Point> freePositions = new List<Point>();

            for (int i = 0; i < BOARD_SIZE_IN_CELLS; i++)
                for (int j = 0; j < BOARD_SIZE_IN_CELLS; j++)
                    freePositions.Add(new Point(i, j));

            for (int i = 1; i <= 4; i++)
            {
                for (int j = i; j <= 4; j++)
                {
                    computerBoard.AddingNewShip = true;
                    InsertShipToTheBoard(i, freePositions);
                }
            }
        }

        private void InsertShipToTheBoard(int shipSize, List<Point> freePositions)
        {
            Ship newShip = CreateShipAtFreePosition(shipSize, freePositions);

            int widthInCells = (newShip.IsHorizontal) ? shipSize : 1;
            int heightInCells = (newShip.IsHorizontal) ? 1 : shipSize;

            newShip.Rect = computerBoard.CreateShipRect(widthInCells, heightInCells, Colors.LightBlue);
            computerBoard.PrepareNewShip(newShip);
            computerBoard.AcceptNewShip();

            // remove points that aren't free anymore
            BoardArea area = computerBoard.GetShipNeighborhood(newShip);

            for (int i = area.MinX; i < area.MaxX; i++)
                for (int j = area.MinY; j < area.MaxY; j++)
                    freePositions.Remove(new Point(i, j));
        }

        private Ship CreateShipAtFreePosition(int shipSize, List<Point> freePositions)
        {
            List<Point> startPoints = new List<Point>(freePositions);
            Ship newShip = new Ship();
            Point randomPoint;

            do
            {
                int randomIdx = rand.Next(startPoints.Count);
                randomPoint = startPoints.ElementAt(randomIdx);
                newShip.XCellCoords = (int)randomPoint.X;
                newShip.YCellCoords = (int)randomPoint.Y;
                newShip.IsHorizontal = (rand.Next() % 2 == 1) ? true : false;
                newShip.Size = shipSize;

                startPoints.RemoveAt(randomIdx);

                if (computerBoard.IsShipPositionAllowed(newShip))
                    break;

                newShip.IsHorizontal = !newShip.IsHorizontal;
            }
            while (!computerBoard.IsShipPositionAllowed(newShip));

            return newShip;
        }

        private void SettingUp()
        {
            lblInfo.Content = "Rozmieść swoje statki na planszy.";
            lblTurnArrow.Content = "<---";
        }

        private void UserTurn()
        {
            lblInfo.Content = "Wybierz pole do strzału.";
            lblTurnArrow.Content = "--->";
            isUserTurn = true;
            computerBoard.StartShooting();
        }

        private void ComputerTurn()
        {
            lblInfo.Content = "Komputer strzela.";
            lblTurnArrow.Content = "<---";
            isUserTurn = false;

            while (ComputerShot())
            {
                if (userBoard.AreAllShipsDown())
                {
                    EndGame("computer");
                    break;
                }
            }

            userBoard.PaintBoard();

            if (!isEndOfGame)
                UserTurn();
        }

        private bool ComputerShot()
        {
            List<Point> availableCells = GetNextShot(userBoard);
            Point randomCoords = availableCells.ElementAt(rand.Next(availableCells.Count));
            int shipNumber = ShootShip(randomCoords, userBoard);

            return shipNumber > 0;
        }

        private int ShootShip(Point coords, Board board)
        {
            int shipNumber = board.Shot(coords);

            if (shipNumber > 0 && board.IsWholeShipDown(shipNumber))
                board.DisableShipNeighborhood(shipNumber);

            return shipNumber;
        }

        private List<Point> GetNextShot(Board board)
        {
            for (int i = 0; i < BOARD_SIZE_IN_CELLS; i++)
            {
                for (int j = 0; j < BOARD_SIZE_IN_CELLS; j++)
                {
                    if (board.shotBoard[i, j] == false || board.board[i, j] <= 0)
                        continue;

                    List<Point> possibleShots = new List<Point>(4);

                    // 0 - unknown, 1 - horizontal, 2 - vertical
                    int direction = 0;
                    Point leftCell = new Point(i - 1, j);
                    Point rightCell = new Point(i + 1, j);
                    Point topCell = new Point(i, j - 1);
                    Point bottomCell = new Point(i, j + 1);

                    if (ExistsAndIsHit(leftCell, board) || ExistsAndIsHit(rightCell, board))
                            direction = 1;
                    else if (ExistsAndIsHit(topCell, board) || ExistsAndIsHit(bottomCell, board))
                        direction = 2;

                    if (direction == 0)
                    {
                        AddToListIfPossible(leftCell, possibleShots, board);
                        AddToListIfPossible(rightCell, possibleShots, board);
                        AddToListIfPossible(topCell, possibleShots, board);
                        AddToListIfPossible(bottomCell, possibleShots, board);
                    }
                    else if (direction == 1)
                    {
                        AddToListIfPossible(leftCell, possibleShots, board);
                        AddToListIfPossible(rightCell, possibleShots, board);
                    }
                    else
                    {
                        AddToListIfPossible(topCell, possibleShots, board);
                        AddToListIfPossible(bottomCell, possibleShots, board);
                    }

                    if (possibleShots.Count == 0)
                        continue;

                    return possibleShots;
                }
            }


            return userBoard.GetCellsAvailableToShot();
        }

        private bool ExistsAndIsHit(Point coords, Board board)
        {
            return board.AreCoordsCorrect(coords) && board.IsHit(coords);
        }

        private void AddToListIfPossible(Point coords, List<Point> list, Board board)
        {
            if (!board.AreCoordsCorrect(coords))
                return;

            if (board.IsShot(coords))
                return;

            list.Add(coords);
        }
    }
}
