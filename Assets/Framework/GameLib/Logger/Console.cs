
using SysConsole = System.Console;
using SysDebug = System.Diagnostics.Debug;
using UConsole = UnityEngine.Debug;
using UDebug = UnityEngine.Debug;

namespace Game.Diagnostics
{

    public enum DLogType
    {
        Assert,
        Error,
        Exception,
        Warning,
        System,
        Log,
        AI,
        Audio,
        Content,
        Logic,
        GUI,
        Input,
        Network,
        Physics
    }

    public class Console
    {
        static bool isUnityEnv = false;
        static object cl = new object();
        static Console()
        {
            lock (cl)
            {
                try
                {
                    UConsole.Log("test UConsole");
                    isUnityEnv = true;
                }
                catch (System.Exception)
                {
                    isUnityEnv = false;
                }
            }
        }

        public static void Log(object message, DLogType type = DLogType.Log) {
            lock (cl)
            {
                if (isUnityEnv)
                {
                    UConsole.Log(message);
                }
                else
                {
                    SysConsole.WriteLine(message);
                }
            }
        }

        public static void LogWarning(object message, DLogType type = DLogType.Log)
        {
            lock (cl)
            {
                if (isUnityEnv)
                {
                    UConsole.LogWarning(message);
                }
                else
                {
                    SysConsole.WriteLine(message);
                }
            }
        }

        public static void LogError(object message, DLogType type = DLogType.Log)
        {
            lock (cl)
            {
                if (isUnityEnv)
                {
                    UConsole.LogError(message);
                }
                else
                {
                    SysConsole.WriteLine(message);
                }
            }
        }

        public static void Assert(bool condition) {
            if (isUnityEnv)
            {
                UDebug.Assert(condition);
            }
            else
            {
                SysDebug.Assert(condition);
            }
        }
        public static void Assert(bool condition, string message) {
            if (isUnityEnv)
            {
                UDebug.Assert(condition,message);
            }
            else
            {
                SysDebug.Assert(condition,message);
            }
        }
    }
}

namespace Game.Diagnostics
{
    public class Debug : Console
    {

    }
}
