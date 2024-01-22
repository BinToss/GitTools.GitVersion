using System.Globalization;
using System.Text.RegularExpressions;

namespace GitVersion.Logging;

internal sealed class Log(params ILogAppender[] appenders) : ILog
{
    private IEnumerable<ILogAppender> appenders = appenders;
    private readonly Regex obscurePasswordRegex = new("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
    private readonly StringBuilder sb = new();
    private string indent = string.Empty;

    public Log() : this([])
    {
    }

    public Verbosity Verbosity { get; set; } = Verbosity.Normal;

    public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
    {
        if (verbosity > Verbosity)
        {
            return;
        }

        var message = args.Length != 0 ? string.Format(format, args) : format;
        var formattedString = FormatMessage(message, level.ToString().ToUpperInvariant());
        foreach (var appender in this.appenders)
        {
            appender.WriteTo(level, formattedString);
        }

        this.sb.AppendLine(formattedString);
    }

    public IDisposable IndentLog(string operationDescription)
    {
        var start = DateTime.Now;
        Write(Verbosity.Normal, LogLevel.Info, $"-< Begin: {operationDescription} >-");
        this.indent += "  ";

        return Disposable.Create(() =>
        {
            var length = this.indent.Length - 2;
            this.indent = length > 0 ? this.indent[..length] : "";
            Write(Verbosity.Normal, LogLevel.Info, string.Format(CultureInfo.InvariantCulture, "-< End: {0} (Took: {1:N}ms) >-", operationDescription, DateTime.Now.Subtract(start).TotalMilliseconds));
        });
    }

    public void Separator() => Write(Verbosity.Normal, LogLevel.Info, "-------------------------------------------------------");

    public void AddLogAppender(ILogAppender logAppender) => this.appenders = this.appenders.Concat(new[] { logAppender });

    public override string ToString() => this.sb.ToString();

    private string FormatMessage(string message, string level)
    {
        var obscuredMessage = this.obscurePasswordRegex.Replace(message, "$1$2:*******@");
        return string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", this.indent, level, DateTime.Now, obscuredMessage);
    }
}
