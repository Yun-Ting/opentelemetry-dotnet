// <copyright file="MyRedactionProcessor2.cs" company="OpenTelemetry Authors">
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Logs;

internal class MyClass : IReadOnlyList<KeyValuePair<string, object>>
{
    private IReadOnlyList<KeyValuePair<string, object>> myList;

    public MyClass(IReadOnlyList<KeyValuePair<string, object>> inputList)
    {
        this.myList = inputList;
    }

    public int Count => this.myList.Count();

    public KeyValuePair<string, object> this[int index] => this.myList[index];

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var entry in this.myList)
        {
            var entryVal = entry.Value;

            // TODO: check whether entryVal.ToString() would be null if the entryVal is null.
            if (entryVal != null && entryVal.ToString().Contains("sensitive info"))
            {
                yield return new KeyValuePair<string, object>(entry.Key, "newVal");
            }
            else
            {
                yield return entry;
            }
        }
    }

    //IEnumerator IEnumerable.GetEnumerator()
    //{
    //    return this.GetEnumerator();
    //}

    public override string ToString()
    {
        var cur = this.GetEnumerator();
        var sb = new StringBuilder();

        cur.MoveNext();
        sb.Append(cur.Current.ToString());
        while (cur.MoveNext())
        {
            sb.Append(", ");
            sb.Append(cur.Current.ToString());
        }

        return sb.ToString();
    }
}

internal class MyRedactionProcessor2 : BaseProcessor<LogRecord>
{
    private readonly string name;

    public MyRedactionProcessor2(string name)
    {
        this.name = name;
    }

    public override void OnEnd(LogRecord logRecord)
    {
        if (logRecord.State is IReadOnlyList<KeyValuePair<string, object>> listOfKvp)
        {
            logRecord.State = new MyClass(listOfKvp);
        }
    }
}
