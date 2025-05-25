using System;

namespace F8Framework.Core
{
    public class PausedTimer : IReference
    {
        public long ResidueTime { get; private set; }
        public long Interval  { get; private set; }
        public int RepeatCount { get; private set; }
        public Action<int, bool> Callback { get; private set; }
        public int CurrentCount { get; private set; }
        public bool IsRealTimeType { get; private set;  } = true;

        public static PausedTimer Create(long residueTime, long interval, int repeatCount, bool isRealTime,Action<int, bool> callback)
        {
            var timer = ReferencePool.Acquire<PausedTimer>();
            timer.ResidueTime = residueTime;
            timer.Interval = interval;
            timer.RepeatCount = repeatCount;
            timer.Callback = callback;
            timer.IsRealTimeType = isRealTime;
            return timer;
        }
        public void Clear()
        {
            ResidueTime = 0;
            Interval = 0;
            RepeatCount = 0;
            Callback = null;
            CurrentCount = 0;
            IsRealTimeType = true;
        }
        public void Release()
        {
            Clear();
            ReferencePool.Release(this);
        }
    }
    public class Timer : IReference
    {
        private static int m_SerialId = 0;          //自增id
        private enum TimerType
        {
            RealTime = 0,                           //绝对时间定时器
            RelativeTime = 1,                       //相对时间定时器
        }
        private TimerType m_Type = TimerType.RealTime;
        private int m_ID = 0;                       //定时器id
        private long m_ExpiresTime;                 //下次执行时间。毫秒
        private long m_Interval;                    //执行间隔。毫秒
        private int m_RepeatCount;                  //执行次数。0为持续执行
        private Action<int, bool> m_Callback;       //执行回调。int：为执行次数，bool：最后一次为true
        private int m_CurrentCount;                 //当前执行次数
        public int ID => m_ID;
        public long ExpiresTime => m_ExpiresTime;

        public long Interval => m_Interval;
        public int RepeatCount => m_RepeatCount;
        public Action<int, bool> Callback => m_Callback;
        public int CurrentCount => m_CurrentCount;
        public bool IsRemoved { set; get; } = false;
        public bool IsRealTimeType => m_Type == TimerType.RealTime;
        public long ResidueTime { get; set; }

        /// <summary>
        /// 执行回调
        /// </summary>
        /// <param name="executeCallback">是否执行回调，用于过期定时器清理</param>
        /// <returns>false:执行完毕无需加入到队列 true：需要添加到下一次队列</returns>
        public bool OnCallback(bool executeCallback = true)
        {
            if (IsRemoved)
                return false;
            m_CurrentCount++;
            if(executeCallback)
                m_Callback?.Invoke(m_CurrentCount, m_CurrentCount == m_RepeatCount);
            if (m_RepeatCount == 0 || m_CurrentCount < m_RepeatCount)
            {
                m_ExpiresTime += m_Interval;//下次执行时间
                return true;
            }

            return false;
        }

        public bool CheckFinish()
        {
            if (m_RepeatCount == 0 || m_CurrentCount < m_RepeatCount)
            {
                m_ExpiresTime += m_Interval;//下次执行时间
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建一个定时器
        /// </summary>
        /// <param name="startTime">延迟执行时间，毫秒</param>
        /// <param name="interval">执行间隔，毫秒</param>
        /// <param name="repeatCount">执行次数。0为持续执行</param>
        /// <param name="callback">执行回调。int：为执行次数，bool：最后一次为true</param>
        /// <returns></returns>
        public static Timer Create(long startTime, long interval, int repeatCount, Action<int, bool> callback)
        {
            var timer = ReferencePool.Acquire<Timer>();
            timer.m_ID = ++m_SerialId;
            timer.m_ExpiresTime = startTime + interval;
            timer.m_Interval = interval;
            timer.m_RepeatCount = repeatCount;
            timer.m_Callback = callback;
            return timer;
        }
        
        public static Timer CreateRelativeTime(long startTime, long interval, int repeatCount, Action<int, bool> callback)
        {
            var timer = Create(startTime, interval, repeatCount, callback);
            timer.m_Type = TimerType.RelativeTime;
            return timer;
        }
        
        public void Clear()
        {
            m_ID = 0;
            m_ExpiresTime = 0;
            m_Interval = 0;
            m_RepeatCount = 0;
            m_CurrentCount = 0;
        }
        
        public void Release()
        {
            Clear();
            ReferencePool.Release(this);
        }
    }
}