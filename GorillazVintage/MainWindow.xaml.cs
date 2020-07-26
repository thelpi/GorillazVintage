using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GorillazVintage
{
    public partial class MainWindow : Window
    {
        private const double BUILDING_WINDOW_SIZE = 12;
        private const double BUILDING_WINDOW_LINE_COUNT = 4;
        private const double HEIGHT = 600;
        private const double WIDTH = 800;
        private const double MAX_BUILDING_HEIGHT_RATE = 0.8;
        private const double MIN_BUILDING_HEIGHT_RATE = 0.2;
        private const double BUILDING_WIDTH_RATE = 0.1;
        private const double BUILDING_SPREAD_RATE = 0.1;
        private const string BUILDING_TAG = "BUILDING";

        private static readonly double WINDOW_WIDTH_FIRST_TO_LAST = ((BUILDING_WINDOW_LINE_COUNT * BUILDING_WINDOW_SIZE) + ((BUILDING_WINDOW_LINE_COUNT - 1) * (BUILDING_WINDOW_SIZE * 0.5)));
        private static readonly double BUILDING_WIDTH = WIDTH * BUILDING_WIDTH_RATE;
        private static readonly int BUILDING_COUNT = (int)(1 / BUILDING_WIDTH_RATE);

        private static readonly Random _rdm = new Random();
        private readonly List<double> _buildingsHeightInfo = new List<double>();

        public MainWindow()
        {
            InitializeComponent();

            CvsMain.Width = WIDTH;
            CvsMain.Height = HEIGHT;

            SetBuildings();

            DrawMainCanvasBuildings();
        }

        private void SetBuildings()
        {
            _buildingsHeightInfo.Clear();
            
            var formerBuildingRate = Double.NaN;
            for (int i = 0; i < BUILDING_COUNT; i++)
            {
                var buildingHeightRate = _rdm.NextDouble();
                while (!IsInBuildingHeightRange(formerBuildingRate, buildingHeightRate))
                {
                    buildingHeightRate = _rdm.NextDouble();
                }
                _buildingsHeightInfo.Add(buildingHeightRate * HEIGHT);
                formerBuildingRate = buildingHeightRate;
            }
        }

        private static bool IsInBuildingHeightRange(double formerBuildingRate, double buildingHeightRate)
        {
            return buildingHeightRate > MIN_BUILDING_HEIGHT_RATE
                && buildingHeightRate < MAX_BUILDING_HEIGHT_RATE
                && (
                    Double.IsNaN(formerBuildingRate)
                    || Math.Abs(formerBuildingRate - buildingHeightRate) > BUILDING_SPREAD_RATE
                );
        }

        private void DrawMainCanvasBuildings()
        {
            var formerBuldings = GetChildrenByTypeAndTag<Canvas>(CvsMain, BUILDING_TAG);

            foreach (var formerBulding in formerBuldings)
            {
                CvsMain.Children.Remove(formerBulding);
            }

            foreach (var building in CreateBuildings(_buildingsHeightInfo))
            {
                CvsMain.Children.Add(building);
            }
        }

        private static IEnumerable<Canvas> CreateBuildings(List<double> buildingsHeightInfo)
        {
            var currentBuildingIndex = 0;
            foreach (var buildingHeight in buildingsHeightInfo)
            {
                yield return DrawBuilding(currentBuildingIndex, buildingHeight);

                currentBuildingIndex++;
            }
        }

        private static Canvas DrawBuilding(int currentBuildingIndex, double buildingHeight)
        {
            var buildingCanvas = new Canvas
            {
                Width = BUILDING_WIDTH,
                Height = buildingHeight,
                Background = Brushes.Gray,
                Tag = BUILDING_TAG
            };

            buildingCanvas.SetValue(Panel.ZIndexProperty, 1);
            buildingCanvas.SetValue(Canvas.TopProperty, HEIGHT - buildingHeight);
            buildingCanvas.SetValue(Canvas.LeftProperty, currentBuildingIndex * BUILDING_WIDTH);

            foreach (var window in CreateBuildingWindows(buildingHeight))
            {
                buildingCanvas.Children.Add(window);
            }

            return buildingCanvas;
        }

        private static IEnumerable<Rectangle> CreateBuildingWindows(double buildingHeight)
        {
            var heightToDraw = buildingHeight;
            var switcher = 0;
            while (heightToDraw >= BUILDING_WINDOW_SIZE)
            {
                if (switcher % 2 == 1)
                {
                    var pad = (BUILDING_WIDTH - WINDOW_WIDTH_FIRST_TO_LAST) * 0.5;
                    for (var i = 0; i < BUILDING_WINDOW_LINE_COUNT; i++)
                    {
                        yield return DrawWindow(buildingHeight, heightToDraw, pad, i);

                        pad += BUILDING_WINDOW_SIZE * 0.5;
                    }
                }
                switcher++;
                heightToDraw -= BUILDING_WINDOW_SIZE;
            }
        }

        private static Rectangle DrawWindow(double buildingHeight, double heightWhereToDraw, double concretePadLeft, int currentColumnIndex)
        {
            var window = new Rectangle
            {
                Width = BUILDING_WINDOW_SIZE,
                Height = BUILDING_WINDOW_SIZE,
                Fill = _rdm.Next(1, 3) == 1 ? Brushes.Yellow : Brushes.Black
            };

            window.SetValue(Panel.ZIndexProperty, 2);
            window.SetValue(Canvas.TopProperty, buildingHeight - heightWhereToDraw);
            window.SetValue(Canvas.LeftProperty, concretePadLeft + (currentColumnIndex * BUILDING_WINDOW_SIZE));

            return window;
        }

        private static List<T> GetChildrenByTypeAndTag<T>(Panel panel, string tag) where T : FrameworkElement
        {
            return panel.Children
                .OfType<T>()
                .Where(r => r.Tag != null && r.Tag.ToString() == tag)
                .ToList();
        }
    }
}
