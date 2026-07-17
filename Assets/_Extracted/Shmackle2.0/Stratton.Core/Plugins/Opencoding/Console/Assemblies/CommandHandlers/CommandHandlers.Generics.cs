using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Opencoding.CommandHandlerSystem
{
    public static partial class CommandHandlers
    {
        // For registering properties
        public static void RegisterCommandHandler<T>(Func<T> getDelegate, Action<T> setDelegate, string commandName, string description = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(getDelegate, setDelegate, commandName, description, strongReference);
        }

        public static void RegisterCommandHandler(Action action, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler(Delegate del, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(del, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler<T1>(Action<T1> action, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler<T1, T2>(Action<T1, T2> action, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler<T1, T2, T3>(Action<T1, T2, T3> action, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, string commandName = null, string description = null, Type[] types = null, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, types, strongReference);
        }

        public static void RegisterCommandHandler(Delegate del, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(del, commandName, description, paramInfos, strongReference);
        }

        public static void RegisterCommandHandler(Action action, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, paramInfos, strongReference);
        }

        public static void RegisterCommandHandler<T1>(Action<T1> action, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, paramInfos, strongReference);
        }

        public static void RegisterCommandHandler<T1,T2>(Action<T1,T2> action, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, paramInfos, strongReference);
        }

        public static void RegisterCommandHandler<T1, T2, T3>(Action<T1, T2, T3> action, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, paramInfos, strongReference);
        }

        public static void RegisterCommandHandler<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = true)
        {
            RegisterCommandHandler_internal(action, commandName, description, paramInfos, strongReference);
        }
    }
}
