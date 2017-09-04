using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace Xunit.ConsoleClient
{
    public class ShowProgressVisitor : XmlTestExecutionVisitor
    {
        string assemblyFileName;
        readonly ConcurrentDictionary<string, ExecutionSummary> completionMessages;
        readonly StringBuilder log;

        public ShowProgressVisitor(XElement assemblyElement,
                                     Func<bool> cancelThunk,
                                     StringBuilder log,
                                     ConcurrentDictionary<string, ExecutionSummary> completionMessages = null)
            : base(assemblyElement, cancelThunk)
        {
            this.completionMessages = completionMessages;
            this.log = log;
        }

        protected override bool Visit(ITestAssemblyStarting assemblyStarting)
        {
            assemblyFileName = Path.GetFileName(assemblyStarting.TestAssembly.Assembly.AssemblyPath);

            log.AppendLine($"Starting:    {Path.GetFileNameWithoutExtension(assemblyFileName)}");

            return base.Visit(assemblyStarting);
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            // Base class does computation of results, so call it first.
            var result = base.Visit(assemblyFinished);

            log.AppendLine($"Finished:    {Path.GetFileNameWithoutExtension(assemblyFileName)}");

            if (completionMessages != null)
                completionMessages.TryAdd(Path.GetFileNameWithoutExtension(assemblyFileName), new ExecutionSummary
                {
                    Total = assemblyFinished.TestsRun,
                    Failed = assemblyFinished.TestsFailed,
                    Skipped = assemblyFinished.TestsSkipped,
                    Time = assemblyFinished.ExecutionTime,
                    Errors = Errors
                });

            return result;
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            log.AppendLine($"   {XmlEscape(testFailed.Test.DisplayName)} [FAIL]");
            log.AppendLine($"      {ExceptionUtility.CombineMessages(testFailed).Replace(Environment.NewLine, Environment.NewLine + "      ")}");

            return base.Visit(testFailed);
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            return base.Visit(testPassed);
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            return base.Visit(testSkipped);
        }

        protected override bool Visit(ITestStarting testStarting)
        {
            log.AppendLine($"   {XmlEscape(testStarting.Test.DisplayName)} [STARTING]");
            return base.Visit(testStarting);
        }

        protected override bool Visit(ITestFinished testFinished)
        {
            log.AppendLine($"   {XmlEscape(testFinished.Test.DisplayName)} [FINISHED] Time: {testFinished.ExecutionTime}s");
            return base.Visit(testFinished);
        }

        protected override bool Visit(IErrorMessage error)
        {
            WriteError("FATAL", error);

            return base.Visit(error);
        }

        protected override bool Visit(ITestAssemblyCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Assembly Cleanup Failure ({0})", cleanupFailure.TestAssembly.Assembly.AssemblyPath), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected override bool Visit(ITestCaseCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Case Cleanup Failure ({0})", cleanupFailure.TestCase.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected override bool Visit(ITestClassCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Class Cleanup Failure ({0})", cleanupFailure.TestClass.Class.Name), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected override bool Visit(ITestCollectionCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Collection Cleanup Failure ({0})", cleanupFailure.TestCollection.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected override bool Visit(ITestCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Cleanup Failure ({0})", cleanupFailure.Test.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected override bool Visit(ITestMethodCleanupFailure cleanupFailure)
        {
            WriteError(String.Format("Test Method Cleanup Failure ({0})", cleanupFailure.TestMethod.Method.Name), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        protected void WriteError(string failureName, IFailureInformation failureInfo)
        {
            log.AppendLine($"   [{failureName}] {XmlEscape(failureInfo.ExceptionTypes[0])}");
            log.AppendLine($"      {XmlEscape(ExceptionUtility.CombineMessages(failureInfo))}");

            // WriteStackTrace(ExceptionUtility.CombineStackTraces(failureInfo));
        }

        // void WriteStackTrace(string stackTrace)
        // {
        //     if (String.IsNullOrWhiteSpace(stackTrace))
        //         return;

        //     Console.ForegroundColor = ConsoleColor.DarkGray;
        //     Console.Error.WriteLine("      Stack Trace:");

        //     Console.ForegroundColor = ConsoleColor.Gray;
        //     foreach (var stackFrame in stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        //     {
        //         Console.Error.WriteLine("         {0}", StackFrameTransformer.TransformFrame(stackFrame, defaultDirectory));
        //     }
        // }
    }
}