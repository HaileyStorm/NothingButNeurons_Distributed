using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NothingButNeurons.Debugger;

/// <summary>
/// Represents a message used to update the DebugUI subscription.
/// </summary>
public record DebugUISubUpdateMessage(DebugSeverity Severity = DebugSeverity.Trace, string Context = "", string Summary = "", string Message = "", string SenderClass = "", string SenderName = "", string ParentName = "") : Message;
/// <summary>
/// Represents a message used to include or exclude sender, parent, and server received time information in the DebugUI.
/// </summary>
public record DebugUIIncludesMessage(bool IncludeSenderInfo, bool IncludeParentInfo, bool IncludeServerReceivedTime) : Message;

/// <summary>
/// Provides a debug user interface for displaying debug messages in a RichTextBox or TextBox control.
/// </summary>
internal partial class DebugUI : ActorBase
{
    // Private properties for UI elements, filtering, and UI update control.
    private TextBox? DebugTextBox { get; init; }
    private RichTextBox? DebugRichTextBox { get; init; }
    private List<DebugInboundMessage> AllReceived { get; set; }
    private DebugSeverity CurrentSeverity { get; set; }

    // Regex properties for filtering the displayed debug messages.
    private Regex CurrentContext { get; set; }
    private Regex CurrentSummary { get; set; }
    private Regex CurrentMessage { get; set; }
    private Regex CurrentSenderClass { get; set; }
    private Regex CurrentSenderName { get; set; }
    private Regex CurrentParentName { get; set; }

    // Display settings for sender and parent information, and server received time.
    private bool IncludeSenderInfo { get; set; }
    private bool IncludeParentInfo { get; set; }
    private bool IncludeServerReceivedTime { get; set; }

    // Constants and variables used for auto-flushing and throttling UI updates.
    private const int AutoFlushAt = 300;
    private const int AutoFlushTo = 150;
    private readonly TimeSpan RTBUpdateInterval = TimeSpan.FromMilliseconds(500);
    private DateTime LastRTBUpdate;
    private readonly object RTBLock = new object();

    /// <summary>
    /// Initializes a new instance of the DebugUI class with a TextBox control.
    /// </summary>
    /// <param name="debugTextBox">The TextBox control to display debug messages.</param>
    public DebugUI(TextBox debugTextBox)
    {
        DebugTextBox = debugTextBox;
    }
    /// <summary>
    /// Initializes a new instance of the DebugUI class with a RichTextBox control and optional inclusion settings.
    /// </summary>
    /// <param name="debugRichTextBox">The RichTextBox control to display debug messages.</param>
    /// <param name="includeSenderInfo">Whether to include sender information in the displayed messages.</param>
    /// <param name="includeParentInfo">Whether to include parent information in the displayed messages.</param>
    /// <param name="includeServerReceivedTime">Whether to include the server received time in the displayed messages.</param>
    public DebugUI(RichTextBox debugRichTextBox, bool includeSenderInfo = true, bool includeParentInfo = false, bool includeServerReceivedTime = false)
    {
        // Initialization of DebugRichTextBox and related properties.
        DebugRichTextBox = debugRichTextBox;
        DebugRichTextBox.Document = new();
        AllReceived = new();
        CurrentSeverity = DebugSeverity.Trace;
        CurrentContext = MatchAll();
        CurrentSummary = MatchAll();
        CurrentMessage = MatchAll();
        CurrentSenderClass = MatchAll();
        CurrentSenderName = MatchAll();
        CurrentParentName = MatchAll();
        IncludeSenderInfo = includeSenderInfo;
        IncludeParentInfo = includeParentInfo;
        IncludeServerReceivedTime = includeServerReceivedTime;
    }

    /// <summary>
    /// Overrides the ReceiveMessage method to process incoming messages and update the debug user interface accordingly.
    /// </summary>
    /// <param name="context">The current actor context.</param>
    /// <returns>True if the message was processed; otherwise, false.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        bool processed = false;

