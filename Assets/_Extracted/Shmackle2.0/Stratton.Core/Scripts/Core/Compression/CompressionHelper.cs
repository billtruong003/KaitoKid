using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using zlib;

namespace Stratton.Core
{
    public static class CompressionHelper
    {
        #region PublicMethods
        private const int STATIC_BUFFER_LEN = 5120;
        private static byte[] staticBuffer = new byte[STATIC_BUFFER_LEN];

        public static IEnumerator Compress(string text, Action<string> callback)
        {
            yield return CompressDataCoroutine(
                Encoding.UTF8.GetBytes(text), 
                (data) => callback(Convert.ToBase64String(data))
            );
        }

        public static string Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            byte[] gzBuffer = new byte[0];
            try
            {
                CompressData(buffer, out gzBuffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return Convert.ToBase64String(gzBuffer);
        }
		
        public static byte[] Compress(byte[] buffer)
        {
            byte[] gzBuffer = new byte[0];
            try
            {
                CompressData(buffer, out gzBuffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return gzBuffer;
        }
		
        public static byte[] CompressToBytes(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            byte[] gzBuffer = new byte[0];
            try
            {
                CompressData(buffer, out gzBuffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return gzBuffer;
        }
		
        public static string DecompressToString(byte[] bytes)
        {
            byte[] buffer = new byte[0];
            try
            {
                DecompressData(bytes, out buffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public static string Decompress(string compressedText)
        {
            byte[] gzBuffer = Convert.FromBase64String(compressedText);
            byte[] buffer = new byte[0];
            try
            {
                DecompressData(gzBuffer, out buffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public static byte[] Decompress(byte[] compressedBuffer)
        {
            byte[] buffer = new byte[0];
            try
            {
                DecompressData(compressedBuffer, out buffer);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            return buffer;
        }

        public static void CompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            {
                using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
                {
                    using (Stream inMemoryStream = new MemoryStream(inData))
                    {
                        CopyStream(inMemoryStream, outZStream);
                        outZStream.finish();
                        outData = outMemoryStream.ToArray();
                    }
                }
            }
        }

        public static void DecompressData(byte[] inData, out byte[] outData)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DecompressData");
            using (MemoryStream outMemoryStream = new MemoryStream())
            {
                using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
                {
                    using (Stream inMemoryStream = new MemoryStream(inData))
                    {
                        CopyStream(inMemoryStream, outZStream);
                        outZStream.finish();
                        outData = outMemoryStream.ToArray();
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public static IEnumerator CompressDataCoroutine(byte[] inData, Action<byte[]> callback)
        {
            // profiled, looks good the same GC collection amount as in CompressData 
            // but brokent into 3 main parts that happends in separate frames

            MemoryStream outMemoryStream = new MemoryStream();
            ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION);
            Stream inMemoryStream = new MemoryStream(inData);

            yield return null;

            CopyStream(inMemoryStream, outZStream);
            yield return null;

            outZStream.finish();
            yield return null;

            byte[] outData = outMemoryStream.ToArray();
            yield return null;

            outMemoryStream.Close();
            yield return null;

            callback(outData);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            int len;
            while ((len = input.Read(staticBuffer, 0, STATIC_BUFFER_LEN)) > 0)
            {
                output.Write(staticBuffer, 0, len);
            }
            output.Flush();
        }

        #endregion
    }
}