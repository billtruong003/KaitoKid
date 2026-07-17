using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Stratton.Core
{
    public static class ObjectExtensions
    {
        #region Public Methods

        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T) formatter.Deserialize(stream);
            }
        }
        public static void Log(this object obj, object log)
        {
            Core.Log.Message(BaseLogChannel.Core, "[{0}] {1}".SetArgs(obj.GetType().Name,log));
        }
        public static void Log(this object obj , string log, params object[] parameters)
        {
            Core.Log.Message(BaseLogChannel.Core, "[{0}] {1}".SetArgs(obj.GetType().Name, parameters.Length > 0? log.SetArgs(parameters) : log));
        }

        public static void LogError(this object obj, string log, params object[] parameters)
        {
            Core.Log.Error(BaseLogChannel.Core, "[{0}] {1}".SetArgs(obj.GetType().Name, parameters.Length > 0 ? log.SetArgs(parameters) : log));
        }

        public static void LogWarn(this object obj, string log, params object[] parameters)
        {
            Core.Log.Warning(BaseLogChannel.Core, "[{0}] {1}".SetArgs( obj.GetType().Name, parameters.Length > 0 ? log.SetArgs(parameters) : log));
        }


        public static void DLog(this object obj, string log, params object[] parameters)
        {
            Core.Log.Message(BaseLogChannel.Core, "[{0}_{1}] {2}".SetArgs(obj.GetType().Name, UnityEngine.Time.frameCount % 900 + 100, parameters.Length > 0 ? log.SetArgs(parameters) : log));
        }

        public static void DLogError(this object obj, string log, params object[] parameters)
        {
            Core.Log.Error(BaseLogChannel.Core, "[{0}_{1}] {2}".SetArgs(obj.GetType().Name, UnityEngine.Time.frameCount % 900 + 100, parameters.Length > 0 ? log.SetArgs(parameters) : log));
        }

        public static void DLogWarn(this object obj, string log, params object[] parameters)
        {
            Core.Log.Warning(BaseLogChannel.Core, "[{0}_{1}] {2}".SetArgs(obj.GetType().Name, UnityEngine.Time.frameCount % 900 + 100, parameters.Length > 0 ? log.SetArgs(parameters) : log));
        }
        #endregion
    }
}