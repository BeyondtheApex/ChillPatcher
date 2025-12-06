using System;
using ChillPatcher.SDK.Events;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 事件总线接口
    /// 用于模块间的事件通信
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <returns>用于取消订阅的 IDisposable</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IModuleEvent;

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : IModuleEvent;

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        void UnsubscribeAll();
    }
}
