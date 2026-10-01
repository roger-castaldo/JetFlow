using JetFlow.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

internal class PagedWorkflowQuery(IEnumerable<ActiveWorkflow> data, ulong lastOffset) : IPagedWorkflowQuery
{
    ulong IPagedWorkflowQuery.LastOffset => lastOffset;

    IEnumerator<ActiveWorkflow> IEnumerable<ActiveWorkflow>.GetEnumerator()
        => data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => data.GetEnumerator();
}
