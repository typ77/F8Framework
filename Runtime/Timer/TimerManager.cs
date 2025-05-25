using System;
using System.Collections.Generic;
using UnityEngine;

namespace F8Framework.Core
{
    /// <summary>
    /// 新的定时器管理器，使用最小堆实现。降低排序（或遍历）的复杂度，提高性能。
    /// 1.实现真实时间定时器
    /// 2.实现相对时间定时器。（受timescale影响，可加速,可暂停。x0,x0.5,x1x2x3...）
    /// </summary>
    [UpdateRefresh]
    public class TimerManager : ModuleSingleton<TimerManager>, IModule
    {
        //private List<Timer> times = new List<Timer>(); // 存储计时器的列表
        private MinHeap<Timer> m_GameTimerHeap;                             // 最小堆实现的计时器堆
        private MinHeap<Timer> m_RealTimerHeap;                             // 最小堆实现的计时器堆
        private readonly Dictionary<int, Timer> m_TimerDict = new Dictionary<int, Timer>(); // 存储计时器的字典，用于快速查找
        private readonly Dictionary<int, PausedTimer> m_PausedTimerDict = new Dictionary<int, PausedTimer>(); // 存储暂停的计时器的字典，用于快速查找
        private long initTime; // 初始化时间
        private long serverTime; // 服务器时间
        private long tempTime; // 临时时间
        private bool isFocus = true; // 是否处于焦点状态

        private long m_RunTime = 0;     //程序运行时时间
        private long m_GameTime = 0;   //游戏运行时间，受timescale影响
        //private int frameTime = 1; // 帧时间，默认为1

        public void OnInit(object createParam)
        {
            m_GameTimerHeap = new MinHeap<Timer>(comparer: Comparer<Timer>.Create((a, b) => 
                a.ExpiresTime.CompareTo(b.ExpiresTime)));
            m_RealTimerHeap = new MinHeap<Timer>(comparer: Comparer<Timer>.Create((a, b) => 
                a.ExpiresTime.CompareTo(b.ExpiresTime)));
            initTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            serverTime = 0;
            tempTime = 0;
        }

        public void OnLateUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }

        public void OnTermination()
        {
            MessageManager.Instance.RemoveEventListener(MessageEvent.ApplicationFocus, OnApplicationFocus, this);
            MessageManager.Instance.RemoveEventListener(MessageEvent.NotApplicationFocus, NotApplicationFocus, this);
            m_GameTimerHeap.Clear();
            m_RealTimerHeap.Clear();
            m_TimerDict.Clear();
            m_PausedTimerDict.Clear();
            Destroy();
        }

        public void OnUpdate()
        {
            if (isFocus == false ) // 如果失去焦点，则返回
            {
                return;
            }
            //实际运行时间
            m_RunTime = Mathf.FloorToInt(Time.realtimeSinceStartup * 1000);
            //1.真实事件定时器
            CheckTimer(m_RealTimerHeap, m_RunTime);
            //游戏运行时间，受timescale影响
            m_GameTime = Mathf.FloorToInt(Time.time * 1000);
            //2.相对时间定时器
            CheckTimer(m_GameTimerHeap, m_GameTime);
        }

        private void CheckTimer(MinHeap<Timer> heap, long time)
        {
            while (true)
            {
                //取出堆顶，如果堆顶的时间小于当前时间，则取出堆顶，否则退出
                var topTimer = heap.PeekMin();
                if (topTimer == null || topTimer.ExpiresTime > time)
                {
                    break;
                }
                //移除堆顶
                heap.RemoveMin();

                if(topTimer.OnCallback())
                    heap.Insert(topTimer);
                else
                {
                    //回收定时器
                    m_TimerDict.Remove(topTimer.ID);
                    topTimer.Release();
                }
            }
        }

        #region 真是时间定时器

        /// <summary>
        /// 注册一个真实时间定时器。不受timescale影响
        /// </summary>
        /// <returns></returns>
        public int AddRealTimer(long delay, long interval, int repeatCount, Action<int, bool> callback)
        {
            var currentRealTime = (long)(Time.realtimeSinceStartup * 1000);
            var timer = Timer.Create(currentRealTime + delay, interval, repeatCount, callback);
            m_RealTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }
        /// <summary>
        /// 添加一个延时执行的定时器。不受timescale影响
        /// </summary>
        /// <returns></returns>
        public int AddOnceTimer(long delay, Action<int, bool> callback)
        {
            var currentRealTime = (long)(Time.realtimeSinceStartup * 1000);
            var timer = Timer.Create(currentRealTime + delay, 0, 1, callback);
            m_RealTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }
        /// <summary>
        /// 添加一个下一帧执行的定时器。不受timescale影响
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public int AddNextFrameTimer(Action<int, bool> callback)
        {
            var currentRealTime = (long)(Time.realtimeSinceStartup * 1000);
            var timer = Timer.Create(currentRealTime + 1, 0, 1, callback);
            m_RealTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }

        #endregion

        #region 游戏时间定时器

