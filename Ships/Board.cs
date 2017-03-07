using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ships
{
    class Board
    {
        public int ShipsCount { get; set; }
        public bool AddingNewShip { get; set; }
        public bool HideShips { get; set; }
        public bool Shooting { get; set; }

        public int[,] board;
        public bool[,] shotBoard;
        private Canvas canvas;
        private Ship newShip = new Ship();
        
        private int boardSizeInCells;
        private int boardSizeInPixels;
        private int cellSizeInPixels;

        public Board(int sizeInCells, int sizeInPixels, Canvas canvas)
        {
            board = new int[sizeInCells, sizeInCells];
            shotBoard = new bool[sizeInCells, sizeInCells];
            this.canvas = canvas;
            boardSizeInCells = sizeInCells;
            boardSizeInPixels = sizeInPixels;
            cellSizeInPixels = sizeInPixels / sizeInCells;
        }

        public void PaintBoard()
        {
            if (canvas == null)
                return;

            canvas.Children.Clear();

            // paint aimed cell
            if (Shooting)
                PaintNewShip();

            // paint ships from the board
            if (!HideShips)
                PaintShips();

            // paint new ship
            if (AddingNewShip)
                PaintNewShip();

            PaintShotCells();

            PaintLines(canvas);
        }

        private void PaintShips()
        {
            for (int i = 0; i < boardSizeInCells; i++)
            {
                for (int j = 0; j < boardSizeInCells; j++)
                {
                    if (board[i, j] == 0)
                        continue;
                    
                    AddSingleCell(i, j, Colors.SkyBlue);
                }
            }
        }

        private void PaintNewShip()
        {
            canvas.Children.Add(newShip.Rect);
            Canvas.SetTop(newShip.Rect, newShip.YCellCoords * cellSizeInPixels);
            Canvas.SetLeft(newShip.Rect, newShip.XCellCoords * cellSizeInPixels);
        }

        private void PaintShotCells()
        {
            for (int i = 0; i < boardSizeInCells; i++)
            {
                for (int j = 0; j < boardSizeInCells; j++)
                {
                    if (shotBoard[i, j] == false)
                        continue;

                    Color cellColor;

                    if (board[i, j] > 0)
                        cellColor = Colors.OrangeRed;
                    else
                        cellColor = Colors.LightGray;

                    AddSingleCell(i, j, cellColor);
                }
            }
        }

        private void AddSingleCell(int xCell, int yCell, Color color)
        {
            Rectangle cell = CreateShipRect(1, 1, color);
            canvas.Children.Add(cell);
            Canvas.SetTop(cell, yCell * cellSizeInPixels);
            Canvas.SetLeft(cell, xCell * cellSizeInPixels);
        }

        public void StartAddingNewShip(int size)
        {
            AddingNewShip = true;
            newShip.Size = size;
        }

        public void StartShooting()
        {
            Shooting = true;
            newShip.Size = 1;
        }

        public void PrepareNewShip(Ship newShip)
        {
            this.newShip = newShip;
        }

        public bool IsShipPositionAllowed(Ship ship)
        {
            if (ship.IsHorizontal)
            {
                if (ship.XCellCoords + ship.Size > boardSizeInCells)
                    return false;
                if (ship.YCellCoords + 1 > boardSizeInCells)
                    return false;
            }
            else
            {
                if (ship.XCellCoords + 1 > boardSizeInCells)
                    return false;
                if (ship.YCellCoords + ship.Size > boardSizeInCells)
                    return false;
            }

            BoardArea area = GetShipNeighborhood(ship);

            for (int i = area.MinX; i < area.MaxX; i++)
            {
                for (int j = area.MinY; j < area.MaxY; j++)
                {
                    if (board[i, j] > 0)
                        return false;
                }
            }

            return true;
        }

        public bool AreAllShipsDown()
        {
            for (int i = 0; i < boardSizeInCells; i++)
                for (int j = 0; j < boardSizeInCells; j++)
                    if (board[i, j] > 0 && shotBoard[i, j] == false)
                        return false;

            return true;
        }

        public BoardArea GetShipNeighborhood(Ship ship)
        {
            BoardArea neighborhood = new BoardArea();

            neighborhood.MaxX = ship.XCellCoords + 1;
            neighborhood.MaxY = ship.YCellCoords + 1;

            if (ship.IsHorizontal)
            {
                neighborhood.MaxX += ship.Size;
                neighborhood.MaxY++;
            }
            else
            {
                neighborhood.MaxY += ship.Size;
                neighborhood.MaxX++;
            }

            neighborhood.MaxX = (neighborhood.MaxX > boardSizeInCells) ? boardSizeInCells : neighborhood.MaxX;
            neighborhood.MaxY = (neighborhood.MaxY > boardSizeInCells) ? boardSizeInCells : neighborhood.MaxY;

            neighborhood.MinX = ship.XCellCoords - 1;
            neighborhood.MinX = (neighborhood.MinX < 0) ? 0 : neighborhood.MinX;

            neighborhood.MinY = ship.YCellCoords - 1;
            neighborhood.MinY = (neighborhood.MinY < 0) ? 0 : neighborhood.MinY;

            return neighborhood;
        }

        public List<Point> GetCellsAvailableToShot()
        {
            List<Point> availableCells = new List<Point>();

            for (int i = 0; i < boardSizeInCells; i++)
            {
                for (int j = 0; j < boardSizeInCells; j++)
                {
                    Point p = new Point(i, j);

                    if (!IsShot(p))
                        availableCells.Add(p);
                }
            }

            return availableCells;
        }

        public void AcceptNewShip()
        {
            if (!AddingNewShip)
                return;
            if (!IsShipPositionAllowed(newShip))
                return;

            AddingNewShip = false;
            AddShip();
        }

        public Ship GetNewShip()
        {
            return newShip;
        }

        public bool AreCoordsCorrect(Point coords)
        {
            int x = (int)coords.X;
            int y = (int)coords.Y;

            if (x < 0 || x >= boardSizeInCells)
                return false;
            if (y < 0 || y >= boardSizeInCells)
                return false;

            return true;
        }

        public Rectangle CreateShipRect(int widthInCells, int heightInCells, Color color)
        {
            SolidColorBrush shipBrush = new SolidColorBrush(color);
            Rectangle shipRect = new Rectangle();
            shipRect.Fill = shipBrush;
            shipRect.Stroke = shipBrush;
            shipRect.Width = widthInCells * cellSizeInPixels;
            shipRect.Height = heightInCells * cellSizeInPixels;

            return shipRect;
        }

        public bool IsShot(Point coords)
        {
            return shotBoard[(int)coords.X, (int)coords.Y];
        }

        public bool IsHit(Point coords)
        {
            return IsShot(coords) && board[(int)coords.X, (int)coords.Y] > 0;
        }

        public int Shot(Point coords)
        {
            int x = (int)coords.X;
            int y = (int)coords.Y;
            shotBoard[x, y] = true;
            return board[x, y];
        }

        public bool IsWholeShipDown(int shipNumber)
        {
            for (int i = 0; i < boardSizeInCells; i++)
                for (int j = 0; j < boardSizeInCells; j++)
                    if (board[i, j] == shipNumber && shotBoard[i, j] == false)
                        return false;

            return true;
        }

        public void DisableShipNeighborhood(int shipNumber)
        {
            Ship shotShip = GetShipFromItsNumber(shipNumber);
            BoardArea area = GetShipNeighborhood(shotShip);

            for (int i = area.MinX; i < area.MaxX; i++)
            {
                for (int j = area.MinY; j < area.MaxY; j++)
                {
                    shotBoard[i, j] = true;
                }
            }
        }

        private Ship GetShipFromItsNumber(int shipNumber)
        {
            Ship ship = new Ship();
            Point startCoords = GetShipStartCoords(shipNumber);
            bool isHorizontal = IsShipHorizontal(startCoords);
            int shipSize = GetShipSize(startCoords, isHorizontal);

            ship.XCellCoords = (int)startCoords.X;
            ship.YCellCoords = (int)startCoords.Y;
            ship.Size = shipSize;
            ship.IsHorizontal = isHorizontal;

            return ship;
        }

        private Point GetShipStartCoords(int shipNumber)
        {
            for (int i = 0; i < boardSizeInCells; i++)
                for (int j = 0; j < boardSizeInCells; j++)
                    if (board[i, j] == shipNumber)
                        return new Point(i, j);

            return new Point(0, 0);
        }

        private bool IsShipHorizontal(Point startCoords)
        {
            int x = (int)startCoords.X;
            int y = (int)startCoords.Y;

            if (x + 1 >= boardSizeInCells)
                return false;
            if (y + 1 >= boardSizeInCells)
                return true;
            if (board[x, y] == board[x + 1, y])
                return true;
            if (board[x, y] == board[x, y + 1])
                return false;

            return true;
        }

        private int GetShipSize(Point startCoords, bool isHorizontal)
        {
            int x = (int)startCoords.X;
            int y = (int)startCoords.Y;

            int size = 1;
            
            if (isHorizontal)
            {
                while (x + 1 < boardSizeInCells)
                {
                    if (board[x, y] != board[x + 1, y])
                        break;

                    size++;
                    x++;
                }
            }
            else
            {
                while (y + 1 < boardSizeInCells)
                {
                    if (board[x, y] != board[x, y + 1])
                        break;

                    size++;
                    y++;
                }
            }

            return size;
        }

        private void PaintLines(Canvas board)
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.Gray);

            for (int i = 0; i <= 10; i++)
            {
                Line line = new Line();
                line.Stroke = brush;
                line.X1 = i * cellSizeInPixels;
                line.Y1 = 0;
                line.X2 = i * cellSizeInPixels;
                line.Y2 = boardSizeInPixels;

                board.Children.Add(line);
            }

            for (int i = 0; i <= 10; i++)
            {
                Line line = new Line();
                line.Stroke = brush;
                line.Y1 = i * cellSizeInPixels;
                line.X1 = 0;
                line.Y2 = i * cellSizeInPixels;
                line.X2 = boardSizeInPixels;

                board.Children.Add(line);
            }
        }

        private void AddShip()
        {
            ShipsCount++;

            int maxI = newShip.XCellCoords;
            int maxJ = newShip.YCellCoords;

            if (newShip.IsHorizontal)
            {
                maxI += newShip.Size;
                maxJ++;
            }
            else
            {
                maxJ += newShip.Size;
                maxI++;
            }

            for (int i = newShip.XCellCoords; i < maxI; i++)
                for (int j = newShip.YCellCoords; j < maxJ; j++)
                    board[i, j] = ShipsCount;
        }
    }
}
