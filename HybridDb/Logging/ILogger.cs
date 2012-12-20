﻿using System;

namespace HybridDb.Logging
{
    public interface ILogger
    {
        /// <summary>
        ///     Writes the specified message with the DEBUG level
        /// </summary>
        void Debug(string message, params object[] objs);

        /// <summary>
        ///     Writes the specified message with the INFO level
        /// </summary>
        void Info(string message, params object[] objs);

        /// <summary>
        ///     Writes the specified message with the WARN level
        /// </summary>
        void Warn(string message, params object[] objs);

        /// <summary>
        ///     Writes the specified message with the ERROR level
        /// </summary>
        void Error(string message, params object[] objs);

        /// <summary>
        ///     Writes the specified message with the ERROR level and includes the full details of the specified exception
        /// </summary>
        void Error(string message, Exception exception, params object[] objs);
    }
}

