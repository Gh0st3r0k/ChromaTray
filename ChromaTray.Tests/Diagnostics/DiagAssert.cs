using Xunit.Sdk;

namespace ChromaTray.Tests.Diagnostics;

/// <summary>
/// Проверки с подробным сообщением: входные данные, ожидаемый результат и подсказка для поиска ошибки.
/// </summary>
public static class DiagAssert
{
    public static void Equal<T>(
        T expected,
        T actual,
        string problemId,
        string component,
        string scenario,
        string inputContext,
        string hint)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            ThrowDiagnosticFailure(
                problemId,
                component,
                scenario,
                inputContext,
                expected?.ToString() ?? "<null>",
                actual?.ToString() ?? "<null>",
                hint);
        }
    }

    public static void DoubleEqual(
        double expected,
        double actual,
        double tolerance,
        string problemId,
        string component,
        string scenario,
        string inputContext,
        string hint)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            ThrowDiagnosticFailure(
                problemId,
                component,
                scenario,
                inputContext,
                $"{expected:F2} (±{tolerance})",
                $"{actual:F2}",
                hint);
        }
    }

    public static void True(
        bool condition,
        string problemId,
        string component,
        string scenario,
        string inputContext,
        string expectedDescription,
        string actualDescription,
        string hint)
    {
        if (!condition)
        {
            ThrowDiagnosticFailure(
                problemId,
                component,
                scenario,
                inputContext,
                expectedDescription,
                actualDescription,
                hint);
        }
    }

    private static void ThrowDiagnosticFailure(
        string problemId,
        string component,
        string scenario,
        string inputContext,
        string expected,
        string actual,
        string hint)
    {
        string report = $"""

            ================================================================================
            ❌ [CHROMATRAY DIAGNOSTIC FAILURE REPORT]
            ================================================================================
            • Problem ID   : {problemId}
            • Component    : {component}
            • Scenario     : {scenario}
            • Input Data   : {inputContext}
            --------------------------------------------------------------------------------
            • Expected     : {expected}
            • Actual       : {actual}
            --------------------------------------------------------------------------------
            💡 Root-Cause Hint:
              {hint}
            ================================================================================
            """;

        throw new XunitException(report);
    }
}
