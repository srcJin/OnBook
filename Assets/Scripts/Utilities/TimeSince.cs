using UnityEngine;

namespace Scripts.Utilities
{
    /// <summary>
    /// 
    /// This structure will accumulate time from 0 seconds when it gets created.
    /// 
    /// Here's an example of its usage in a different script:
    /// TimeSince timeSinceStart;
    /// 
    /// void Start() {
    ///     // Time will start accumulating.
    ///     timeSinceStart = 0;
    /// }
    /// 
    /// void Update() {
    ///     // We can use timeSinceStart as a timer.
    ///     // When the timeSinceStart has accumulated 1 second, do something.
    ///     if (timeSinceStart > 1) {
    ///         doSomething();
    ///         
    ///         // If we want to loop this if conditional, we can reset the timer every time.
    ///         timeSinceStart = 0;
    ///     }
    /// }
    /// 
    /// </summary>
    public struct TimeSince
    {
        float time;

        public static implicit operator float(TimeSince ts)
        {
            return Time.time - ts.time;
        }

        public static implicit operator TimeSince(float ts)
        {
            return new TimeSince { time = Time.time - ts };
        }
    }
}