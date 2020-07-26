using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GorillazVintage
{
    public partial class MainWindow : Window
    {
        private const double FPS = 60;
        private const double MONKEY_SIZE = 40;
        private const double BANANA_SIZE = 10;
        private const double BUILDING_WINDOW_SIZE = 12;
        private const double BUILDING_WINDOW_LINE_COUNT = 4;
        private const double HEIGHT = 600;
        private const double WIDTH = 800;
        private const double MAX_BUILDING_HEIGHT_RATE = 0.8;
        private const double MIN_BUILDING_HEIGHT_RATE = 0.2;
        private const double BUILDING_WIDTH_RATE = 0.1;
        private const double BUILDING_SPREAD_RATE = 0.1;
        private const string BUILDING_TAG = "BUILDING";
        private const double GRAVITY = 9.80665;

        private static readonly double WINDOW_WIDTH_FIRST_TO_LAST = ((BUILDING_WINDOW_LINE_COUNT * BUILDING_WINDOW_SIZE) + ((BUILDING_WINDOW_LINE_COUNT - 1) * (BUILDING_WINDOW_SIZE * 0.5)));
        private static readonly double BUILDING_WIDTH = WIDTH * BUILDING_WIDTH_RATE;
        private static readonly int BUILDING_COUNT = (int)(1 / BUILDING_WIDTH_RATE);
        private static readonly double ELAPSE = 1000 / FPS;

        private static readonly Random _rdm = new Random();
        private readonly List<double> _buildingsHeightInfo = new List<double>();
        private readonly Timer _timer = new Timer(ELAPSE);
        private Point _bananaCurrentPosition;
        private Point _bananaInitialPosition;
        private UIElement _bananaControl;
        private bool _timerIsCurrent;
        private double _initialSpeed;
        private double _initialAngle;
        private double _flightTime;

        public MainWindow()
        {
            InitializeComponent();

            CvsMain.Width = WIDTH;
            CvsMain.Height = HEIGHT;

            SetBuildings();

            DrawMainCanvasBuildings();

            CvsMain.Children.Add(DrawMonkeySprite(1));
            CvsMain.Children.Add(DrawMonkeySprite(8));
        }

        private Image DrawMonkeySprite(int buildingIndex)
        {
            var img = new Image
            {
                Source = Properties.Resources.monkey_sprite.ToBitmapImage(),
                Width = MONKEY_SIZE,
                Height = MONKEY_SIZE
            };

            img.SetValue(Panel.ZIndexProperty, 1);
            img.SetValue(Canvas.TopProperty, HEIGHT - _buildingsHeightInfo[buildingIndex] - MONKEY_SIZE);
            img.SetValue(Canvas.LeftProperty, (buildingIndex * BUILDING_WIDTH) + ((BUILDING_WIDTH - MONKEY_SIZE) / 2));
            return img;
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

        private void BtnShoot_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.Enabled)
            {
                return;
            }

            _bananaInitialPosition = new Point(
                (HEIGHT - _buildingsHeightInfo[8] - MONKEY_SIZE) - BANANA_SIZE,
                ((8 * BUILDING_WIDTH) + ((BUILDING_WIDTH - MONKEY_SIZE) / 2)) - BANANA_SIZE
            );
            _bananaCurrentPosition = new Point(
                _bananaInitialPosition.X,
                _bananaInitialPosition.Y
            );

            _flightTime = 0;
            _initialSpeed = SldSpeed.Value / 10;
            _initialAngle = (Math.PI / 180) * SldAngle.Value;

            SetBanana(true);

            CvsMain.Children.Add(_bananaControl);

            _timer.Elapsed += _timer_Elapsed;

            _timer.Start();
        }

        private void SetBanana(bool start)
        {
            if (start)
            {
                _bananaControl = new Ellipse
                {
                    Width = BANANA_SIZE,
                    Height = BANANA_SIZE,
                    Fill = Brushes.Purple
                };
                _bananaControl.SetValue(Panel.ZIndexProperty, 2);
            }
            _bananaControl.SetValue(Canvas.TopProperty, _bananaCurrentPosition.X);
            _bananaControl.SetValue(Canvas.LeftProperty, _bananaCurrentPosition.Y);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_timerIsCurrent)
            {
                return;
            }
            _timerIsCurrent = true;

            _flightTime += 1 / FPS;

            _bananaCurrentPosition = new Point(
                _bananaInitialPosition.X + ((_flightTime * _initialSpeed * Math.Cos(_initialAngle)) * -1),
                ((_flightTime * _initialSpeed * Math.Sin(_initialAngle)) - (0.5 * GRAVITY * _flightTime * _flightTime)) + _bananaInitialPosition.Y);

            bool colision = false;

            if (colision)
            {
                Dispatcher.Invoke(() => CvsMain.Children.Remove(_bananaControl));
                _timer.Stop();
            }
            else
            {
                Dispatcher.Invoke(() => SetBanana(false));
            }

            _timerIsCurrent = false;
        }
    }
}
