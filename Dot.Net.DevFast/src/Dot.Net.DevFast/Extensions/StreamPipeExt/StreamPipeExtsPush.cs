﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dot.Net.DevFast.Etc;
using Dot.Net.DevFast.Extensions.Internals;
using Dot.Net.DevFast.Extensions.JsonExt;
using Dot.Net.DevFast.Extensions.StringExt;
using Newtonsoft.Json;

namespace Dot.Net.DevFast.Extensions.StreamPipeExt
{
    /// <summary>
    /// Extensions methods on stream pipes.
    /// </summary>
    public static partial class StreamPipeExts
    {
        #region Various Push

        /// <summary>
        /// Pushes the string content and returns a new pipe for chaining.
        /// <para>NOTE: If you already have a string builder, then use the overloaded method instead
        /// of doing yourStringBuilder.ToString().LoadString(...), as overloaded method is optimized.</para>
        /// Supplied <paramref name="stringTask"/> is awaited during bootstrapping (NOT during chaining)
        /// </summary>
        /// <param name="stringTask">Task returning the source string. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="bufferSize">Buffer size (as number of char instead of bytes)</param>
        public static Func<PushFuncStream, Task> Push(this Task<string> stringTask,
            Encoding enc = null,
            int bufferSize = StdLookUps.DefaultBufferSize)
        {
            return async pfs =>
            {
                var str = await stringTask.StartIfNeeded().ConfigureAwait(false);
                await str.Push(enc, bufferSize)(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes the string content and returns a new pipe for chaining.
        /// <para>NOTE: If you already have a string builder, then use the overloaded method instead
        /// of doing yourStringBuilder.ToString().LoadString(...), as overloaded method is optimized.</para>
        /// </summary>
        /// <param name="s">source string</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="bufferSize">Buffer size (as number of char instead of bytes)</param>
        public static Func<PushFuncStream, Task> Push(this string s,
            Encoding enc = null, 
            int bufferSize = StdLookUps.DefaultBufferSize)
        {
            return new Action<int, char[], int, int>(s.CopyTo).ApplyLoad(s.Length, enc ?? new UTF8Encoding(false),
                bufferSize);
        }

        /// <summary>
        /// Pushes the string content of the builder and returns a new pipe for chaining.
        /// Supplied <paramref name="sbTask"/> is awaited during bootstrapping (NOT during chaining) 
        /// </summary>
        /// <param name="sbTask">Task returning source string builder. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="bufferSize">Buffer size (as number of char instead of bytes)</param>
        public static Func<PushFuncStream, Task> Push(this Task<StringBuilder> sbTask,
            Encoding enc = null,
            int bufferSize = StdLookUps.DefaultBufferSize)
        {
            return async pfs =>
            {
                var sb = await sbTask.StartIfNeeded().ConfigureAwait(false);
                await sb.Push(enc, bufferSize)(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes the string content of the builder and returns a new pipe for chaining.
        /// </summary>
        /// <param name="sb">source string builder</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="bufferSize">Buffer size (as number of char instead of bytes)</param>
        public static Func<PushFuncStream, Task> Push(this StringBuilder sb,
            Encoding enc = null, 
            int bufferSize = StdLookUps.DefaultBufferSize)
        {
            return new Action<int, char[], int, int>(sb.CopyTo).ApplyLoad(sb.Length, enc ?? new UTF8Encoding(false),
                bufferSize);
        }

        /// <summary>
        /// Pushes the data of the given file as byte stream and returns a new pipe for chaining.
        /// </summary>
        /// <param name="folder">Parent folder of the file</param>
        /// <param name="filename">An existing readable file's name with extension</param>
        /// <param name="fileStreamBuffer">Buffer size to use</param>
        /// <param name="options">File options</param>
        public static Func<PushFuncStream, Task> Push(this DirectoryInfo folder,
            string filename,
            int fileStreamBuffer = StdLookUps.DefaultFileBufferSize,
            FileOptions options = FileOptions.SequentialScan)
        {
            return folder.CreateFileInfo(filename).Push(fileStreamBuffer, options);
        }

        /// <summary>
        /// Pushes the data of the given file as byte stream and returns a new pipe for chaining.
        /// </summary>
        /// <param name="fileinfo">Fileinfo instance of an existing readable file</param>
        /// <param name="fileStreamBuffer">Buffer size to use</param>
        /// <param name="options">File options</param>
        public static Func<PushFuncStream, Task> Push(this FileInfo fileinfo,
            int fileStreamBuffer = StdLookUps.DefaultFileBufferSize,
            FileOptions options = FileOptions.SequentialScan)
        {
            return fileinfo.CreateStream(FileMode.Open, FileAccess.Read, FileShare.Read, fileStreamBuffer, options)
                .Push(fileStreamBuffer);
        }

        /// <summary>
        /// Pushes bytes from given array and returns a new pipe for chaining.
        /// Supplied <paramref name="byteTask"/> is awaited during bootstrapping (NOT during chaining) 
        /// </summary>
        /// <param name="byteTask">Task returning Source byte array. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        public static Func<PushFuncStream, Task> Push(this Task<byte[]> byteTask)
        {
            return async pfs =>
            {
                var bytes = await byteTask.StartIfNeeded().ConfigureAwait(false);
                await bytes.Push()(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes bytes from given array and returns a new pipe for chaining.
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        public static Func<PushFuncStream, Task> Push(this byte[] bytes)
        {
            return new ArraySegment<byte>(bytes, 0, bytes.Length).Push();
        }

        /// <summary>
        /// Pushes bytes from given byte segment and returns a new pipe for chaining.
        /// Supplied <paramref name="byteSegTask"/> is awaited during bootstrapping (NOT during chaining) 
        /// </summary>
        /// <param name="byteSegTask">task returning Source array segment. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        public static Func<PushFuncStream, Task> Push(this Task<ArraySegment<byte>> byteSegTask)
        {
            return async pfs =>
            {
                var byteSeg = await byteSegTask.StartIfNeeded().ConfigureAwait(false);
                await byteSeg.Push()(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes bytes from given byte segment and returns a new pipe for chaining.
        /// </summary>
        /// <param name="byteSeg">Source byte array</param>
        public static Func<PushFuncStream, Task> Push(this ArraySegment<byte> byteSeg)
        {
            return async pfs =>
            {
                var s = pfs.Writable;
                var t = pfs.Token;
                try
                {
                    await s.WriteAsync(byteSeg.Array, byteSeg.Offset, byteSeg.Count, t).ConfigureAwait(false);
                    await s.FlushAsync(t).ConfigureAwait(false);
                }
                finally
                {
                    s.DisposeIfRequired(pfs.Dispose);
                }
            };
        }

        /// <summary>
        /// Pushes bytes from given source stream and returns a new pipe for chaining.
        /// Supplied <paramref name="streamTask"/> is awaited during bootstrapping (NOT during chaining) 
        /// </summary>
        /// <param name="streamTask">Task returning Source data stream. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        /// <param name="streamBuffer">Buffer size to use during data loading</param>
        /// <param name="disposeSourceStream">If true, source stream is disposed</param>
        public static Func<PushFuncStream, Task> Push(this Task<Stream> streamTask,
            int streamBuffer = StdLookUps.DefaultFileBufferSize,
            bool disposeSourceStream = true)
        {
            return async pfs =>
            {
                var stream = await streamTask.StartIfNeeded().ConfigureAwait(false);
                await stream.Push(streamBuffer, disposeSourceStream)(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes bytes from given source stream and returns a new pipe for chaining.
        /// </summary>
        /// <param name="source">Source data stream</param>
        /// <param name="streamBuffer">Buffer size to use during data loading</param>
        /// <param name="disposeSourceStream">If true, source stream is disposed</param>
        public static Func<PushFuncStream, Task> Push(this Stream source,
            int streamBuffer = StdLookUps.DefaultFileBufferSize,
            bool disposeSourceStream = true)
        {
            return async pfs =>
            {
                var s = pfs.Writable;
                var t = pfs.Token;
                try
                {
                    await source.CopyToAsync(s, streamBuffer, t).ConfigureAwait(false);
                    source.DisposeIfRequired(disposeSourceStream);
                    await s.FlushAsync(t).ConfigureAwait(false);
                }
                finally
                {
                    s.DisposeIfRequired(pfs.Dispose);
                }
            };
        }

        /// <summary>
        /// Pushes the equivalent json representation of the object and returns a new pipe for chaining.
        /// Supplied <paramref name="objTask"/> is awaited during bootstrapping (NOT during chaining) 
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="objTask">Task returning Object to serialize as json text. If the task is just created,
        ///  it will be started during bootstrapping.</param>
        /// <param name="serializer">if not provided, JsonSerializer with default values
        /// (see also <seealso cref="CustomJson.Serializer()"/>) will be used.</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="writerBuffer">Buffer size for the stream writer</param>
        public static Func<PushFuncStream, Task> PushJson<T>(this Task<T> objTask,
            JsonSerializer serializer = null,
            Encoding enc = null,
            int writerBuffer = StdLookUps.DefaultFileBufferSize)
        {
            return async pfs =>
            {
                var obj = await objTask.StartIfNeeded().ConfigureAwait(false);
                await obj.PushJson(serializer, enc, writerBuffer)(pfs).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Pushes the equivalent json representation of the object and returns a new pipe for chaining.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize as json text</param>
        /// <param name="serializer">if not provided, JsonSerializer with default values
        /// (see also <seealso cref="CustomJson.Serializer()"/>) will be used.</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="writerBuffer">Buffer size for the stream writer</param>
        public static Func<PushFuncStream, Task> PushJson<T>(this T obj,
            JsonSerializer serializer = null,
            Encoding enc = null,
            int writerBuffer = StdLookUps.DefaultFileBufferSize)
        {
            return new Action<PushFuncStream>(pfs => obj.ToJson(pfs.Writable, serializer, enc,
                writerBuffer, pfs.Dispose)).ToAsync(false);
        }

        /// <summary>
        /// Pushes the equivalent json array representation using the enumeration and returns a new pipe for chaining.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize as json text</param>
        /// <param name="serializer">if not provided, JsonSerializer with default values
        /// (see also <seealso cref="CustomJson.Serializer()"/>) will be used.</param>
        /// <param name="enc">Encoding to use while writing the file.
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="writerBuffer">Buffer size for the stream writer</param>
        public static Func<PushFuncStream, Task> PushJson<T>(this IEnumerable<T> obj,
            JsonSerializer serializer = null,
            Encoding enc = null,
            int writerBuffer = StdLookUps.DefaultFileBufferSize)
        {
            return new Action<PushFuncStream>(pfs => obj.ToJsonArray(pfs.Writable, serializer, pfs.Token,
                enc, writerBuffer, pfs.Dispose)).ToAsync(false);
        }

        /// <summary>
        /// Pushes the equivalent json array representation of the objects in the given blocking collection
        /// and returns a new pipe for chaining.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize as json text</param>
        /// <param name="serializer">if not provided, JsonSerializer with default values
        /// (see also <seealso cref="CustomJson.Serializer()"/>) will be used.</param>
        /// <param name="enc">Encoding to use while writing the file. 
        /// If not supplied, by default <seealso cref="Encoding.UTF8"/>
        /// (withOUT the utf-8 identifier, i.e. new UTF8Encoding(false)) will be used</param>
        /// <param name="writerBuffer">Buffer size for the stream writer</param>
        /// <param name="pcts">source to cancel in case some error is encountered. Normally,
        /// this source token is observed at data producer side.</param>
        public static Func<PushFuncStream, Task> PushJson<T>(this BlockingCollection<T> obj,
            JsonSerializer serializer = null,
            Encoding enc = null,
            int writerBuffer = StdLookUps.DefaultFileBufferSize,
            CancellationTokenSource pcts = default(CancellationTokenSource))
        {
            return new Action<PushFuncStream>(pfs => obj.ToJsonArrayParallely(pfs.Writable, serializer,
                pfs.Token, pcts, enc, writerBuffer, pfs.Dispose)).ToAsync(false);
        }
        
        #endregion Various Push

        #region Then Clauses

        /// <summary>
        /// Applies compression on the data of given functional Stream pipe and returns a new pipe for chaining.
        /// </summary>
        /// <param name="pushSrc">Current pipe of the pipeline</param>
        /// <param name="include">If true is passed, compression is performed else ignored</param>
        /// <param name="gzip">If true, <seealso cref="GZipStream"/> is used else 
        /// <seealso cref="DeflateStream"/> is used</param>
        /// <param name="level">Compression level to use.</param>
        public static Func<PushFuncStream, Task> ThenCompress(this Func<PushFuncStream, Task> pushSrc,
            bool gzip = true,
            CompressionLevel level = CompressionLevel.Optimal,
            bool include = true)
        {
            return pushSrc.ThenApply(s => s.ApplyCompression(gzip, level), include);
        }

        /// <summary>
        /// Computes the hash on the data of the given functional stream pipe and returns a new pipe for chaining.
        /// <para>IMPORTANT: Access <seealso cref="HashAlgorithm.Hash"/> ONLY AFTER the full
        /// piepline is bootstrapped and processed, i.e., calling <paramref name="ha"/>.Hash immediately
        /// after this call will not provide the correct hash.</para>
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="ha">Instance of crypto hash algorithm</param>
        /// <param name="include">If true is passed, hash is computed else ignored</param>
        public static Func<PushFuncStream, Task> ThenComputeHash(this Func<PushFuncStream, Task> src, 
            HashAlgorithm ha,
            bool include = true)
        {
            return src.ThenTransform(ha, include);
        }

        /// <summary>
        /// Converts the data, of the given functional stream pipe to equivalent Base64
        /// and returns a new pipe for chaining.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="include">If true is passed, ToBase64 conversion is performed else ignored</param>
        public static Func<PushFuncStream, Task> ThenToBase64(
            this Func<PushFuncStream, Task> src,
            bool include = true)
        {
            return src.ThenTransform(new ToBase64Transform(), include);
        }

        /// <summary>
        /// Decodes the Base64 data, of the given functional stream pipe and returns a new pipe for chaining.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="mode">Base64 transform mode</param>
        /// <param name="include">If true is passed, FromBase64 conversion is performed else ignored</param>
        public static Func<PushFuncStream, Task> ThenFromBase64(
            this Func<PushFuncStream, Task> src,
            FromBase64TransformMode mode = FromBase64TransformMode.DoNotIgnoreWhiteSpaces,
            bool include = true)
        {
            return src.ThenTransform(new FromBase64Transform(mode), include);
        }

        /// <summary>
        /// Applies the given crypto transformation to the data of the given functional stream pipe
        /// and returns a new pipe for chaining.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="transformation">Crypto Transformation to apply</param>
        /// <param name="include">If true is passed, CryptoTransform is performed else ignored</param>
        public static Func<PushFuncStream, Task> ThenTransform(
            this Func<PushFuncStream, Task> src,
            ICryptoTransform transformation,
            bool include = true)
        {
            return src.ThenApply(s => s.ApplyTransform(transformation), include);
        }

        /// <summary>
        /// Appends the given arbitrary custom functional stream pipe (i.e. <paramref name="applyFunc"/>) to the pipeline
        /// and returns a new pipe for chaining.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="applyFunc">Yet another custom functional stream pipe</param>
        /// <param name="include">If true is passed, given func is applied else ignored</param>
        public static Func<PushFuncStream, Task> ThenApply(this Func<PushFuncStream, Task> src,
            Func<Func<PushFuncStream, Task>, Func<PushFuncStream, Task>> applyFunc,
            bool include = true)
        {
            return include ? applyFunc(src) : src;
        }

        #endregion Then Clauses

        #region Finalization

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline to the file.
        /// <para>Returns the <seealso cref="FileInfo"/> object of that the written file</para>
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="folder">folder path where file is saved</param>
        /// <param name="filename">name of file. If not supplied a new GUID string will be used instead (without extension)</param>
        /// <param name="fileStreamBuffer">Buffer size of the file stream</param>
        /// <param name="options">File options</param>
        /// <param name="token">Cancellation token to observe</param>
        public static async Task<FileInfo> AndWriteFileAsync(this Func<PushFuncStream, Task> src,
            string folder,
            string filename = null,
            int fileStreamBuffer = StdLookUps.DefaultFileBufferSize,
            FileOptions options = FileOptions.SequentialScan,
            CancellationToken token = default(CancellationToken))
        {
            return await src.AndWriteFileAsync(folder.ToDirectoryInfo(), filename, fileStreamBuffer, options, token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline to the file.
        /// <para>Returns the <seealso cref="FileInfo"/> object of that the written file</para>
        /// </summary> 
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="folder">directory information of folder where file will be created</param>
        /// <param name="filename">name of file. If not supplied a new GUID string will be used instead (without extension)</param>
        /// <param name="fileStreamBuffer">Buffer size of the file stream</param>
        /// <param name="options">File options</param>
        /// <param name="token">Cancellation token to observe</param>
        public static async Task<FileInfo> AndWriteFileAsync(this Func<PushFuncStream, Task> src,
            DirectoryInfo folder,
            string filename = null,
            int fileStreamBuffer = StdLookUps.DefaultFileBufferSize,
            FileOptions options = FileOptions.SequentialScan,
            CancellationToken token = default(CancellationToken))
        {
            var targetFile = folder.CreateFileInfo(filename ?? Guid.NewGuid().ToString("N"));
            await src.AndWriteFileAsync(targetFile, fileStreamBuffer, options, token).ConfigureAwait(false);
            return targetFile;
        }

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline to the file.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="fileinfo">file info object of the file to create/rewrite.</param>
        /// <param name="fileStreamBuffer">Buffer size of the file stream</param>
        /// <param name="options">File options</param>
        /// <param name="token">Cancellation token to observe</param>
        public static async Task AndWriteFileAsync(this Func<PushFuncStream, Task> src,
            FileInfo fileinfo,
            int fileStreamBuffer = StdLookUps.DefaultFileBufferSize,
            FileOptions options = FileOptions.SequentialScan,
            CancellationToken token = default(CancellationToken))
        {
            using (var strm = fileinfo.CreateStream(FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                fileStreamBuffer, options))
            {
                await src.AndWriteStreamAsync(strm, false, token).ConfigureAwait(false);
                await strm.FlushAsync(token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline and returns the results as byte array.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="token">Cancellation token to observe</param>
        /// <param name="estimatedSize">Intial guess for the size of the byte array (optimization on resizing operation).</param>
        public static async Task<byte[]> AndWriteBytesAsync(this Func<PushFuncStream, Task> src,
            CancellationToken token = default(CancellationToken),
            int estimatedSize = StdLookUps.DefaultBufferSize)
        {
            return (await src.AndWriteBufferAsync(token, false, estimatedSize).ConfigureAwait(false)).ToArray();
        }

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline and returns the contents as newly created <seealso cref="MemoryStream"/>.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="token">Cancellation token to observe</param>
        /// <param name="seekToOrigin">If true, Seek with <seealso cref="SeekOrigin.Begin"/> is performed else not.</param>
        /// <param name="initialSize">Initial Memory buffer Size</param>
        public static async Task<MemoryStream> AndWriteBufferAsync(this Func<PushFuncStream, Task> src,
            CancellationToken token = default(CancellationToken), 
            bool seekToOrigin = false,
            int initialSize = StdLookUps.DefaultBufferSize)
        {
            var ms = new MemoryStream(initialSize);
            await src.AndWriteStreamAsync(ms, false, token).ConfigureAwait(false);
            if (seekToOrigin) ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// Call to this method shall bootstrap the streaming pipeline and returns the associated asynchronous task that 
        /// pushes data throw the pipeline and appends the contents to the given <seealso cref="Stream"/>.
        /// </summary>
        /// <param name="src">Current pipe of the pipeline</param>
        /// <param name="writableTarget">Target stream to write on</param>
        /// <param name="disposeTarget">If true, target stream is disposed else left open.</param>
        /// <param name="token">Cancellation token to observe</param>
        public static async Task AndWriteStreamAsync(this Func<PushFuncStream, Task> src,
            Stream writableTarget,
            bool disposeTarget = false,
            CancellationToken token = default(CancellationToken))
        {
            await src(new PushFuncStream(writableTarget, disposeTarget, token)).ConfigureAwait(false);
        }

        #endregion String Finalization

        /// <summary>
        /// Data structure to facilitate Push based functional streaming,
        /// i.e., 1st Pipe writes on 2nd that writes on 3rd and so on and so forth...
        /// </summary>
        public struct PushFuncStream
        {
            /// <summary>
            /// Writable stream
            /// </summary>
            public Stream Writable { get; }

            /// <summary>
            /// If true, stream is disposed at the end of streaming else left open
            /// </summary>
            public bool Dispose { get; }

            /// <summary>
            /// Associated Cancellation token
            /// </summary>
            public CancellationToken Token { get; }

            /// <summary>
            /// Ctor.
            /// </summary>
            /// <param name="writable">writable stream</param>
            /// <param name="dispose">true to dispose at the end of streaming else false</param>
            /// <param name="token">Cancellation token to observe</param>
            /// <exception cref="DdnDfException"></exception>
            public PushFuncStream(Stream writable, bool dispose, CancellationToken token)
            {
                Writable = writable.CanWrite.ThrowIfNot(DdnDfErrorCode.Unspecified, "Cannot write on the stream",
                    writable);
                Dispose = dispose;
                Token = token;
            }
        }
    }
}