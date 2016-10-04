using System;

namespace DataCore
{
    public class ConsoleMessageArgs : EventArgs
    {
        public string Message { get; set; }
        public bool Tab { get; set; }
        public int TabCount { get; set; }
        public bool Break { get; set; }
        public int BreakCount { get; set; }

        public ConsoleMessageArgs(string message) { Message = message; Tab = false; TabCount = 0; }
        public ConsoleMessageArgs(string message, bool tab) { Message = message; Tab = true; TabCount = 1; }
        public ConsoleMessageArgs(string message, bool tab, int tabCount) { Message = message; Tab = true; TabCount = tabCount; }
        public ConsoleMessageArgs(string message, bool tab, int tabCount, bool @break) { Message = message; Tab = true; TabCount = tabCount;  Break = @break;  BreakCount = 1; }
        public ConsoleMessageArgs(string message, bool tab, int tabCount, bool @break, int breakCount) { Message = message; Tab = true; TabCount = tabCount; Break = @break; BreakCount = breakCount; }
    }

    /// <summary>
    /// Houses arguments passed to caller during raising of ErrorOccured event
    /// </summary>
    public class ErrorArgs : EventArgs
    {
        /// <summary>
        /// string containing the error message
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Constructor for the ErrorArgs, inheriting from Eventargs
        /// Assigns the Error string
        /// </summary>
        /// <param name="error">Message to be set as Error</param>
        public ErrorArgs(string error) { Error = error; }
    }

    public class WarningArgs : EventArgs
    {
        public string Warning { get; set; }

        public WarningArgs(string warning) { Warning = warning; }
    }

    /// <summary>
    /// Houses arguments passed to caller during raising of TotalMaxDetermined Event
    /// </summary>
    public class TotalMaxArgs : EventArgs
    {
        /// <summary>
        /// Maximum value that should be set to a "Total" Progressbar.Maximum
        /// </summary>
        public int Maximum { get; set; }

        /// <summary>
        /// Indicates if the DataCore is processing a group of tasks or reporting a single progresses total
        /// </summary>
        public bool IsTasks { get; set; }

        /// <summary>
        /// Constructor for the TotalMaxArgs, inheriting from EventArgs
        /// Assigns the Maximum value
        /// </summary>
        /// <param name="maximum"></param>
        public TotalMaxArgs(int maximum, bool isTasks) { Maximum = maximum; IsTasks = isTasks; }
    }

    /// <summary>
    /// Houses arguments passed to caller during raising of TotalProgressChanged Event
    /// </summary>
    public class TotalChangedArgs : EventArgs
    {
        /// <summary>
        /// Value that should be assigned to a "Total" Progressbar.Value
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Status that should be assigned to a "Total Status" label.Text
        /// </summary>
        public string Status { get; set; }

        public bool IgnoreStatus { get; set; }

        /// <summary>
        /// Constructor for TotalChangesArgs, inherits from EventArgs
        /// Assigns Value and Status
        /// </summary>
        /// <param name="value"></param>
        /// <param name="status"></param>
        public TotalChangedArgs(int value, string status)
        {
            Value = value;
            Status = status;
            IgnoreStatus = (status.Length > 0) ? false : true;
        }
    }

    public class TotalResetArgs : EventArgs
    {
        public bool WriteOK { get; set; }

        public TotalResetArgs(bool writeOK) { WriteOK = writeOK; }
    }

    /// <summary>
    /// Houses arguments passed to caller during raising of CurrentMaxDetermined Event
    /// </summary>
    public class CurrentMaxArgs : EventArgs
    {
        public long Maximum { get; set; }

        public CurrentMaxArgs(long maximum) { Maximum = maximum; }
    }

    /// <summary>
    /// Houses arguments passed to caller during raising of CurrentProgressChanged Event
    /// </summary>
    public class CurrentChangedArgs : EventArgs
    {
        /// <summary>
        /// Value that should be assigned to a 'Current' Progressbar.Value
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// String that should be assigned to a "Status" label.Text
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Constructor for the CurrentChangedArgs, inheriting from EventArgs
        /// assigns the Value/Status properties
        /// </summary>
        /// <param name="value"></param>
        /// <param name="status"></param>
        public CurrentChangedArgs(long value, string status) { Value = value; Status = status; }
    }

    public class CurrentResetArgs : EventArgs
    {
        public bool WriteOK { get; set; }

        public CurrentResetArgs(bool writeOK) { WriteOK = writeOK; }
    }
}