        switch (context.Message)
        {
            // Handles updates to the DebugUI subscription.
            case DebugUISubUpdateMessage msg:
                processed = true;

                CurrentSeverity = msg.Severity;
                CurrentContext = StrToRegex(msg.Context);
                CurrentSummary = StrToRegex(msg.Summary);
                CurrentMessage = StrToRegex(msg.Message);
                CurrentSenderClass = StrToRegex(msg.SenderClass);
                CurrentSenderName = StrToRegex(msg.SenderName);
                CurrentParentName = StrToRegex(msg.ParentName);

                if (DebugRichTextBox == null)
                {
                    SubscribeToDebugs(msg.Severity, msg.Context);
                } else
                {
                    // In case we were totally unsubscribed before.
                    SubscribeToDebugs();
                    UpdateRTB();
                }

                break;
            // Handles incoming debug messages and adds them to the text box or rich text box.
            case DebugInboundMessage msg:
                processed = true;

                if (DebugTextBox != null)
                {
                    DebugTextBox.Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrEmpty(DebugTextBox.Text)) DebugTextBox.Text += "\n\n";
                        DebugTextBox.Text += msg;
                    });
                }
                else if (DebugRichTextBox != null)
                {
                    if (!string.Equals(msg.Context, "AutoFlush", StringComparison.InvariantCultureIgnoreCase))
                        AllReceived.Add(msg);

                    if (AllReceived.Count > AutoFlushAt)
                    {
                        int oCount = AllReceived.Count;
                        foreach (DebugInboundMessage m in new List<DebugInboundMessage>(AllReceived.Where(m => (string.Equals(m.Context, "DebugUI", StringComparison.InvariantCultureIgnoreCase) || string.Equals(m.Context, "AutoFlush", StringComparison.InvariantCultureIgnoreCase)) && (byte)m.Severity < (byte)DebugSeverity.Warning)))
                        {
                            AllReceived.Remove(m);
                        }
                        if (AllReceived.Count > AutoFlushTo)
                        {
                            foreach (DebugSeverity severity in Enum.GetValues(typeof(DebugSeverity)))
                            {
                                foreach (DebugInboundMessage m in new List<DebugInboundMessage>(AllReceived.Where(m => m.Severity == severity)))
                                {
                                    AllReceived.Remove(m);
                                    if (AllReceived.Count <= AutoFlushTo)
                                        goto flushed;
                                }
                            }
                        }
                    flushed:
                        // Send message (so other subscribers such as DebugFileWriter), but also add to AllReceived immediately. Otherwise, the message comes in later and we end up with a stack of flush messages at the end, instead of the  desired only having one flush message in the current list (with non-flushed items before and stuff since after). Accordingly, AutoFlush messages are not added to AllReceived when received, see above.
                        DebugOutboundMessage flushMsg = new(DebugSeverity.Info, "AutoFlush", "Debug log auto-flushed.", $"{oCount - AllReceived.Count} lowest-severity/oldest logs removed.", this.GetType().ToString(), SelfPID == null ? "" : SelfPID.Id, SelfPID == null ? "" : SelfPID.Address, ParentPID == null ? "" : ParentPID.Id, ParentPID == null ? "" : ParentPID.Address, DateTimeOffset.Now.ToUnixTimeMilliseconds());
                        SendDebugMessage(flushMsg);
                        AllReceived.Add(new DebugInboundMessage(flushMsg, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                    }
                    if (msg.Severity >= CurrentSeverity && CurrentContext.IsMatch(msg.Context) && CurrentSummary.IsMatch(msg.Summary) && CurrentMessage.IsMatch(msg.Message) && CurrentSenderClass.IsMatch(msg.SenderClass) && CurrentSenderName.IsMatch(msg.SenderName) && CurrentParentName.IsMatch(msg.ParentName))
                        UpdateRTB();
                }
                
                break;
            // Handles requests to flush debug messages.
            case DebugFlushMessage msg:
                processed = true;

                AllReceived = new();
                UpdateRTB();
                SendDebugMessage(DebugSeverity.Info, "DebugUI", "Debug log flushed.");

                break;
            // Handles updates to include/exclude message details.
            case DebugUIIncludesMessage msg:
                processed = true;

                IncludeSenderInfo = msg.IncludeSenderInfo;
                IncludeParentInfo = msg.IncludeParentInfo;
                IncludeServerReceivedTime = msg.IncludeServerReceivedTime;
                UpdateRTB();

                break;
        }

        return processed;
    }

    /// <summary>
    /// Updates the RichTextBox control with the latest debug messages according to the current subscription settings.
    /// </summary>
    private void UpdateRTB()
    {
        if (DebugRichTextBox == null) return;

        #region Throttle updates
        DateTime now = DateTime.Now;
        if (now - LastRTBUpdate < RTBUpdateInterval)
        {
            return;
        }

        lock (RTBLock)
        {
            if (now - LastRTBUpdate < RTBUpdateInterval)
            {
                return;
            }
            LastRTBUpdate = now;
            #endregion Throttle updates

            DebugRichTextBox.Dispatcher.Invoke(() =>
            {
                static FontWeight Bolder(FontWeight inWeight)
                {
                    int weight = inWeight.ToOpenTypeWeight();
                    return FontWeight.FromOpenTypeWeight(Math.Min(weight + 100, 999));
                }

                DebugRichTextBox.Document.Blocks.Clear();

                if (CurrentSeverity == DebugSeverity.Trace && CurrentContext.ToString() == MatchAll().ToString() && CurrentMessage.ToString() == MatchAll().ToString() && CurrentSenderClass.ToString() == MatchAll().ToString() && CurrentSenderName.ToString() == MatchAll().ToString() && CurrentParentName.ToString() == MatchAll().ToString())
                {
                    DebugRichTextBox.Document.Blocks.Add(new Paragraph(new Run("When Debug Severity is set to Trace, please enable at least one other filter.")));
                }
                else
                {
                    SolidColorBrush brush;
                    FontWeight weight;
                    double size;
                    foreach (DebugInboundMessage item in AllReceived.Where(a => a.Severity >= CurrentSeverity && CurrentContext.IsMatch(a.Context) && CurrentSummary.IsMatch(a.Summary) && CurrentMessage.IsMatch(a.Message) && CurrentSenderClass.IsMatch(a.SenderClass) && CurrentSenderName.IsMatch(a.SenderName) && CurrentParentName.IsMatch(a.ParentName)))
                    {
                        switch (item.Severity)
                        {
                            case DebugSeverity.Trace:
                                brush = new SolidColorBrush(Colors.LightSteelBlue);
                                weight = FontWeights.Light;
                                break;
                            case DebugSeverity.Info:
                                brush = new SolidColorBrush(Colors.DeepSkyBlue);
                                weight = FontWeights.Light;
                                break;
                            case DebugSeverity.Debug:
                                brush = new SolidColorBrush(Colors.Green);
                                weight = FontWeights.Normal;
                                break;
                            case DebugSeverity.Warning:
                                brush = new SolidColorBrush(Colors.Goldenrod);
                                weight = FontWeights.Medium;
                                break;
                            case DebugSeverity.Alert:
                                brush = new SolidColorBrush(Colors.Orange);
                                weight = FontWeights.Medium;
                                break;
                            case DebugSeverity.Error:
                                brush = new SolidColorBrush(Colors.Tomato);
                                weight = FontWeights.SemiBold;
                                break;
                            case DebugSeverity.Critical:
                                brush = new SolidColorBrush(Colors.Red);
                                weight = FontWeights.Bold;
                                break;
                            case DebugSeverity.Test:
                                brush = new SolidColorBrush(Colors.DeepPink);
                                weight = FontWeights.Bold;
                                break;
                            default:
                                brush = new SolidColorBrush(Colors.Black);
                                break;
                        }
                        size = item.Severity == DebugSeverity.Test ? 13d : 11d;

                        Paragraph paragraph = new Paragraph();

                        bool needsNewline = false;
                        if (item.MessageSentTime != 0)
                        {
                            paragraph.Inlines.Add(new Run($"{CCSL.DatesAndTimes.UnixTimeToLocalDateTime(item.MessageSentTime):MM/dd/yyyy hh:mm:ss.fff tt}") { Foreground = brush, FontWeight = weight, FontSize = size - 2d, FontStyle = FontStyles.Italic });
                            needsNewline = true;
                        }
                        if (!String.IsNullOrEmpty(item.Context))
                        {
                            paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}{item.Context}") { Foreground = brush, FontWeight = weight, FontSize = size, FontStyle = FontStyles.Italic });
                            needsNewline = true;
                        }
                        if (!String.IsNullOrEmpty(item.Summary))
                        {
                            paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}{item.Summary}") { Foreground = brush, FontWeight = Bolder(weight), FontSize = size });
                            needsNewline = true;
                        }
                        if (!String.IsNullOrEmpty(item.Message))
                        {
                            paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}{item.Message}") { Foreground = brush, FontWeight = weight, FontSize = size });
                            needsNewline = true;
                        }
                        if (IncludeSenderInfo)
                        {
                            string sender;
                            if (String.IsNullOrEmpty(item.SenderSystemAddr))
                            {
                                if (String.IsNullOrEmpty(item.SenderName))
                                {
                                    sender = "";
                                }
                                else
                                {
                                    sender = item.SenderName;
                                }
                            }
                            else
                            {
                                sender = item.SenderSystemAddr;
                                if (!String.IsNullOrEmpty(item.SenderName))
                                {
                                    sender += $"/{item.SenderName}";
                                }
                            }
                            if (String.IsNullOrEmpty(sender))
                            {
                                if (!String.IsNullOrEmpty(item.SenderClass))
                                {
                                    sender = $"Sender Class: {item.SenderClass}";
                                }
                            }
                            else
                            {
                                sender = "Sender: " + sender;
                                if (!String.IsNullOrEmpty(item.SenderClass))
                                {
                                    sender += $", of Class: {item.SenderClass}";
                                }
                            }
                            if (!String.IsNullOrEmpty(sender))
                            {
                                paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}{sender}") { Foreground = brush, FontWeight = weight, FontSize = size - 2d, FontStyle = FontStyles.Italic });
                                needsNewline = true;
                            }
                        }
                        if (IncludeParentInfo)
                        {
                            string parent;
                            if (String.IsNullOrEmpty(item.ParentSystemAddr))
                            {
                                if (String.IsNullOrEmpty(item.ParentName))
                                {
                                    parent = "";
                                }
                                else
                                {
                                    parent = item.ParentName;
                                }
                            }
                            else
                            {
                                parent = item.ParentSystemAddr;
                                if (!String.IsNullOrEmpty(item.ParentName))
                                {
                                    parent += $"/{item.ParentName}";
                                }
                            }
                            if (!String.IsNullOrEmpty(parent))
                            {
                                parent = $"Parent: {parent}";
                                paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}{parent}") { Foreground = brush, FontWeight = weight, FontSize = size - 2d, FontStyle = FontStyles.Italic });
                                needsNewline = true;
                            }
                        }
                        if (IncludeServerReceivedTime && item.ServerReceivedTime != 0)
                            paragraph.Inlines.Add(new Run($"{(needsNewline ? "\n" : "")}Debug Server received: {CCSL.DatesAndTimes.UnixTimeToLocalDateTime(item.ServerReceivedTime):MM/dd/yyyy hh:mm:ss.fff tt}") { Foreground = brush, FontWeight = weight, FontSize = size - 2d, FontStyle = FontStyles.Italic });

                        DebugRichTextBox.Document.Blocks.Add(paragraph);
                    }
                }

                DebugRichTextBox.UpdateLayout();
                DebugRichTextBox.ScrollToEnd();
            });
        }
    }

    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        if (DebugRichTextBox != null)
        {
            SubscribeToDebugs();
        }
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }

    /// <summary>
    /// Converts a string pattern to a Regex object. If the pattern is invalid, returns a Regex that matches all strings.
    /// </summary>
    /// <param name="pattern">The string pattern to convert.</param>
    /// <returns>A Regex object representing the pattern.</returns>
    private static Regex StrToRegex(string pattern)
    {
        Regex rx;
        if (string.IsNullOrEmpty(pattern))
        {
            rx = MatchAll();
        }
        else
        {
            try
            {
                rx = new Regex(pattern);
            }
            catch (RegexParseException)
            {
                rx = MatchAll();
            }
        }
        return rx;
    }

    /// <summary>
    /// Generates a Regex object that matches all strings.
    /// </summary>
    /// <returns>A Regex object that matches all strings.</returns>
    [GeneratedRegex(".*")]
    private static partial Regex MatchAll();
}