        public int AddGameTimer(long delay, long interval, int repeatCount, Action<int, bool> callback)
        {
            var currentGameTime = (long)(Time.time * 1000);
            var timer = Timer.CreateRelativeTime(currentGameTime + delay, interval, repeatCount, callback);
            m_GameTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }
        
        public int AddOnceGameTimer(long delay, Action<int, bool> callback)
        {
            var currentGameTime = (long)(Time.time * 1000);
            var timer = Timer.CreateRelativeTime(currentGameTime + delay, 0, 1, callback);
            m_GameTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }

        public int AddNextFrameGameTimer(Action<int, bool> callback)
        {
            var currentGameTime = (long)(Time.time * 1000);
            var timer = Timer.CreateRelativeTime(currentGameTime + 1, 0, 1, callback);
            m_GameTimerHeap.Insert(timer);
            m_TimerDict.Add(timer.ID, timer);
            return timer.ID;
        }
        #endregion
        
        /// <summary>
        /// 删除定时器，出于性能的考虑，删除定时器并不会立刻被销毁。定时器会在下次检查时被销毁。
        /// </summary>
        /// <param name="id"></param>
        public void RemoveTimer(int id)
        {
            if (m_TimerDict.Remove(id, out var timer))
            {
                //标记为删除，不会立刻被销毁。定时器会在下次检查时被销毁。
                timer.IsRemoved = true;
            }
            //如果在暂停的定时器里面，删除后需主动回收
            if(m_PausedTimerDict.Remove(id, out var pausedTimer))
            {
                pausedTimer.Release();
            }
        }

        // 设置服务器时间
        public void SetServerTime(long val)
        {
            if (val != 0) // 如果传入的值不为0，则更新服务器时间和临时时间
            {
                serverTime = val;
                tempTime = GetTime();
            }
        }

        // 获取服务器时间
        public long GetServerTime()
        {
            return serverTime + (GetTime() - tempTime); // 返回服务器时间加上当前时间与临时时间之间的差值
        }

        // 获取游戏中的总时长
        public long GetTime()
        {
            //可改为Unity启动的总时长
            // float floatValue = Time.time;
            // long longValue = (long)(floatValue * 1000000);
            // return longValue;
            return GetLocalTime() - initTime; // 返回当前时间与初始化时间的差值
        }

        // 获取本地时间
        public long GetLocalTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 返回当前时间的毫秒数
        }
        /// <summary>
        /// 查询是否存在计时器
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsExistTimer(int id)
        {
            return m_TimerDict.ContainsKey(id) || m_PausedTimerDict.ContainsKey(id);
        }
        /// <summary>
        /// 不实现。因为定时器修改需要重新排序。实际做法会先删除原有定时器，再创建新的定时器。
        /// 实际上定时器已更变为新的定时器。因timer id已更变。所以建议自行实现。
        /// </summary>
        [Obsolete("此方法不实现，可以先删除后创建来实现。",true)]
        public void ChangeTimer()
        {
            
        }

        /// <summary>
        /// 暂停定计时器
        /// </summary>
        /// <param name="id"></param>
        public void Pause(int id = 0)
        {
            //暂停定时器，取出相应参数，原定时器移除
            if (m_TimerDict.Remove(id, out var timer))
            {
                //标记为删除，不会立刻被销毁。定时器会在下次检查时被销毁。
                timer.IsRemoved = true;
                long currentTime;
                if (timer.IsRealTimeType)
                    currentTime = (long)(Time.realtimeSinceStartup * 1000); // 真实时间
                else
                    currentTime = (long)(Time.time * 1000); // 游戏时间（受timescale影响）
                var residueTime = timer.ExpiresTime - currentTime;
                if (residueTime < 0) residueTime = 0; // 避免负的剩余时间
                
                var pausedTimer = PausedTimer.Create(residueTime,
                    timer.Interval, timer.RepeatCount, timer.IsRealTimeType, timer.Callback);
                m_PausedTimerDict.Add(timer.ID, pausedTimer);
            }
        }

        /// <summary>
        /// 恢复定计时器
        /// </summary>
        /// <param name="id"></param>
        public void Resume(int id = 0)
        {
            if (m_PausedTimerDict.Remove(id, out var timer))
            {
                if (timer.IsRealTimeType)
                {
                    AddRealTimer(timer.ResidueTime, timer.Interval, timer.RepeatCount, timer.Callback);
                }
                else
                {
                    AddGameTimer(timer.ResidueTime, timer.Interval, timer.RepeatCount, timer.Callback);
                }
            }
        }
        
        public void AddListenerApplicationFocus()
        {
            MessageManager.Instance.AddEventListener(MessageEvent.ApplicationFocus, OnApplicationFocus, this);
            MessageManager.Instance.AddEventListener(MessageEvent.NotApplicationFocus, NotApplicationFocus, this);
        }

        // 当应用程序获得焦点时调用
        void OnApplicationFocus()
        {
            isFocus = true;
        }

        // 当应用程序失去焦点时调用
        void NotApplicationFocus()
        {
            isFocus = false;
        }

        // 重新启动所有计时器，或指定计时器
        public void Restart(int id = 0)
        {
        }
    }
}