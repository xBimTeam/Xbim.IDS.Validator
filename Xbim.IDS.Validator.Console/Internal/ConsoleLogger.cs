using System;
using Xbim.IDS.Validator.Core;
using static System.ConsoleColor;

namespace Xbim.IDS.Validator.Console.Internal;

/// <summary>
/// Controls log levels
/// </summary>
public enum Verbosity
{
    /// <summary>
    /// No output except console errors.
    /// </summary>
    /// <remarks>For use in unattended mode. An ExitCode &gt; 0 indicates number of failed specs</remarks>
    Quiet = 1,
    /// <summary>
    /// Output minimal logs
    /// </summary>
    Minimal = 2,
    /// <summary>
    /// The regular log levels
    /// </summary>
    Normal = 3,
    /// <summary>
    /// Detailed logging output
    /// </summary>
    Detailed = 4,
    /// <summary>
    /// Output trace info
    /// </summary>
    Verbose = 5
}

/// <summary>
/// A wrapper around <see cref="Console.Write"/> that supports <see cref="Verbosity"/> levels and reliable
/// coloring.
/// </summary>
public class ConsoleLogger
{
    public ConsoleLogger(Verbosity verbosity)
    {
        MinVerbosity = verbosity;
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
    }

    private Stack<ConsoleColor> foreColorStack = new();

    public Verbosity MinVerbosity { get; }
    public ConsoleColor CurrentColor { get => System.Console.ForegroundColor; }

    public ConsoleLogger WriteTrace(ConsoleColor color, string message, params object[]? args) => SetColor(color).Write(Verbosity.Verbose, message, args);

    public ConsoleLogger WriteDetail(ConsoleColor color, string message, params object[]? args) => SetColor(color).Write(Verbosity.Detailed, message, args);
    public ConsoleLogger WriteInfo(ConsoleColor color, string message, params object[]? args) => SetColor(color).Write(Verbosity.Normal, message, args);
    public ConsoleLogger WriteWarning(ConsoleColor color, string message, params object[]? args) => SetColor(color).Write(Verbosity.Minimal, message, args);
    public ConsoleLogger WriteImportant(ConsoleColor color, string message, params object[]? args) => SetColor(color).Write(Verbosity.Minimal, message, args);

    public ConsoleLogger WriteTraceLine(string message, params object[]? args) => WriteTraceLine(CurrentColor, message, args);
    public ConsoleLogger WriteTraceLine(ConsoleColor color, string message, params object[]? args) => SetColor(color).WriteLine(Verbosity.Verbose, message, args);
    public ConsoleLogger WriteDetailLine(string message, params object[]? args) => WriteDetailLine(CurrentColor, message, args);
    public ConsoleLogger WriteDetailLine(ConsoleColor color, string message, params object[]? args) => SetColor(color).WriteLine(Verbosity.Detailed, message, args);
    public ConsoleLogger WriteInfoLine(string message, params object[]? args) => WriteInfoLine(CurrentColor, message, args);
    public ConsoleLogger WriteInfoLine(ConsoleColor color, string message, params object[]? args) => SetColor(color).WriteLine(Verbosity.Normal, message, args);
    public ConsoleLogger WriteWarningLine(string message, params object[]? args) => WriteWarningLine(CurrentColor, message, args);
    public ConsoleLogger WriteWarningLine(ConsoleColor color, string message, params object[]? args) => SetColor(color).WriteLine(Verbosity.Minimal, message, args);
    public ConsoleLogger WriteImportantLine(string message, params object[]? args) => WriteImportantLine(CurrentColor, message, args);
    public ConsoleLogger WriteImportantLine(ConsoleColor color, string message, params object[]? args) => SetColor(color).WriteLine(Verbosity.Minimal, message, args);

    public ConsoleLogger WriteColored(ValidationStatus status, string message, params object[]? args)
    {
        var color = GetColorForStatus(status);

        return status switch
        {
            ValidationStatus.Pass => WriteInfo(color, message, args),
            ValidationStatus.Inconclusive => WriteDetail(color, message, args),
            ValidationStatus.Skipped => WriteDetail(color, message, args),
            ValidationStatus.Fail => WriteWarning(color, message, args),
            ValidationStatus.Error => WriteImportant(color, message, args),
            _ => WriteImportant(color, message, args),
        };
    }

    public ConsoleColor GetColorForStatus(ValidationStatus status)
    {

        return status switch
        {
            ValidationStatus.Pass => Green,
            ValidationStatus.Inconclusive => Yellow,
            ValidationStatus.Skipped => Yellow,
            ValidationStatus.Fail => Red,
            ValidationStatus.Error => Red,
            _ => DarkRed,
        };
    }
    public ConsoleLogger SetColor(ConsoleColor color)
    {
        foreColorStack.Push(System.Console.ForegroundColor);
        System.Console.ForegroundColor = color;
        return this;
    }

    public ConsoleLogger ResetColor()
    {
        if (foreColorStack.Count > 0)
            System.Console.ForegroundColor = foreColorStack.Pop();
        return this;
    }

    private ConsoleLogger Write(Verbosity level, string message, params object[]? args)
    {
        if (MinVerbosity >= level)
        {
            if (args == null || args.Length == 0)
            {
                System.Console.Write(message);
            }
            else
            {
                System.Console.Write(message, args);
            }
        }
        ResetColor();
        return this;
    }

    private ConsoleLogger WriteLine(Verbosity level, string message, params object[]? args)
    {
        Write(level, message, args);
        Write(level, Environment.NewLine);
        return this;
    }
}
