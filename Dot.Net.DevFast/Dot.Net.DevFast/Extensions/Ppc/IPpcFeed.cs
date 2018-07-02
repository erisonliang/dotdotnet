﻿using System;
using System.Threading;

namespace Dot.Net.DevFast.Extensions.Ppc
{
    /// <summary>
    /// Feed generated by producers (to be consumed by consumers).
    /// </summary>
    /// <typeparam name="T">feed Data type</typeparam>
    public interface IProducerFeed<T>
    {
        /// <summary>
        /// Provides the data instances generatd by Producer(s) and returns true. 
        /// False is returned when no more data is available OR timeout is reached.
        /// <para>NOTE: It is possible that more data will be available in future,
        /// thus, check <see cref="Finished"/> before giving up looping.</para>
        /// </summary>
        /// <param name="millisecTimeout">Timeout in milliseconds. use <seealso cref="Timeout.Infinite"/> 
        /// to wait inifinitely.</param>
        /// <param name="token">Cancellation token to observer while extracting data</param>
        /// <param name="data">Produced data instance, if any</param>
        /// <exception cref="OperationCanceledException">If token is canceled</exception>
        bool TryGet(int millisecTimeout, CancellationToken token, out T data);

        /// <summary>
        /// Returns true if the data collection would never return an item when calling
        /// <see cref="TryGet"/> even with <seealso cref="Timeout.Infinite"/> timeout.
        /// </summary>
        bool Finished { get; }
    }

    /// <summary>
    /// Feed generated for consumers (to be populated by producers).
    /// </summary>
    /// <typeparam name="T">feed Data type</typeparam>
    public interface IConsumerFeed<in T>
    {
        /// <summary>
        /// Adds an item in the feed to be consumed by consumers observing the feed.
        /// <para>This method is THREAD-SAFE.</para>
        /// </summary>
        /// <param name="item">item to add</param>
        /// <param name="token">Cancellation token to observer while adding data</param>
        /// <exception cref="OperationCanceledException">If token is canceled</exception>
        void Add(T item, CancellationToken token);

        /// <summary>
        /// Tries adding an item in the feed to be consumed by consumers observing the feed.
        /// Returns true if the item was added before the timeout else false.
        /// <para>This method is THREAD-SAFE.</para>
        /// </summary>
        /// <param name="item">item to add</param>
        /// <param name="millisecTimeout">timeout in milliseconds</param>
        /// <param name="token">Cancellation token to observer while adding data</param>
        /// <exception cref="OperationCanceledException">If token is canceled</exception>
        bool TryAdd(T item, int millisecTimeout, CancellationToken token);
    }

    internal interface IPpcFeed<T> : IConsumerFeed<T>, IProducerFeed<T>, IDisposable
    {
    }
}