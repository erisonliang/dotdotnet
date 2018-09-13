﻿using System;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dot.Net.DevFast.Extensions.Internals;

namespace Dot.Net.DevFast.Extensions.StreamPipeExt
{
    public static partial class StreamPipeExts
    {
        // we keep internal extensions here
        internal static Func<PushFuncStream, Task> ApplyCompression(this Func<PushFuncStream, Task> pipe,
            bool gzip,
            CompressionLevel level)
        {
            return async pfs =>
            {
                var s = pfs.Writable;
                var t = pfs.Token;
                using (var compStrm = s.CreateCompressionStream(gzip, level, pfs.Dispose))
                {
                    await pipe(new PushFuncStream(compStrm, false, t)).StartIfNeeded().ConfigureAwait(false);
                    await compStrm.FlushAsync(t).ConfigureAwait(false);
                    await s.FlushAsync(t).ConfigureAwait(false);
                }
            };
        }

        internal static Func<PushFuncStream, Task> ApplyCrypto<T>(this Func<PushFuncStream, Task> pipe,
            T encAlg, bool encrypt)
            where T : SymmetricAlgorithm
        {
            return async pfs =>
            {
                using (encAlg)
                {
                    var cryptor = encrypt
                        ? encAlg.CreateEncryptor(encAlg.Key, encAlg.IV)
                        : encAlg.CreateDecryptor(encAlg.Key, encAlg.IV);
                    using (cryptor)
                    {
                        await pipe.ApplyTransform(cryptor)(pfs).ConfigureAwait(false);
                    }
                }
            };
        }

        internal static Func<PushFuncStream, Task> ApplyTransform(this Func<PushFuncStream, Task> pipe,
            ICryptoTransform ct)
        {
            return async pfs =>
            {
                await pipe(new PushFuncStream(
                        pfs.Writable.CreateCryptoStream(ct, CryptoStreamMode.Write, pfs.Dispose), true, pfs.Token))
                    .StartIfNeeded().ConfigureAwait(false);
            };
        }

        internal static Func<PushFuncStream, Task> ApplyLoad(this Action<int, char[], int, int> loadAction,
            int totalLen,
            Encoding enc,
            int bufferSize)
        {
            return async pfs =>
            {
                var s = pfs.Writable;
                try
                {
                    await s.CopyFromAsync(totalLen, enc, pfs.Token, bufferSize, loadAction).ConfigureAwait(false);
                }
                finally
                {
                    s.DisposeIfRequired(pfs.Dispose);
                }
            };
        }

        #region PullFuncStream TASK

        internal static Func<Task<PullFuncStream>> ApplyDecompression(this Func<Task<PullFuncStream>> pipe,
            bool gzip)
        {
            return async () =>
            {
                var data = await pipe().StartIfNeeded().ConfigureAwait(false);
                return data.ApplyDecompression(gzip);
            };
        }

        internal static Func<Task<PullFuncStream>> ApplyTransform(this Func<Task<PullFuncStream>> pipe,
            ICryptoTransform ct)
        {
            return async () =>
            {
                var data = await pipe().StartIfNeeded().ConfigureAwait(false);
                return data.ApplyTransform(ct);
            };
        }

        #endregion PullFuncStream TASK

        #region PullFuncStream NoTASK

        internal static Func<PullFuncStream> ApplyDecompression(this Func<PullFuncStream> pipe,
            bool gzip)
        {
            return () => pipe().ApplyDecompression(gzip);
        }

        internal static Func<PullFuncStream> ApplyTransform(this Func<PullFuncStream> pipe,
            ICryptoTransform ct)
        {
            return () => pipe().ApplyTransform(ct);
        }

        #endregion PullFuncStream NoTASK

        #region PullFuncStream PRIVATE

        private static PullFuncStream ApplyDecompression(this PullFuncStream data, bool gzip)
        {
            return new PullFuncStream(data.Readable.CreateDecompressionStream(gzip, data.Dispose), true);
        }

        private static PullFuncStream ApplyTransform(this PullFuncStream data, ICryptoTransform ct)
        {
            return new PullFuncStream(data.Readable.CreateCryptoStream(ct, CryptoStreamMode.Read, data.Dispose), true);
        }

        #endregion PullFuncStream PRIVATE

        internal static T InitKeyNIv<T>(this T alg, string password, string salt,
#if NET472
            HashAlgorithmName hashName,
#endif
            int loopCnt,
            Encoding enc)
            where T : SymmetricAlgorithm
        {
            var keyIv = password.CreateKeyAndIv(salt,
#if NET472
                hashName,
#endif
                alg.KeySize / 8, alg.BlockSize / 8, loopCnt, enc);
            alg.Key = keyIv.Item1;
            alg.IV = keyIv.Item2;
            return alg;
        }
    }
}