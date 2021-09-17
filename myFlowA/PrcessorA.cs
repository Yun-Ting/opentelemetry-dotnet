// <copyright file="MyProcessor.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTelemetry;
using OpenTelemetry.Logs;

internal class MyProcessorA : BaseProcessor<LogRecord>
{
    private readonly string name;

    public MyProcessorA(string name = "MyProcessor")
    {
        this.name = name;
    }

    public override void OnStart(LogRecord logRecord)
    {
        Console.WriteLine($"{this.name}.OnStart({logRecord})");
    }

    public override void OnEnd(LogRecord logRecord)
    {
        var listKvp = logRecord.State as IReadOnlyList<KeyValuePair<string, object>>;

        if (listKvp == null)
        {
            return;
        }

        Regex rule = new Regex(@"(?i)sig=[a-z0-9%]{43,63}%3d");
        for (int i = 0; i < listKvp.Count; i++)
        {
            var entry = listKvp[i];
            var str = entry.Value as string; // if the value is not a string, we don't attempt to call ToString

            if (str != null)
            {
                Console.WriteLine(str);
                if (rule.IsMatch(str))
                {
                    Console.WriteLine("such a sad story!");
                } else
                {
                    Console.WriteLine("happy ending!");
                }
            }
        }

        Console.WriteLine($"{this.name}.OnEnd({logRecord})");
    }
}
