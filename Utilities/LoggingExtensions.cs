using System;
using System.Diagnostics;
using ExtremeDumper.Logging;

namespace ExtremeDumper.Utilities;

/// <summary>
/// Extension methods for advanced logging functionality
/// </summary>
public static class LoggingExtensions {
	/// <summary>
	/// Log with performance timing
	/// </summary>
	public static void LogWithTiming(this object context, string operation, Action action) {
		var sw = Stopwatch.StartNew();
		Logger.Info($"[{context.GetType().Name}] Starting: {operation}");

		try {
			action();
			sw.Stop();
			Logger.Info($"[{context.GetType().Name}] ✓ Completed: {operation} ({sw.ElapsedMilliseconds}ms)");
		}
		catch (Exception ex) {
			sw.Stop();
			Logger.Error($"[{context.GetType().Name}] ✗ Failed: {operation} ({sw.ElapsedMilliseconds}ms)");
			Logger.Exception(ex);
			throw;
		}
	}

	/// <summary>
	/// Log with performance timing (generic return)
	/// </summary>
	public static T LogWithTiming<T>(this object context, string operation, Func<T> func) {
		var sw = Stopwatch.StartNew();
		Logger.Info($"[{context.GetType().Name}] Starting: {operation}");

		try {
			T result = func();
			sw.Stop();
			Logger.Info($"[{context.GetType().Name}] ✓ Completed: {operation} ({sw.ElapsedMilliseconds}ms)");
			return result;
		}
		catch (Exception ex) {
			sw.Stop();
			Logger.Error($"[{context.GetType().Name}] ✗ Failed: {operation} ({sw.ElapsedMilliseconds}ms)");
			Logger.Exception(ex);
			throw;
		}
	}

	/// <summary>
	/// Conditionally log based on level
	/// </summary>
	public static void LogConditional(this object context, bool condition, string trueMessage, string falseMessage) {
		if (condition)
			Logger.Info($"[{context.GetType().Name}] {trueMessage}");
		else
			Logger.Warn($"[{context.GetType().Name}] {falseMessage}");
	}

	/// <summary>
	/// Log progress percentage
	/// </summary>
	public static void LogProgress(this object context, int current, int total, string operation = "") {
		int percentage = (int)((current * 100.0) / total);
		string message = $"[{context.GetType().Name}] Progress: {percentage}% ({current}/{total})";
		
		if (!string.IsNullOrEmpty(operation))
			message += $" - {operation}";

		Logger.Debug(message);
	}

	/// <summary>
	/// Log structured data
	/// </summary>
	public static void LogStructured(this object context, string operation, params (string key, object value)[] data) {
		Logger.Info($"[{context.GetType().Name}] {operation}");
		foreach (var (key, value) in data) {
			Logger.Debug($"  {key}: {value}");
		}
	}

	/// <summary>
	/// Log method entry/exit
	/// </summary>
	public static void LogMethodEntry(this object context, string methodName, params object[] parameters) {
		var paramStr = string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"));
		Logger.Debug($"[{context.GetType().Name}] → {methodName}({paramStr})");
	}

	public static void LogMethodExit(this object context, string methodName, object? returnValue = null) {
		var returnStr = returnValue?.ToString() ?? "void";
		Logger.Debug($"[{context.GetType().Name}] ← {methodName}: {returnStr}");
	}

	/// <summary>
	/// Log with indentation level
	/// </summary>
	private static int _indentLevel = 0;

	public static void LogIndented(this object context, string message, int level = 1) {
		string indent = new string(' ', _indentLevel * 2);
		Logger.Debug($"{indent}{message}");
	}

	public static void LogIndentIn(this object context) {
		_indentLevel++;
	}

	public static void LogIndentOut(this object context) {
		_indentLevel = Math.Max(0, _indentLevel - 1);
	}

	/// <summary>
	/// Log exception with full context
	/// </summary>
	public static void LogExceptionDetail(this object context, Exception ex) {
		Logger.Error($"[{context.GetType().Name}] Exception Details:");
		Logger.Error($"  Type: {ex.GetType().FullName}");
		Logger.Error($"  Message: {ex.Message}");
		Logger.Error($"  Source: {ex.Source}");
		Logger.Error($"  HResult: {ex.HResult:X8}");
		
		if (ex.InnerException != null) {
			Logger.Error($"  Inner: {ex.InnerException.GetType().FullName}");
			Logger.Error($"    Message: {ex.InnerException.Message}");
		}

		if (!string.IsNullOrEmpty(ex.StackTrace)) {
			var lines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			Logger.Error($"  Stack Trace ({lines.Length} frames):");
			foreach (var line in lines.Take(5)) {
				Logger.Error($"    {line}");
			}
			if (lines.Length > 5)
				Logger.Error($"    ... and {lines.Length - 5} more frames");
		}
	}

	/// <summary>
	/// Log memory usage
	/// </summary>
	public static void LogMemoryUsage(this object context, string operation = "") {
		long memory = GC.GetTotalMemory(false);
		string memStr = memory > 1024 * 1024 
			? $"{memory / (1024.0 * 1024):F2} MB"
			: $"{memory / 1024.0:F2} KB";

		Logger.Debug($"[{context.GetType().Name}] Memory: {memStr} {operation}");
	}

	/// <summary>
	/// Log performance metrics
	/// </summary>
	public static void LogMetric(this object context, string metricName, double value, string unit = "") {
		Logger.Debug($"[{context.GetType().Name}] {metricName}: {value:F2} {unit}");
	}
}