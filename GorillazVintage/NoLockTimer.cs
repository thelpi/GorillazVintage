using System;
using System.Timers;

namespace GorillazVintage
{
    public class NoLockTimer : IDisposable
    {
        private readonly Timer _timer;

        public NoLockTimer(double interval, Func<bool> stuffToDo)
        {
            _timer = new Timer
            {
                AutoReset = false,
                Interval = interval
            };

            _timer.Elapsed += delegate
            {
                if (stuffToDo())
                {
                    _timer.Start();
                }
            };
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }
    }
}
