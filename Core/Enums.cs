using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

internal enum ActivityRunStatus
{
    Acquired, 
    AlreadyCompleted,
    CurrentlyRunning
}
