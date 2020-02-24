// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class Progress
    {
        private const int ProgressDelayMs = 2000;
        private static readonly AsyncLocal<ImmutableStack<LogScope>> t_scope = new AsyncLocal<ImmutableStack<LogScope>>();

        public static IDisposable Start(string name)
        {
            var scope = new LogScope(name, Stopwatch.StartNew());

            t_scope.Value = (t_scope.Value ?? ImmutableStack<LogScope>.Empty).Push(scope);

            if (Log.Verbose)
            {
                Console.Write(scope.Name + "...\r");
            }

            return scope;
        }

        public static void Update(int done, int total)
        {
            Debug.Assert(t_scope.Value != null);

            var scope = t_scope.Value.Peek();

            // Only write progress if it takes longer than 2 seconds
            var elapsedMs = scope.Stopwatch.ElapsedMilliseconds;
            if (elapsedMs < ProgressDelayMs)
            {
                return;
            }

            // Throttle writing progress to console once every second.
            if (done != total && elapsedMs - scope.LastElapsedMs < 1000)
            {
                return;
            }
            scope.LastElapsedMs = elapsedMs;

            var eol = done == total ? '\n' : '\r';
            var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();
            var duration = TimeSpan.FromSeconds(elapsedMs / 1000);

            Console.Write($"{scope.Name}: {percent.PadLeft(3)}% ({done}/{total}), {duration} {eol}");
        }

        public static string FormatTimeSpan(TimeSpan value)
        {
            if (value.TotalMinutes > 1)
                return TimeSpan.FromSeconds(value.TotalSeconds).ToString();
            if (value.TotalSeconds > 1)
                return Math.Round(value.TotalSeconds, digits: 2) + "s";
            return Math.Round(value.TotalMilliseconds, digits: 2) + "ms";
        }

        private struct LogScope : IDisposable
        {
            public readonly string Name;

            public readonly Stopwatch Stopwatch;

            public long LastElapsedMs;

            public LogScope(string name, Stopwatch stopwatch)
            {
                Name = name;
                Stopwatch = stopwatch;
                LastElapsedMs = 0;
            }

            public void Dispose()
            {
                t_scope.Value = t_scope.Value!.Pop(out _);

                var elapsedMs = Stopwatch.ElapsedMilliseconds;
                if (Log.Verbose || elapsedMs > ProgressDelayMs)
                {
                    Console.WriteLine($"{Name} done in {FormatTimeSpan(Stopwatch.Elapsed)}");
                }
            }
        }
    }
}
