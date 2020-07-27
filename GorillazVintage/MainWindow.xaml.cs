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
        private const int WINDOW_ZINDEX = 2;
        private const int EXPLOSION_ZINDEX = 3;
        private const int BANANA_ZINDEX = 4;
        private const int MONKEY_ZINDEX = 1;
        private const int BUILDING_ZINDEX = 1;

        private const double EXPLOSION_RANGE = 20;
        private const double MAX_SPEED = 30;
        private const double METER_TO_PIXEL_RATE = 12;
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
        private readonly List<Rect> _buildingsInfo = new List<Rect>();
        private readonly Dictionary<Rect, bool> _explosions = new Dictionary<Rect, bool>();
        private readonly NoLockTimer _timer;
        private Rect _bananaRect;
        private Point _bananaInitialPosition;
        private UIElement _bananaControl;
        private double _initialSpeed;
        private double _initialAngle;
        private double _flightTime;
        private DateTime _lastCheckDate;
        private bool _throwInProgress = false;
        private object _locker = new object();
        private Rect _monkey1;
        private Rect _monkey2;

        public MainWindow()
        {
            InitializeComponent();

            CvsMain.Width = WIDTH;
            CvsMain.Height = HEIGHT;

            NewGame();

            _timer = new NoLockTimer(ELAPSE, TimerAction);
        }

        private static Image DrawMonkeySprite(Rect monkeyRect)
        {
            var img = new Image
            {
                Source = Properties.Resources.monkey_sprite.ToBitmapImage(),
                Width = monkeyRect.Width,
                Height = monkeyRect.Height
            };

            img.SetValue(Panel.ZIndexProperty, MONKEY_ZINDEX);
            img.SetValue(Canvas.TopProperty, monkeyRect.Top);
            img.SetValue(Canvas.LeftProperty, monkeyRect.Left);
            return img;
        }

        private static IEnumerable<Canvas> CreateBuildings(List<Rect> buildingsHeightInfo)
        {
            foreach (var buildind in buildingsHeightInfo)
            {
                yield return DrawBuilding(buildind);
            }
        }

        private static Canvas DrawBuilding(Rect building)
        {
            var buildingCanvas = new Canvas
            {
                Width = BUILDING_WIDTH,
                Height = building.Height,
                Background = Brushes.Gray,
                Tag = BUILDING_TAG
            };

            buildingCanvas.SetValue(Panel.ZIndexProperty, BUILDING_ZINDEX);
            buildingCanvas.SetValue(Canvas.TopProperty, building.Top);
            buildingCanvas.SetValue(Canvas.LeftProperty, building.Left);

            foreach (var window in CreateBuildingWindows(building))
            {
                buildingCanvas.Children.Add(window);
            }

            return buildingCanvas;
        }

        private static IEnumerable<Rectangle> CreateBuildingWindows(Rect building)
        {
            var heightToDraw = building.Height;
            var switcher = 0;
            while (heightToDraw >= BUILDING_WINDOW_SIZE)
            {
                if (switcher % 2 == 1)
                {
                    var pad = (BUILDING_WIDTH - WINDOW_WIDTH_FIRST_TO_LAST) * 0.5;
                    for (var i = 0; i < BUILDING_WINDOW_LINE_COUNT; i++)
                    {
                        yield return DrawWindow(building.Height, heightToDraw, pad, i);

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

            window.SetValue(Panel.ZIndexProperty, WINDOW_ZINDEX);
            window.SetValue(Canvas.TopProperty, buildingHeight - heightWhereToDraw);
            window.SetValue(Canvas.LeftProperty, concretePadLeft + (currentColumnIndex * BUILDING_WINDOW_SIZE));

            return window;
        }

        private void SetBuildings()
        {
            var formerBuildingRate = Double.NaN;
            for (int i = 0; i < BUILDING_COUNT; i++)
            {
                var buildingHeightRate = _rdm.NextDouble();
                while (!IsInBuildingHeightRange(formerBuildingRate, buildingHeightRate))
                {
                    buildingHeightRate = _rdm.NextDouble();
                }
                _buildingsInfo.Add(new Rect(i * BUILDING_WIDTH, HEIGHT - (buildingHeightRate * HEIGHT), BUILDING_WIDTH, buildingHeightRate * HEIGHT));
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
        
        private void BtnShoot_Click(object sender, RoutedEventArgs e)
        {
            if (_throwInProgress)
            {
                return;
            }

            SetBananaInitialValues(false);

            DrawBanana(true);

            CvsMain.Children.Add(_bananaControl);

            _timer.Start();
        }

        private void SetBananaInitialValues(bool firstPlayer)
        {
            var buildingIndex = firstPlayer ? 1 : 8;

            _bananaInitialPosition = new Point(
                (_buildingsInfo[buildingIndex].Top - MONKEY_SIZE) - BANANA_SIZE,
                ((buildingIndex * BUILDING_WIDTH) + ((BUILDING_WIDTH - MONKEY_SIZE) / 2)) - BANANA_SIZE
            );

            _bananaRect = new Rect(
                _bananaInitialPosition.Y,
                _bananaInitialPosition.X,
                BANANA_SIZE,
                BANANA_SIZE
            );

            _flightTime = 0;
            _initialSpeed = MAX_SPEED * (SldSpeed.Value / 100);
            _initialAngle = (Math.PI / 180) * SldAngle.Value;
            _lastCheckDate = DateTime.Now;
            _throwInProgress = true;
        }

        private void DrawBanana(bool start)
        {
            if (start)
            {
                _bananaControl = new Ellipse
                {
                    Width = BANANA_SIZE,
                    Height = BANANA_SIZE,
                    Fill = Brushes.Yellow
                };
                _bananaControl.SetValue(Panel.ZIndexProperty, BANANA_ZINDEX);
            }
            _bananaControl.SetValue(Canvas.TopProperty, _bananaRect.Y);
            _bananaControl.SetValue(Canvas.LeftProperty, _bananaRect.X);
        }

        private bool TimerAction()
        {
            if (_throwInProgress)
            {
                _flightTime += (DateTime.Now - _lastCheckDate).TotalSeconds;
                _lastCheckDate = DateTime.Now;

                SetBananaNewPosition(true);

                ColideWithMonkey();

                var buildingColision = ColideWithBuilding();

                Dispatcher.Invoke(() => DrawBanana(false));

                if (buildingColision)
                {
                    Dispatcher.Invoke(() => CvsMain.Children.Remove(_bananaControl));
                    _throwInProgress = false;
                }

                foreach (var explosion in _explosions.Keys)
                {
                    if (!_explosions[explosion])
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Ellipse exEll = new Ellipse
                            {
                                Fill = CvsMain.Background,
                                Width = explosion.Width,
                                Height = explosion.Height
                            };

                            exEll.SetValue(Panel.ZIndexProperty, EXPLOSION_ZINDEX);
                            exEll.SetValue(Canvas.TopProperty, explosion.Top);
                            exEll.SetValue(Canvas.LeftProperty, explosion.Left);

                            CvsMain.Children.Add(exEll);
                        });
                        _explosions[explosion] = true;
                    }
                }
            }

            return _throwInProgress;
        }

        private void ColideWithMonkey()
        {

        }

        private void SetBananaNewPosition(bool fromRight)
        {
            var yDelta = (_flightTime * _initialSpeed * Math.Cos(_initialAngle)) * (fromRight  ? - 1 : 1);
            var xDelta = ((_flightTime * _initialSpeed * Math.Sin(_initialAngle)) - (0.5 * GRAVITY * _flightTime * _flightTime));

            _bananaRect.Y = _bananaInitialPosition.X - (METER_TO_PIXEL_RATE * xDelta);
            _bananaRect.X = _bananaInitialPosition.Y + (METER_TO_PIXEL_RATE * yDelta);
        }

        private bool ColideWithBuilding()
        {
            int i = 0;
            foreach (var building in _buildingsInfo)
            {
                building.Intersect(_bananaRect);
                if (building != Rect.Empty)
                {
                    foreach (var explosion in _explosions.Keys)
                    {
                        explosion.Intersect(_bananaRect);
                        if (_bananaRect == explosion)
                        {
                            return false;
                        }
                    }

                    _explosions.Add(new Rect(
                        building.Left - EXPLOSION_RANGE,
                        building.Top - EXPLOSION_RANGE,
                        2 * EXPLOSION_RANGE,
                        2 * EXPLOSION_RANGE), false);
                    
                    return true;
                }
                i++;
            }

            return false;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!_throwInProgress)
            {
                NewGame();
            }
        }

        private void NewGame()
        {
            _buildingsInfo.Clear();
            _explosions.Clear();
            CvsMain.Children.Clear();

            SetBuildings();

            foreach (var building in CreateBuildings(_buildingsInfo))
            {
                CvsMain.Children.Add(building);
            }

            _monkey1 = new Rect(
                (BUILDING_WIDTH) + ((BUILDING_WIDTH - MONKEY_SIZE) / 2),
                HEIGHT - _buildingsInfo[1].Height - MONKEY_SIZE,
                MONKEY_SIZE,
                MONKEY_SIZE
            );
            _monkey2 = new Rect(
                (8 * BUILDING_WIDTH) + ((BUILDING_WIDTH - MONKEY_SIZE) / 2),
                HEIGHT - _buildingsInfo[8].Height - MONKEY_SIZE,
                MONKEY_SIZE,
                MONKEY_SIZE
            );

            CvsMain.Children.Add(DrawMonkeySprite(_monkey1));
            CvsMain.Children.Add(DrawMonkeySprite(_monkey2));
        }
    }
}
