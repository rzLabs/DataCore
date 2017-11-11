using System;

namespace DataCore
{
    /// <summary>
    /// GUI / Console compatible message containing the message string and formatting information
    /// </summary>
    public class MessageArgs : EventArgs
    {
        /// <summary>
        /// String containing the message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Determines if this message needs to be tabbed [Console Only]
        /// </summary>
        public bool Tab { get; set; }
        /// <summary>
        /// Determines the amount of tabs to be prepended to the message [Console Only]
        /// </summary>
        public int TabCount { get; set; }
        /// <summary>
        /// Determines if this message needs to contain a line break [Console Only]
        /// </summary>
        public bool Break { get; set; }
        /// <summary>
        /// Determines the amount of line breaks to be appended to the message [Console Only]
        /// </summary>
        public int BreakCount { get; set; }

        public MessageArgs(string message) { Message = message; Tab = false; TabCount = 0; }
        public MessageArgs(string message, bool tab) { Message = message; Tab = true; TabCount = 1; }
        public MessageArgs(string message, bool tab, int tabCount) { Message = message; Tab = true; TabCount = tabCount; }
        public MessageArgs(string message, bool tab, int tabCount, bool @break) { Message = message; Tab = true; TabCount = tabCount;  Break = @break;  BreakCount = 1; }
        public MessageArgs(string message, bool tab, int tabCount, bool @break, int breakCount) { Message = message; Tab = true; TabCount = tabCount; Break = @break; BreakCount = breakCount; }
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

    /// <summary>
    /// Houses arguments intended for Console applications
    /// </summary>
    public class CurrentResetArgs : EventArgs
    {
        /// <summary>
        /// Determines if [OK] should be appended to the current line of a console when this event occurs
        /// </summary>
        public bool WriteOK { get; set; }

        /// <summary>
        /// Constructor for the CurrentResetArgs, inheriting from EventArgs
        /// </summary>
        /// <param name="writeOK">Determines if the [OK] should be appended</param>
        public CurrentResetArgs(bool writeOK) { WriteOK = writeOK; }
    }
}
